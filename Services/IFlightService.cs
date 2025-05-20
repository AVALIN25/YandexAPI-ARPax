using FlightValidationService.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightValidationService.Services;

public interface IFlightService
{
  Task LoadCacheFromDatabaseAsync();
  Task RefreshCacheFromApiAsync();
  IEnumerable<Flight> GetAll();
  Flight? Get(string flightNumber, DateTime date);
  Task<Flight> AddAsync(Flight f, int adminId);
  Task<Flight?> UpdateAsync(int id, Flight updated, int adminId);
  Task<bool> DeleteAsync(int id);
  Task<IEnumerable<ManualFlightEdit>> GetEditHistoryAsync(int flightId);
}
