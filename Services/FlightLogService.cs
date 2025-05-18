using FlightValidationService.Data;
using FlightValidationService.Models;

namespace FlightValidationService.Services
{
  public interface IFlightLogService
  {
    Task LogCheckAsync(int userId, string flightNumber, bool result);
    Task LogManualEditAsync(int adminId, int flightId, string oldStatus, string newStatus, DateTime oldDeparture, DateTime newDeparture);
  }

  public class FlightLogService : IFlightLogService
  {
    private readonly AppDbContext _context;

    public FlightLogService(AppDbContext context)
    {
      _context = context;
    }

    public async Task LogCheckAsync(int userId, string flightNumber, bool result)
    {
      var log = new CheckLog
      {
        UserId = userId,
        FlightNumber = flightNumber,
        Result = result,
        Timestamp = DateTime.UtcNow
      };

      _context.CheckLogs.Add(log);
      await _context.SaveChangesAsync();
    }

    public async Task LogManualEditAsync(int adminId, int flightId, string oldStatus, string newStatus, DateTime oldDeparture, DateTime newDeparture)
    {
      var log = new ManualFlightEdit
      {
        AdminId = adminId,
        FlightId = flightId,
        OldStatus = oldStatus,
        NewStatus = newStatus,
        OldDeparture = oldDeparture,
        NewDeparture = newDeparture,
        Timestamp = DateTime.UtcNow
      };

      _context.ManualFlightEdits.Add(log);
      await _context.SaveChangesAsync();
    }
  }
}
