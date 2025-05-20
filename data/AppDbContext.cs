// FlightValidationService/Data/AppDbContext.cs

using FlightValidationService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Emit;

namespace FlightValidationService.Data
{
  public class AppDbContext : DbContext
  {
    public DbSet<ManualFlightEdit> ManualFlightEdits { get; set; }

    public DbSet<Flight> Flights { get; set; }
    public DbSet<CheckLog> CheckLogs { get; set; }
    public DbSet<User> Users { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Flights → public.flights
      modelBuilder.Entity<Flight>()
          .ToTable("flights")
          .HasIndex(f => new { f.FlightNumber, f.DepartureDate })
          .IsUnique();

      // Users → public.users
      modelBuilder.Entity<User>()
          .ToTable("users");

      // CheckLog → public.check_log
      modelBuilder.Entity<CheckLog>()
          .ToTable("check_log")
          .HasOne(c => c.User)
          .WithMany()  // нет навигации User.CheckLogs
          .HasForeignKey(c => c.UserId)
          .OnDelete(DeleteBehavior.Cascade);

      // ManualFlightEdit → public.manual_flight_edits
      modelBuilder.Entity<ManualFlightEdit>()
          .ToTable("manual_flight_edits")
          .HasOne(e => e.Flight)
          .WithMany(f => f.Edits)
          .HasForeignKey(e => e.FlightId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<ManualFlightEdit>()
          .HasOne(e => e.Admin)
          .WithMany()  // нет навигации User.Edits
          .HasForeignKey(e => e.AdminId)
          .OnDelete(DeleteBehavior.Restrict);
    }
  }
}
