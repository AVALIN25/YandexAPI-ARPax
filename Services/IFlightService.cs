namespace FlightValidationService.Services
{
  public interface IFlightService
  {
    Task UpdateFlightsAsync();

    /// <summary>Загружает все рейсы из БД в память (IMemoryCache).</summary>
    Task LoadCacheFromDatabaseAsync();

    /// <summary>Ищет рейс сначала в кэше, потом в БД; если нашёл в БД — кладёт в кэш.</summary>
    Task<Flight?> GetFlightFromCacheOrDbAsync(string flightNumber, string? departureTime = null);

  }
}
