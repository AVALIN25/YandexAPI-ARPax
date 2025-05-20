using FlightValidationService.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightValidationService.Models;

[Table("check_log")]
public class CheckLog
{
  public int Id { get; set; }
  public int UserId { get; set; }
  public User User { get; set; } = null!;
  public string FlightNumber { get; set; } = null!;
  public bool Result { get; set; }
  public DateTime Timestamp { get; set; }
}
