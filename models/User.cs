using FlightValidationService.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightValidationService.Models;
[Table("users")]
public class User
{
  public int Id { get; set; }
  public string Username { get; set; } = null!;
  public string PasswordHash { get; set; } = null!;
  public string Role { get; set; } = null!;   // "admin" или "worker"
}

