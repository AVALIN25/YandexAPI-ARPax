using FlightValidationService.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("users")]
public class User
{
  [Key]
  public int Id { get; set; }

  [Required]
  [MaxLength(100)]
  public string Username { get; set; } = string.Empty;

  [MaxLength(255)]
  public string? PasswordHash { get; set; }

  [MaxLength(20)]
  public string? Role { get; set; }

  public ICollection<CheckLog>? Checks { get; set; }
  public ICollection<ManualFlightEdit>? AdminEdits { get; set; }
}
