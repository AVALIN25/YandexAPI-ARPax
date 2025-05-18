using Microsoft.EntityFrameworkCore;
using FlightValidationService.Models;

namespace FlightValidationService.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Flight> Flights { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<CheckLog> CheckLogs { get; set; }
    public DbSet<ManualFlightEdit> ManualFlightEdits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Flight>()
          .Property(f => f.DepartureTime)
          .HasColumnType("varchar(5)");

      base.OnModelCreating(modelBuilder);
    }

  }
}
