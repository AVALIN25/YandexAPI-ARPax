using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YandexApiExample
{
  // Модель ответа API
  public class YandexApiResponse
  {
    [JsonPropertyName("schedule")]
    public List<FlightInfo> Schedule { get; set; }
  }

  // Информация о рейсе
  public class FlightInfo
  {
    [JsonPropertyName("thread")]
    public ThreadInfo Thread { get; set; } = new ThreadInfo();

    // Данные о вылете
    [JsonPropertyName("departure")]
    public TimeInfo? Departure { get; set; }

    // Дополнительное поле статуса (если возвращается)
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    // Задержка в минутах (если есть)
    [JsonPropertyName("delay")]
    public int? Delay { get; set; }

    // Флаг задержки (если есть)
    [JsonPropertyName("is_delayed")]
    public bool? IsDelayed { get; set; }
  }

  // Информация о рейсе: номер, название маршрута и уникальный идентификатор
  public class ThreadInfo
  {
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    // Уникальный идентификатор рейса
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;
  }

  // Информация о времени и терминале вылета
  [JsonConverter(typeof(TimeInfoConverter))]
  public class TimeInfo
  {
    // Запланированное время вылета (в формате ISO 8601)
    [JsonPropertyName("planned")]
    public string? Planned { get; set; }

    // Терминал вылета
    [JsonPropertyName("terminal")]
    public string? Terminal { get; set; }
  }

  // Кастомный конвертер для поля TimeInfo, позволяющий обрабатывать его как строку или объект
  public class TimeInfoConverter : JsonConverter<TimeInfo>
  {
    public override TimeInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType == JsonTokenType.Null)
        return null;

      if (reader.TokenType == JsonTokenType.String)
      {
        string plannedTime = reader.GetString();
        return new TimeInfo { Planned = plannedTime };
      }

      if (reader.TokenType == JsonTokenType.StartObject)
      {
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
          JsonElement root = document.RootElement;
          string? planned = null;
          if (root.TryGetProperty("planned", out JsonElement plannedElement) && plannedElement.ValueKind == JsonValueKind.String)
            planned = plannedElement.GetString();
          else if (root.TryGetProperty("actual", out JsonElement actualElement) && actualElement.ValueKind == JsonValueKind.String)
            planned = actualElement.GetString();
          else if (root.TryGetProperty("time", out JsonElement timeElement) && timeElement.ValueKind == JsonValueKind.String)
            planned = timeElement.GetString();

          string? terminal = null;
          if (root.TryGetProperty("terminal", out JsonElement terminalElement) && terminalElement.ValueKind == JsonValueKind.String)
            terminal = terminalElement.GetString();

          return new TimeInfo
          {
            Planned = planned,
            Terminal = terminal
          };
        }
      }

      throw new JsonException("Unexpected token type for TimeInfo.");
    }

    public override void Write(Utf8JsonWriter writer, TimeInfo value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      writer.WriteString("planned", value.Planned);
      writer.WriteString("terminal", value.Terminal);
      writer.WriteEndObject();
    }
  }

  // Сервис для работы с Яндекс API расписания
  public class YandexApiService
  {
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.rasp.yandex.net/v3.0/schedule/";

    public YandexApiService(HttpClient httpClient, string apiKey)
    {
      _httpClient = httpClient;
      _apiKey = apiKey;
    }

    // Получение расписания для указанного аэропорта и даты
    public async Task<List<FlightInfo>> GetFlightsAsync(string stationCode, string date)
    {
      var parameters = new Dictionary<string, string>
            {
                { "apikey", _apiKey },
                { "station", stationCode },
                { "transport_types", "plane" },
                { "date", date },
                { "lang", "ru_RU" },
                { "show_fields", "departure,thread,status,delay,is_delayed" }
            };

      var url = $"{ApiUrl}?{string.Join("&", parameters.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"))}";
      Console.WriteLine($"Запрос к API: {url}");

      try
      {
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
          Console.WriteLine($"Ошибка запроса: {response.StatusCode} - {response.ReasonPhrase}");
          return new List<FlightInfo>();
        }
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var flightData = JsonSerializer.Deserialize<YandexApiResponse>(jsonResponse, options);
        return flightData?.Schedule ?? new List<FlightInfo>();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"API error: {ex.Message}");
        return new List<FlightInfo>();
      }
    }
  }

  class Program
  {
    static async Task Main(string[] args)
    {
      // Ваш API-ключ
      const string apiKey = "48aeaffd-917a-4a69-a9f4-3a3991c0e4ac";
      // Код станции для Кольцово
      const string stationCode = "s9600370";

      Console.Write("Введите номер рейса: ");
      string flightNumberInput = Console.ReadLine()?.Trim();
      if (string.IsNullOrWhiteSpace(flightNumberInput))
      {
        Console.WriteLine("Номер рейса не может быть пустым.");
        return;
      }

      using var httpClient = new HttpClient();
      var yandexService = new YandexApiService(httpClient, apiKey);

      // Поиск рейсов за несколько ближайших дней
      int searchDays = 3;
      List<FlightInfo> aggregatedFlights = new List<FlightInfo>();
      DateTime startDate = DateTime.Today;
      for (int i = 0; i < searchDays; i++)
      {
        string date = startDate.AddDays(i).ToString("yyyy-MM-dd");
        var flightsForDate = await yandexService.GetFlightsAsync(stationCode, date);
        if (flightsForDate != null)
          aggregatedFlights.AddRange(flightsForDate);
      }

      if (aggregatedFlights.Count == 0)
      {
        Console.WriteLine("API не вернул ни одного рейса. Проверьте параметры.");
        return;
      }

      // Фильтрация по номеру рейса
      var matchingFlights = aggregatedFlights.Where(f =>
          f.Thread?.Number?.Replace(" ", "")
              .Equals(flightNumberInput.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) == true).ToList();

      if (matchingFlights.Count == 0)
      {
        Console.WriteLine("Рейс с указанным номером не найден.");
        Console.WriteLine("\nСписок всех рейсов на ближайшие дни:");
        foreach (var f in aggregatedFlights)
        {
          Console.WriteLine($"Рейс: {f.Thread.Number} | Маршрут: {f.Thread.Title} | Вылет: {f.Departure?.Planned} | Терминал: {f.Departure?.Terminal}");
        }
        return;
      }

      // Выбираем ближайший по времени рейс
      FlightInfo? chosenFlight = null;
      DateTimeOffset now = DateTimeOffset.Now;
      var upcoming = matchingFlights
          .Select(f =>
          {
            DateTimeOffset dt;
            bool parsed = DateTimeOffset.TryParse(f.Departure?.Planned, out dt);
            return new { Flight = f, Parsed = parsed, DepartureTime = dt };
          })
          .Where(x => x.Parsed && x.DepartureTime >= now)
          .OrderBy(x => x.DepartureTime)
          .ToList();

      if (upcoming.Any())
      {
        chosenFlight = upcoming.First().Flight;
      }
      else
      {
        var past = matchingFlights
            .Select(f =>
            {
              DateTimeOffset dt;
              bool parsed = DateTimeOffset.TryParse(f.Departure?.Planned, out dt);
              return new { Flight = f, Parsed = parsed, DepartureTime = dt };
            })
            .Where(x => x.Parsed && x.DepartureTime < now)
            .OrderByDescending(x => x.DepartureTime)
            .ToList();
        if (past.Any())
        {
          chosenFlight = past.First().Flight;
        }
      }

      if (chosenFlight == null)
      {
        Console.WriteLine("Не удалось определить ближайший рейс.");
        return;
      }

      // Вывод информации по выбранному рейсу
      Console.WriteLine("\nИнформация по рейсу:");
      Console.WriteLine($"Маршрут: {chosenFlight.Thread.Title}");
      string departureRaw = chosenFlight.Departure?.Planned;
      if (!string.IsNullOrEmpty(departureRaw))
      {
        if (DateTimeOffset.TryParse(departureRaw, out DateTimeOffset departureDt))
        {
          string formattedDeparture = departureDt.ToString("HH:mm dd-MM-yyyy");
          Console.WriteLine($"Время вылета: {formattedDeparture}");

          if (departureDt < now)
          {
            Console.WriteLine("Рейс уже вылетел.");
          }
          else
          {
            TimeSpan timeUntilFlight = departureDt - now;
            if (timeUntilFlight.TotalHours > 12)
              Console.WriteLine("Проход в чистую зону запрещён.");
            else
              Console.WriteLine("Проход в чистую зону разрешён.");
          }
        }
        else
        {
          Console.WriteLine($"Время вылета: {departureRaw} (не удалось отформатировать)");
        }
      }
      else
      {
        Console.WriteLine("Время вылета не указано.");
      }

      // Вывод статуса рейса
      if (chosenFlight.Delay.HasValue && chosenFlight.Delay.Value > 0)
        Console.WriteLine($"Статус рейса: Задержан на {chosenFlight.Delay.Value} минут(ы).");
      else if (chosenFlight.IsDelayed.HasValue && chosenFlight.IsDelayed.Value)
        Console.WriteLine("Статус рейса: Задержан.");
      else if (!string.IsNullOrEmpty(chosenFlight.Status))
        Console.WriteLine($"Статус рейса: {chosenFlight.Status}");
      else
        Console.WriteLine("Статус рейса: По расписанию.");

      Console.WriteLine($"Уникальный идентификатор рейса: {chosenFlight.Thread.Uid}");
      Console.WriteLine($"Терминал: {chosenFlight.Departure?.Terminal ?? "не указан"}");
    }
  }
}
