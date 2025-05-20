using FlightValidationService.Models.Dto;
using FlightValidationService.Models;
using FlightValidationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class FlightController : ControllerBase
{
  private readonly IFlightService _fs;
  private readonly IFlightLogService _log;
  private static readonly TimeZoneInfo EkbZone =
      TimeZoneInfo.FindSystemTimeZoneById("Asia/Yekaterinburg");

  public FlightController(IFlightService fs, IFlightLogService log)
  {
    _fs = fs ?? throw new ArgumentNullException(nameof(fs));
    _log = log ?? throw new ArgumentNullException(nameof(log));
  }

  [Authorize(Policy = "WorkerOnly")]
  [HttpGet]
  public IActionResult GetAll()
  {
    var flightsUtc = _fs.GetAll();

    var flightsEkb = flightsUtc
        .Select(f =>
        {
          var rawUtc = f.DepartureDate.Date.Add(f.DepartureTime);
          var utc = DateTime.SpecifyKind(rawUtc, DateTimeKind.Utc);
          var ekbDt = TimeZoneInfo.ConvertTimeFromUtc(utc, EkbZone);

          return new
          {
            f.Id,
            f.FlightNumber,
            DepartureDate = ekbDt.Date,
            DepartureTime = ekbDt.TimeOfDay,
            f.Status,
            f.EditedByAdmin,
            f.Source,
            f.LastUpdated
          };
        })
        .ToList();

    var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EkbZone).Date;
    var tomorrow = today.AddDays(1);

    var todayList = flightsEkb.Where(f => f.DepartureDate == today).OrderBy(f => f.DepartureTime);
    var tomorrowList = flightsEkb.Where(f => f.DepartureDate == tomorrow).OrderBy(f => f.DepartureTime);
    var otherList = flightsEkb
        .Where(f => f.DepartureDate != today && f.DepartureDate != tomorrow)
        .OrderBy(f => f.DepartureDate).ThenBy(f => f.DepartureTime);

    return Ok(todayList.Concat(tomorrowList).Concat(otherList));
  }

  [Authorize(Policy = "WorkerOnly")]
  [HttpGet("{flightNumber}")]
  public IActionResult GetOne(string flightNumber, [FromQuery] DateTime date)
  {
    // Валидация номера рейса (только буквы и цифры):
    if (!System.Text.RegularExpressions.Regex.IsMatch(flightNumber, "^[A-Z0-9]+$"))
      return BadRequest("Invalid flight number format");

    // Используем только .Date без конвертаций!
    var searchDate = date.Date;

    var flight = _fs.Get(flightNumber, searchDate);
    if (flight == null)
      return NotFound();

    var rawUtc = DateTime.SpecifyKind(flight.DepartureDate.Date.Add(flight.DepartureTime), DateTimeKind.Utc);
    var ekbDt = TimeZoneInfo.ConvertTimeFromUtc(rawUtc, EkbZone);

    return Ok(new
    {
      flight.Id,
      flight.FlightNumber,
      DepartureDate = ekbDt.Date,
      DepartureTime = ekbDt.TimeOfDay,
      flight.Status,
      flight.EditedByAdmin,
      flight.Source,
      flight.LastUpdated
    });
  }

  [Authorize(Policy = "WorkerOnly")]
  [HttpPost("check-access")]
  public async Task<IActionResult> Check([FromBody] CheckAccessRequest req)
  {
    var reqDate = req.DepartureDate.Date;
    var flight = _fs.Get(req.FlightNumber, reqDate);

    var allowed = false;

    if (flight != null)
    {
      // Получаем UTC DateTime вылета
      var flightUtc = flight.DepartureDate.Date.Add(flight.DepartureTime);

      // Текущее время в UTC
      var nowUtc = DateTime.UtcNow;

      // Разница между временем вылета и сейчас
      var hoursUntilDeparture = (flightUtc - nowUtc).TotalHours;

      // Доступ только если меньше или равно 12 часов до вылета
      allowed = hoursUntilDeparture <= 12 && hoursUntilDeparture >= 0;
    }

    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    await _log.LogAsync(userId, req.FlightNumber, allowed);

    return Ok(new { allowed });
  }
}
