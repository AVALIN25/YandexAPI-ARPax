using System;

namespace FlightValidationService.Models
{
  public class FlightEditDto
  {
    public DateOnly DepartureDate { get; set; }
    public string DepartureTime { get; set; } = null!;
    public string Status { get; set; } = null!;
  }
}
