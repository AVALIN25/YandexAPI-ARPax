using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using FlightValidationService.Data;
using FlightValidationService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using FlightValidationService.Models.Dto;
using FlightValidationService.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
  private readonly AppDbContext _db;
  private readonly IConfiguration _cfg;

  public AuthController(AppDbContext db, IConfiguration cfg)
  {
    _db = db;
    _cfg = cfg;
  }

  [AllowAnonymous]
  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginRequest req)
  {
    var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == req.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
      return Unauthorized();

    var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var jwt = _cfg.GetSection("Jwt");
    var keyBytes = Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!);


    var token = new JwtSecurityToken(
        issuer: jwt["Issuer"],
        audience: jwt["Issuer"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpireMinutes"]!)),
        signingCredentials: creds);

    return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
  }
}

