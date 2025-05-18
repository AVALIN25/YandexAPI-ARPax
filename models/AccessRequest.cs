using System;

namespace FlightValidationService.Models
{
  public class AccessRequest
  {
    public string FlightNumber { get; set; }
    public DateTime DepartureDate { get; set; }
  }
}
