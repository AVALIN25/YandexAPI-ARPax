using FlightValidationService.Data;
using FlightValidationService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1) Настраиваем DbContext
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) MemoryCache, HttpClient
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// 3) Сервисы
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IFlightLogService, FlightLogService>();

// 4) JWT
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
      o.RequireHttpsMetadata = false;
      o.SaveToken = true;
      o.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Issuer"]
      };
    });

// 5) Политики
builder.Services.AddAuthorization(opts =>
{
  opts.AddPolicy("WorkerOnly", p => p.RequireRole("worker", "admin"));
  opts.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
});

// 6) Фоновый сервис
builder.Services.AddHostedService<FlightRefreshService>();

// 7) Контроллеры
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 8) При старте подтягиваем кэш
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  db.Database.EnsureCreated();         // или .Migrate()
  var fs = scope.ServiceProvider.GetRequiredService<IFlightService>();
  await fs.LoadCacheFromDatabaseAsync();
}

app.Run();
