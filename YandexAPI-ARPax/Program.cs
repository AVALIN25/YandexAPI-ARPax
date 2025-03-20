using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace YandexApiExample
{
  // Модель ответа API
  public class YandexApiResponse
  {
    public List<FlightInfo> Schedule { get; set; }
  }

  // Информация о рейсе
  public class FlightInfo
  {
    public ThreadInfo Thread { get; set; }
    public string Departure { get; set; }
    public string Arrival { get; set; }
  }

  // Номер рейса
  public class ThreadInfo
  {
    public string Number { get; set; }
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
        var response = await _httpClient.GetFromJsonAsync<YandexApiResponse>(url);
        return response?.Schedule ?? new List<FlightInfo>();
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

      Console.Write("Введите дату (yyyy-mm-dd): ");
      string dateInput = Console.ReadLine();

      if (!DateTime.TryParse(dateInput, out DateTime dateValue))
      {
        Console.WriteLine("Неверный формат даты.");
        return;
      }
      string date = dateValue.ToString("yyyy-MM-dd");

      using var httpClient = new HttpClient();
      var yandexService = new YandexApiService(httpClient, apiKey);
      var flights = await yandexService.GetFlightsAsync(stationCode, date);

      if (flights.Count == 0)
      {
        Console.WriteLine("Нет рейсов для указанной даты или произошла ошибка.");
      }
      else
      {
        Console.WriteLine("Рейсы, время вылета и прилёта:");
        foreach (var flight in flights)
        {
          Console.WriteLine($"Рейс: {flight.Thread.Number}, Вылет: {flight.Departure}, Прилёт: {flight.Arrival}");
        }
      }
    }
  }
}
