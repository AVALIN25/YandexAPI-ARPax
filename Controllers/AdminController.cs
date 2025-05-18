using Microsoft.AspNetCore.Mvc;
using FlightValidationService.Services;

namespace FlightValidationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
  private readonly IFlightService _flightService;

  public AdminController(IFlightService flightService)
  {
    _flightService = flightService;
  }

  [HttpPost("update-flights")]
  public async Task<IActionResult> UpdateFlights()
  {
    await _flightService.UpdateFlightsAsync();
    return Ok(new { message = "Flights updated successfully." });
  }
}
