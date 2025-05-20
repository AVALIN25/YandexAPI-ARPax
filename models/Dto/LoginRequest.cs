// Models/Dto/LoginRequest.cs
namespace FlightValidationService.Models.Dto
{
  public class LoginRequest
  {
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
  }
}
