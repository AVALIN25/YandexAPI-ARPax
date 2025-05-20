namespace FlightValidationService.Models.Dto
{
  public class FlightDto
  {
    public int Id { get; set; }
    public string FlightNumber { get; set; } = null!;
    public DateTime DepartureDate { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public string Status { get; set; } = null!;
    public bool EditedByAdmin { get; set; }
    public string Source { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
  }
}
