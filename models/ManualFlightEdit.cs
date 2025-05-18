using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("manual_flight_edits")]
public class ManualFlightEdit
{
  [Key]
  public int Id { get; set; }

  public int? FlightId { get; set; }
  [ForeignKey("FlightId")]
  public Flight? Flight { get; set; }

  public int? AdminId { get; set; }
  [ForeignKey("AdminId")]
  public User? Admin { get; set; }

  [MaxLength(20)]
  public string? OldStatus { get; set; }

  [MaxLength(20)]
  public string? NewStatus { get; set; }

  public DateTime? OldDeparture { get; set; }
  public DateTime? NewDeparture { get; set; }

  public DateTime? Timestamp { get; set; }
}
