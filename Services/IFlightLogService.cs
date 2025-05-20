// Services/IFlightLogService.cs
using System.Threading.Tasks;

namespace FlightValidationService.Services;

public interface IFlightLogService
{
  Task LogAsync(int userId, string flightNumber, bool result);
}
