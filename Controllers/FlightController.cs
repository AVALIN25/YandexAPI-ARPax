using Microsoft.AspNetCore.Mvc;
using FlightValidationService.Data;
using FlightValidationService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using FlightValidationService.Services;

namespace FlightValidationService.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class FlightController : ControllerBase
  {
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public FlightController(AppDbContext context, IMemoryCache cache)
    {
      _context = context;
      _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetFlights()
    {
      var flights = await _context.Flights.ToListAsync();
      return Ok(flights);
    }


    [HttpPost]
    public async Task<IActionResult> CreateFlight([FromBody] Flight flight)
    {
      flight.Source = "manual";
      flight.EditedByAdmin = true;
      flight.LastUpdated = DateTime.UtcNow;

      _context.Flights.Add(flight);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetByFlightNumber), new
      {
        flightNumber = flight.FlightNumber,
        departureDate = flight.DepartureTime
      }, flight);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFlight(int id, [FromBody] Flight updated)
    {
      var flight = await _context.Flights.FindAsync(id);
      if (flight == null) return NotFound();

      // Обновляем строковое поле DepartureTime ("HH:mm"), если передано новое значение
      flight.DepartureTime = updated.DepartureTime ?? flight.DepartureTime;
      flight.Status = updated.Status;
      flight.Source = "manual";
      flight.EditedByAdmin = true;
      flight.LastUpdated = DateTime.UtcNow;

      await _context.SaveChangesAsync();
      return Ok(flight);
    }

    [HttpPost("check-access")]
    public IActionResult CheckAccess([FromBody] AccessRequest request)
    {
      // Ищем рейс по номеру — все рейсы на нужную дату уже загружены
      var flight = _context.Flights.FirstOrDefault(f =>
          f.FlightNumber == request.FlightNumber);


      if (flight == null)
        return NotFound("Рейс не найден");

      bool isAllowed = flight.Status == "on_time";
      return Ok(new { allowed = isAllowed });
    }

    // DELETE api/flight/clear
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearDatabase()
    {
      // Удаляем все записи из таблицы Flights
      _context.Flights.RemoveRange(_context.Flights);
      await _context.SaveChangesAsync();
      return Ok("База данных рейсов успешно очищена.");
    }



    // GET /api/flight/search?flightNumber={номер}
    [HttpGet("search")]
    public async Task<IActionResult> GetByFlightNumber(
        [FromQuery] string flightNumber,
        [FromServices] IFlightService flightService)
    {
      // Нормализуем номер
      var normalized = flightNumber.Replace(" ", "").ToLower().Trim();

      // Пытаемся найти в кэше или БД (через сервис)
      var flight = await flightService.GetFlightFromCacheOrDbAsync(normalized, /*departureTime*/ null!);

      // Если не найдено — грузим из Яндекса и повторяем поиск
      if (flight == null)
      {
        await flightService.UpdateFlightsAsync();
        flight = await flightService.GetFlightFromCacheOrDbAsync(normalized, null!);
        if (flight == null)
          return NotFound($"Рейс {flightNumber} не найден.");
      }

      return Ok(flight);
    }





  }
}
