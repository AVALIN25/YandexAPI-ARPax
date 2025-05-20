// Services/FlightLogService.cs
using System;
using System.Threading.Tasks;
using FlightValidationService.Data;     
using FlightValidationService.Models;  

namespace FlightValidationService.Services;

public class FlightLogService : IFlightLogService
{
  private readonly AppDbContext _db;
  public FlightLogService(AppDbContext db) => _db = db;

  public async Task LogAsync(int userId, string flightNumber, bool result)
  {
    var log = new CheckLog
    {
      UserId = userId,
      FlightNumber = flightNumber,
      Result = result,
      Timestamp = DateTime.UtcNow   
    };
    _db.CheckLogs.Add(log);
    await _db.SaveChangesAsync();
  }
}
