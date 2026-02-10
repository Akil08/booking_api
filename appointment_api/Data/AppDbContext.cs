using appointment_api.Models;
using Microsoft.EntityFrameworkCore;

namespace appointment_api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<DayState> DayStates => Set<DayState>();
    public DbSet<PrioritySubscriber> PrioritySubscribers => Set<PrioritySubscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(b => new { b.PatientId, b.Date }).IsUnique();
            entity.Property(b => b.Date).HasColumnType("date");
            entity.Property(b => b.Status).HasConversion<string>();
        });

        modelBuilder.Entity<DayState>(entity =>
        {
            entity.HasIndex(d => d.Date).IsUnique();
            entity.Property(d => d.Date).HasColumnType("date");
        });

        modelBuilder.Entity<PrioritySubscriber>(entity =>
        {
            entity.HasIndex(p => p.PatientId).IsUnique();
        });
    }
}
