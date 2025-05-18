using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("flights")]
public class Flight
{
  [Key]
  public int Id { get; set; }

  [Required]
  public string? FlightNumber { get; set; }

  [Column(TypeName = "varchar(5)")]
  public string DepartureTime { get; set; }

  public string? Status { get; set; }

  public string? Source { get; set; }

  public bool? EditedByAdmin { get; set; }

  public DateTime? LastUpdated { get; set; }

  public ICollection<ManualFlightEdit>? Edits { get; set; }
}
