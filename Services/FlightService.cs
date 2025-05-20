using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FlightValidationService.Data;
using FlightValidationService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static FlightValidationService.Models.Flight;

namespace FlightValidationService.Services
{
  public class FlightService : IFlightService
  {
    private const string CACHE_KEY = "FLIGHTS";
    private const string KoltsovoCode = "s9600370";
    private const string ApiKey = "48aeaffd-917a-4a69-a9f4-3a3991c0e4ac";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    public FlightService(
        AppDbContext db,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory)
    {
      _db = db;
      _cache = cache;
      _httpClientFactory = httpClientFactory;
    }

    // Загружает из БД в кэш при старте
    public async Task LoadCacheFromDatabaseAsync()
    {
      var all = await _db.Flights
                         .AsNoTracking()
                         .ToListAsync();
      _cache.Set(CACHE_KEY, all);
    }

    // Возвращает все рейсы из кэша или БД
    public IEnumerable<Flight> GetAll()
    {
      if (_cache.TryGetValue<List<Flight>>(CACHE_KEY, out var cached) && cached is not null)
        return cached;

      var fresh = _db.Flights
                     .AsNoTracking()
                     .ToList();
      _cache.Set(CACHE_KEY, fresh, new MemoryCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
      });
      return fresh;
    }

    // Возвращает один рейс по номеру и дате (НОРМАЛИЗАЦИЯ!)
    public Flight? Get(string flightNumber, DateTime date)
    {
      var normalizedNumber = flightNumber.Replace(" ", "").ToUpper();
      var reqDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

      return GetAll()
          .SingleOrDefault(f =>
              f.FlightNumber == normalizedNumber &&
              f.DepartureDate.Date == reqDate.Date // сравниваем только Date
          );
    }

    // Добавление ручного рейса админом
    public async Task<Flight> AddAsync(Flight f, int adminId)
    {
      f.FlightNumber = f.FlightNumber.Replace(" ", "").ToUpper();
      f.DepartureDate = DateTime.SpecifyKind(f.DepartureDate.Date, DateTimeKind.Utc);

      if (string.IsNullOrEmpty(f.Status))
      {
        f.Status = FlightStatusConstants.OnTime;
      }
      f.Source = "manual";
      f.EditedByAdmin = true;
      f.LastUpdated = DateTime.UtcNow;

      _db.Flights.Add(f);
      await _db.SaveChangesAsync();

      _cache.Remove(CACHE_KEY);
      await LoadCacheFromDatabaseAsync();

      return f;
    }

    // Редактирование рейса админом
    public async Task<Flight?> UpdateAsync(int id, Flight updated, int adminId)
    {
      updated.FlightNumber = updated.FlightNumber.Replace(" ", "").ToUpper();
      updated.DepartureDate = DateTime.SpecifyKind(updated.DepartureDate.Date, DateTimeKind.Utc);

      var existing = await _db.Flights.FindAsync(id);
      if (existing == null) return null;

      // Запись истории изменений
      var edit = new ManualFlightEdit
      {
        FlightId = existing.Id,
        AdminId = adminId,
        OldStatus = existing.Status,
        NewStatus = updated.Status,
        OldDeparture = existing.DepartureDate.Date + existing.DepartureTime,
        NewDeparture = updated.DepartureDate.Date + updated.DepartureTime,
        Timestamp = DateTime.UtcNow
      };
      _db.ManualFlightEdits.Add(edit);

      // Применяем изменения
      existing.Status = updated.Status;
      existing.FlightNumber = updated.FlightNumber;
      existing.DepartureDate = updated.DepartureDate;
      existing.DepartureTime = updated.DepartureTime;
      existing.EditedByAdmin = true;
      existing.Source = "manual";
      existing.LastUpdated = DateTime.UtcNow;

      await _db.SaveChangesAsync();

      _cache.Remove(CACHE_KEY);
      await LoadCacheFromDatabaseAsync();

      return existing;
    }

    // Удаление рейса
    public async Task<bool> DeleteAsync(int id)
    {
      var flight = await _db.Flights.FindAsync(id);
      if (flight == null) return false;

      _db.Flights.Remove(flight);
      await _db.SaveChangesAsync();

      _cache.Remove(CACHE_KEY);
      await LoadCacheFromDatabaseAsync();

      return true;
    }

    // История правок для конкретного рейса
    public Task<IEnumerable<ManualFlightEdit>> GetEditHistoryAsync(int flightId)
    {
      var edits = _db.ManualFlightEdits
          .Where(e => e.FlightId == flightId)
          .Include(e => e.Admin)
          .OrderByDescending(e => e.Timestamp)
          .AsEnumerable();
      return Task.FromResult(edits);
    }

    // Внутренний метод: дергает Яндекс и парсит ответ
    private async Task<List<Flight>> FetchYandexAsync()
    {
      var client = _httpClientFactory.CreateClient();
      var today = DateTime.UtcNow.Date;
      var tomorrow = today.AddDays(1);

      var dates = new[] { today, tomorrow };
      var result = new List<Flight>();

      foreach (var date in dates)
      {
        var dateStr = date.ToString("yyyy-MM-dd");
        var url = $"https://api.rasp.yandex.net/v3.0/schedule/"
                    + $"?station={KoltsovoCode}"
                    + "&transport_types=plane&event=departure"
                    + $"&date={dateStr}&apikey={ApiKey}";

        var resp = await client.GetFromJsonAsync<YandexApiResponse>(url);
        if (resp?.Schedule == null) continue;

        foreach (var item in resp.Schedule)
        {
          var thread = item.Thread;
          var depStr = item.Departure;
          if (thread?.Number == null || depStr == null) continue;
          if (!DateTimeOffset.TryParse(depStr, out var dto)) continue;

          result.Add(new Flight
          {
            FlightNumber = thread.Number.Replace(" ", "").ToUpper(),
            DepartureDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
            DepartureTime = dto.UtcDateTime.TimeOfDay,
            Status = FlightStatusConstants.OnTime,
            Source = "external",
            EditedByAdmin = false,
            LastUpdated = DateTime.UtcNow
          });
        }
      }
      return result;
    }

    // Планировщик вызывает этот метод каждые 10 минут
    public async Task RefreshCacheFromApiAsync()
    {
      var fresh = await FetchYandexAsync();
      if (!fresh.Any()) return;

      using var tx = await _db.Database.BeginTransactionAsync();
      foreach (var f in fresh)
      {
        var exist = await _db.Flights
            .SingleOrDefaultAsync(x =>
                x.FlightNumber == f.FlightNumber &&
                x.DepartureDate == f.DepartureDate);

        if (exist == null)
        {
          _db.Flights.Add(f);
        }
        else if (!exist.EditedByAdmin)
        {
          exist.Status = f.Status; // f.Status всегда on_time
          exist.DepartureTime = f.DepartureTime;
          exist.LastUpdated = DateTime.UtcNow;
        }
      }

      await _db.SaveChangesAsync();
      await tx.CommitAsync();

      _cache.Set(CACHE_KEY, fresh, new MemoryCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
      });
    }
  }
}
