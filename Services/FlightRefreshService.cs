using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlightValidationService.Services
{
  public class FlightRefreshService : IHostedService, IDisposable
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer? _timer;

    public FlightRefreshService(IServiceScopeFactory scopeFactory)
    {
      _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _timer = new Timer(
          async _ =>
          {
            // создаём scope заново для каждого вызова
            using var scope = _scopeFactory.CreateScope();
            var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
            await flightService.RefreshCacheFromApiAsync();
          },
          null,
          dueTime: TimeSpan.Zero,
          period: TimeSpan.FromMinutes(10)
      );
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _timer?.Change(Timeout.Infinite, 0);
      return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
  }
}
