using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FlightValidationService.Models
{

  public class Flight
  {
    public int Id { get; set; }
    public string FlightNumber { get; set; } = null!;
    public DateTime DepartureDate { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public string Status { get; set; } = null!;
    public bool EditedByAdmin { get; set; }
    public string Source { get; set; } = null!;         // "yandex" или "manual"
    public DateTime LastUpdated { get; set; }

    public ICollection<ManualFlightEdit> Edits { get; set; } = new List<ManualFlightEdit>();
  }
}
