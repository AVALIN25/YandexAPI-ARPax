using FlightValidationService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FlightValidationService.Services;
using FlightValidationService.Models.Dto;  // для LoginRequest, CheckAccessRequest



[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
  private readonly IFlightService _fs;
  public AdminController(IFlightService fs) => _fs = fs;

  // 1) Чтение всех рейсов — синхронно
  [HttpGet("flights")]
  public IActionResult GetAll()
  {
    var all = _fs.GetAll();
    return Ok(all);
  }

  // 2) Чтение одного рейса по Id — тоже синхронно
  [HttpGet("flights/{id}")]
  public IActionResult GetOne(int id)
  {
    // Не await — просто Enumerable.SingleOrDefault
    var f = _fs.GetAll().SingleOrDefault(x => x.Id == id);
    return f == null ? NotFound() : Ok(f);
  }

  // Оставляем асинхронными только те методы, 
  // где мы действительно await-им Task (AddAsync, UpdateAsync, DeleteAsync)
  [HttpPost("flights")]
  public async Task<IActionResult> Create([FromBody] Flight f)
  {
    var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var created = await _fs.AddAsync(f, adminId);
    return Ok(created);
  }

  [HttpPut("flights/{id}")]
  public async Task<IActionResult> Update(int id, [FromBody] Flight f)
  {
    var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var updated = await _fs.UpdateAsync(id, f, adminId);
    return updated == null ? NotFound() : Ok(updated);
  }

  [HttpDelete("flights/{id}")]
  public async Task<IActionResult> Delete(int id)
  {
    var removed = await _fs.DeleteAsync(id);
    return Ok(new { removed });
  }

  [HttpGet("flights/{id}/edits")]
  public async Task<IActionResult> History(int id)
  {
    var edits = await _fs.GetEditHistoryAsync(id);
    return Ok(edits);
  }
}
