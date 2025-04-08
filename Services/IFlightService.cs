public interface IFlightService
{
  Task UpdateFlightsAsync();
}

public class FlightService : IFlightService
{
  public async Task UpdateFlightsAsync()
  {
    // заглушка — реализация позже
    Console.WriteLine("Обновление расписания рейсов из Яндекс API...");
    await Task.CompletedTask;
  }
}
