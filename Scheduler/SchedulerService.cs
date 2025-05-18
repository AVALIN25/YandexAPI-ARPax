using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using FlightValidationService.Services;

namespace FlightValidationService.Scheduler
{
  public class SchedulerService : BackgroundService
  {
    private readonly ILogger<SchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SchedulerService(ILogger<SchedulerService> logger, IServiceProvider serviceProvider)
    {
      _logger = logger;
      _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("⏱️ SchedulerService запущен");

      while (!stoppingToken.IsCancellationRequested)
      {
        _logger.LogInformation("🔄 Начинаем обновление расписания...");

        using (var scope = _serviceProvider.CreateScope())
        {
          var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
          await flightService.UpdateFlightsAsync();
        }

        _logger.LogInformation("✅ Обновление завершено. Ждём следующего запуска...");

        await Task.Delay(_interval, stoppingToken);
      }
    }
  }
}
