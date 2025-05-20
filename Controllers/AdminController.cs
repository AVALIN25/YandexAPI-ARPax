using FlightValidationService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FlightValidationService.Services;
using FlightValidationService.Models.Dto;  // для LoginRequest, CheckAccessRequest
using System.Linq;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
  private readonly IFlightService _fs;
  public AdminController(IFlightService fs) => _fs = fs;

  // 1) Чтение всех рейсов — возвращаем только DTO
  [HttpGet("flights")]
  public IActionResult GetAll()
  {
    var all = _fs.GetAll()
      .Select(f => new FlightDto
      {
        Id = f.Id,
        FlightNumber = f.FlightNumber,
        DepartureDate = f.DepartureDate,
        DepartureTime = f.DepartureTime,
        Status = f.Status,
        EditedByAdmin = f.EditedByAdmin,
        Source = f.Source,
        LastUpdated = f.LastUpdated
      });
    return Ok(all);
  }

  // 2) Чтение одного рейса по Id — возвращаем DTO
  [HttpGet("flights/{id}")]
  public IActionResult GetOne(int id)
  {
    var f = _fs.GetAll().SingleOrDefault(x => x.Id == id);
    if (f == null) return NotFound();
    return Ok(new FlightDto
    {
      Id = f.Id,
      FlightNumber = f.FlightNumber,
      DepartureDate = f.DepartureDate,
      DepartureTime = f.DepartureTime,
      Status = f.Status,
      EditedByAdmin = f.EditedByAdmin,
      Source = f.Source,
      LastUpdated = f.LastUpdated
    });
  }

  // 3) Создать новый рейс — возвращаем DTO
  [HttpPost("flights")]
  public async Task<IActionResult> Create([FromBody] Flight f)
  {
    var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var created = await _fs.AddAsync(f, adminId);
    return Ok(new FlightDto
    {
      Id = created.Id,
      FlightNumber = created.FlightNumber,
      DepartureDate = created.DepartureDate,
      DepartureTime = created.DepartureTime,
      Status = created.Status,
      EditedByAdmin = created.EditedByAdmin,
      Source = created.Source,
      LastUpdated = created.LastUpdated
    });
  }

  // 4) Изменить рейс — возвращаем DTO
  [HttpPut("flights/{id}")]
  public async Task<IActionResult> Update(int id, [FromBody] Flight f)
  {
    var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var updated = await _fs.UpdateAsync(id, f, adminId);
    if (updated == null) return NotFound();
    return Ok(new FlightDto
    {
      Id = updated.Id,
      FlightNumber = updated.FlightNumber,
      DepartureDate = updated.DepartureDate,
      DepartureTime = updated.DepartureTime,
      Status = updated.Status,
      EditedByAdmin = updated.EditedByAdmin,
      Source = updated.Source,
      LastUpdated = updated.LastUpdated
    });
  }

  // 5) Удалить рейс 
  [HttpDelete("flights/{id}")]
  public async Task<IActionResult> Delete(int id)
  {
    var removed = await _fs.DeleteAsync(id);
    return Ok(new { removed });
  }

  // 6) История правок 
  [HttpGet("flights/{id}/edits")]
  public async Task<IActionResult> History(int id)
  {
    var edits = await _fs.GetEditHistoryAsync(id);
    return Ok(edits);
  }
}
