using FlightValidationService.Services;
using FlightValidationService.Scheduler;
using Microsoft.EntityFrameworkCore;
using FlightValidationService.Data;





var builder = WebApplication.CreateBuilder(args);

// Кэш и HTTP
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// DI
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddHostedService<SchedulerService>();

builder.Services.AddScoped<IFlightLogService, FlightLogService>();


// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Первая загрузка при старте
using (var scope = app.Services.CreateScope())
{
  var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
  await flightService.LoadCacheFromDatabaseAsync();
}

app.Run();
