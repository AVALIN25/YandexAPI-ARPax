namespace FlightValidationService.Services
{
  public interface IFlightService
  {
    Task UpdateFlightsAsync();
  }

  public class FlightService : IFlightService
  {
    public async Task UpdateFlightsAsync()
    {
      Console.WriteLine("Загрузка расписания... (заглушка)");
      await Task.Delay(1000); // эмуляция работы
    }
  }
}
