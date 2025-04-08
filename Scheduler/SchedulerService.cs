using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace FlightValidationService.Scheduler
{
  public class SchedulerService : BackgroundService
  {
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public SchedulerService(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        using var scope = _serviceProvider.CreateScope();
        var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
        await flightService.UpdateFlightsAsync();
        await Task.Delay(_interval, stoppingToken);
      }
    }
  }
}
