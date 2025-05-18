using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("check_log")]
public class CheckLog
{
  [Key]
  public int Id { get; set; }

  public int? UserId { get; set; }
  [ForeignKey("UserId")]
  public User? User { get; set; }

  [MaxLength(20)]
  public string? FlightNumber { get; set; }

  public bool? Result { get; set; }

  public DateTime? Timestamp { get; set; }
}
