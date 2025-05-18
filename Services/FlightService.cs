#nullable enable
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FlightValidationService.Data;
using FlightValidationService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FlightValidationService.Services
{
  public class FlightService : IFlightService
  {
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string KoltsovoCode = "s9600370";

    public FlightService(AppDbContext context, IMemoryCache cache, IHttpClientFactory httpClientFactory)
    {
      _context = context;
      _cache = cache;
      _httpClientFactory = httpClientFactory;
    }

    private string GetCacheKey(string flightNumber, string departureTime)
    {
      if (flightNumber is null) throw new ArgumentNullException(nameof(flightNumber));
      if (departureTime is null) throw new ArgumentNullException(nameof(departureTime));
      return $"{flightNumber.Replace(" ", "").ToLower()}_{departureTime}";
    }

    public async Task LoadCacheFromDatabaseAsync()
    {
      var allFlights = await _context.Flights
          .AsNoTracking()
          .ToListAsync();

      foreach (var f in allFlights)
      {
        var key = GetCacheKey(f.FlightNumber!, f.DepartureTime!);
        _cache.Set(key, f, new MemoryCacheEntryOptions
        {
          AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });
      }
    }

    public async Task<Flight?> GetFlightFromCacheOrDbAsync(string flightNumber, string? departureTime = null)
    {
      if (flightNumber is null) throw new ArgumentNullException(nameof(flightNumber));

      var normalized = flightNumber.Replace(" ", "").ToLower().Trim();
      string key;
      if (departureTime is null)
      {
        // если время не указано — используем только номер как ключ
        key = normalized;
      }
      else
      {
        key = GetCacheKey(normalized, departureTime);
      }

      // пробуем взять из кэша (cached может быть null)
      if (_cache.TryGetValue(key, out Flight? cached) && cached is not null)
        return cached;

      var flight = await _context.Flights
          .AsNoTracking()
          .FirstOrDefaultAsync(f =>
              f.FlightNumber!.Replace(" ", "").ToLower() == normalized &&
              (departureTime == null || f.DepartureTime == departureTime)
          );

      if (flight is not null)
      {
        // для записи кэша формируем ключ с временем
        var cacheKey = GetCacheKey(normalized, flight.DepartureTime!);
        _cache.Set(cacheKey, flight, new MemoryCacheEntryOptions
        {
          AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });
      }

      return flight;
    }

    public async Task UpdateFlightsAsync()
    {
      Console.WriteLine("🔄 Начинаем обновление расписания...");

      var client = _httpClientFactory.CreateClient();
      var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
      var url = $"https://api.rasp.yandex.net/v3.0/schedule/?station={KoltsovoCode}" +
                $"&transport_types=plane&event=departure&date={date}&apikey=48aeaffd-917a-4a69-a9f4-3a3991c0e4ac";

      try
      {
        var response = await client.GetFromJsonAsync<YandexApiResponse>(url);
        if (response?.Schedule is not { Count: > 0 })
        {
          Console.WriteLine("⚠️ Не удалось получить список рейсов или список пуст");
          return;
        }

        Console.WriteLine($"✅ Получено рейсов: {response.Schedule.Count}");

        foreach (var item in response.Schedule)
        {
          if (item?.Thread?.Number is null || item.Departure is null)
          {
            Console.WriteLine("❗ Пропущен сегмент: нет номера рейса или времени вылета");
            continue;
          }

          if (!DateTimeOffset.TryParse(item.Departure, out var departureDto))
          {
            Console.WriteLine($"❗ Ошибка преобразования даты: {item.Departure}");
            continue;
          }

          var departureTimeString = departureDto
              .ToOffset(TimeSpan.FromHours(5))
              .ToString("HH:mm");

          var flight = new Flight
          {
            FlightNumber = item.Thread.Number!,
            DepartureTime = departureTimeString,
            Status = "on_time",
            Source = "external",
            EditedByAdmin = false,
            LastUpdated = DateTime.UtcNow
          };

          var existing = await _context.Flights.FirstOrDefaultAsync(f =>
              f.FlightNumber == flight.FlightNumber &&
              f.DepartureTime == flight.DepartureTime);

          if (existing == null)
          {
            _context.Flights.Add(flight);
            Console.WriteLine("➕ Добавлен новый рейс в базу");
          }
          else if (!existing.EditedByAdmin.GetValueOrDefault())
          {
            existing.Status = flight.Status;
            existing.Source = flight.Source;
            existing.LastUpdated = DateTime.UtcNow;
            Console.WriteLine("♻️ Обновлён существующий рейс");
          }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine("✅ Сохранено в базу данных");

        await LoadCacheFromDatabaseAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine("❌ Ошибка при получении данных из Яндекс API:");
        Console.WriteLine(ex.Message);
      }

      Console.WriteLine("⏱ Обновление завершено. Ждём следующего запуска...");
    }
  }
}
#nullable restore
