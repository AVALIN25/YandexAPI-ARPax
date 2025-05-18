using System.Text.Json.Serialization;

namespace FlightValidationService.Models
{
  public class YandexApiResponse
  {
    [JsonPropertyName("schedule")]
    public List<YandexScheduleItem>? Schedule { get; set; }
  }

  public class YandexScheduleItem
  {
    [JsonPropertyName("departure")]
    public string? Departure { get; set; }

    [JsonPropertyName("thread")]
    public ThreadInfo? Thread { get; set; }
  }

  public class ThreadInfo
  {
    [JsonPropertyName("number")]
    public string? Number { get; set; }
  }
}
