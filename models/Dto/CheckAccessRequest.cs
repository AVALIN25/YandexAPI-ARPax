// Models/Dto/CheckAccessRequest.cs
namespace FlightValidationService.Models.Dto
{
  public class CheckAccessRequest
  {
    public string FlightNumber { get; set; } = null!;
    public DateTime DepartureDate { get; set; }
  }
}
