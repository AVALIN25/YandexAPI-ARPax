using Microsoft.AspNetCore.Mvc;

namespace FlightValidationService.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class HelloController : ControllerBase
  {
    [HttpGet]
    public IActionResult Get()
    {
      return Ok("Сервис работает ✅");
    }
  }
}
