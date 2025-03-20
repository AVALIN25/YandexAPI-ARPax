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
    public ThreadInfo Thread { get; set; } = new();

    // Поля departure и arrival могут быть либо объектами, либо строками
    [JsonPropertyName("departure")]
    public TimeInfo? Departure { get; set; }

    [JsonPropertyName("arrival")]
    public TimeInfo? Arrival { get; set; }

    [JsonPropertyName("is_delayed")]
    public bool IsDelayed { get; set; }
  }

  // Класс для хранения информации о времени и терминале
  [JsonConverter(typeof(TimeInfoConverter))]
  public class TimeInfo
  {
    // Запланированное (или фактическое) время
    [JsonPropertyName("planned")]
    public string? Planned { get; set; }

    // Терминал, если указан
    [JsonPropertyName("terminal")]
    public string? Terminal { get; set; }
  }

  // Кастомный конвертер для TimeInfo, который обрабатывает и строку, и объект,
  // а также проверяет поле "actual", если "planned" отсутствует.
  public class TimeInfoConverter : JsonConverter<TimeInfo>
  {
    public override TimeInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType == JsonTokenType.Null)
        return null;

      // Если значение представлено строкой, считаем его как planned время
      if (reader.TokenType == JsonTokenType.String)
      {
        string plannedTime = reader.GetString();
        return new TimeInfo { Planned = plannedTime };
      }
      // Если значение представлено объектом
      if (reader.TokenType == JsonTokenType.StartObject)
      {
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
          JsonElement root = document.RootElement;
          string? planned = null;
          // Пробуем получить "planned"
          if (root.TryGetProperty("planned", out JsonElement plannedElement) && plannedElement.ValueKind == JsonValueKind.String)
          {
            planned = plannedElement.GetString();
          }
          // Если "planned" отсутствует, пробуем "actual"
          else if (root.TryGetProperty("actual", out JsonElement actualElement) && actualElement.ValueKind == JsonValueKind.String)
          {
            planned = actualElement.GetString();
          }

          string? terminal = root.TryGetProperty("terminal", out JsonElement terminalElement) && terminalElement.ValueKind == JsonValueKind.String
              ? terminalElement.GetString()
              : null;
          return new TimeInfo { Planned = planned, Terminal = terminal };
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

  // Информация о номере рейса
  public class ThreadInfo
  {
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;
  }

  // Сервис для работы с Яндекс API
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

    // Получает рейсы для указанной станции и даты
    public async Task<List<FlightInfo>> GetFlightsAsync(string stationCode, string date)
    {
      var parameters = new Dictionary<string, string>
            {
                { "apikey", _apiKey },
                { "station", stationCode },
                { "transport_types", "plane" },
                { "date", date },
                { "lang", "ru_RU" }
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
      const string apiKey = "48aeaffd-917a-4a69-a9f4-3a3991c0e4ac";
      const string stationCode = "s9600370";
      string date = DateTime.Now.ToString("yyyy-MM-dd");

      Console.Write("Введите номер рейса: ");
      string flightNumberInput = Console.ReadLine()?.Trim();
      if (string.IsNullOrWhiteSpace(flightNumberInput))
      {
        Console.WriteLine("Номер рейса не может быть пустым.");
        return;
      }

      using var httpClient = new HttpClient();
      var yandexService = new YandexApiService(httpClient, apiKey);
      var flights = await yandexService.GetFlightsAsync(stationCode, date);

      if (flights.Count == 0)
      {
        Console.WriteLine("API не вернул ни одного рейса. Проверьте дату и код аэропорта.");
        return;
      }

      // Фильтрация рейсов по номеру
      var flight = flights.FirstOrDefault(f =>
          f.Thread?.Number?.Replace(" ", "")
              .Equals(flightNumberInput.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) == true);

      if (flight == null)
      {
        Console.WriteLine("Рейс с указанным номером не найден.");
        // Вывод всех рейсов, если рейс не найден
        Console.WriteLine("Список всех рейсов на эту дату:");
        foreach (var f in flights)
        {
          Console.WriteLine($"Рейс: {f.Thread.Number}, Вылет: {f.Departure?.Planned}, Прилёт: {f.Arrival?.Planned}");
        }
      }
      else
      {
        Console.WriteLine("Информация по рейсу:");
        Console.WriteLine($"Рейс: {flight.Thread.Number}");
        Console.WriteLine($"Вылет: {flight.Departure?.Planned ?? "неизвестно"}");
        Console.WriteLine($"Прилёт: {flight.Arrival?.Planned ?? "неизвестно"}");
        Console.WriteLine($"Терминал вылета: {flight.Departure?.Terminal ?? "не указан"}");
        Console.WriteLine($"Терминал прилёта: {flight.Arrival?.Terminal ?? "не указан"}");
        Console.WriteLine($"Статус: {(flight.IsDelayed ? "Задержан" : "По расписанию")}");
      }
    }
  }
}
