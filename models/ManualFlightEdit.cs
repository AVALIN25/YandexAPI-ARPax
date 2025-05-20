using System;

namespace FlightValidationService.Models
{
  public class ManualFlightEdit
  {
    public int Id { get; set; }

    // На какой рейс было изменение
    public int FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    // Кто правил
    public int AdminId { get; set; }
    public User Admin { get; set; } = null!;

    // Старые и новые значения
    public string OldStatus { get; set; } = null!;
    public string NewStatus { get; set; } = null!;
    public DateTime OldDeparture { get; set; }
    public DateTime NewDeparture { get; set; }

    // Когда
    public DateTime Timestamp { get; set; }
  }
}
