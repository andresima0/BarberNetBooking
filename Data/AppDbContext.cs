using BarberNetBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Barber> Barbers => Set<Barber>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<BarberWorkingHour> WorkingHours => Set<BarberWorkingHour>();
    public DbSet<BarberTimeOff> TimeOffs => Set<BarberTimeOff>();
    public DbSet<ShopSetting> Settings => Set<ShopSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Price como REAL (SQLite)
        modelBuilder.Entity<Service>()
            .Property(p => p.Price)
            .HasConversion<double>()
            .HasColumnType("REAL");

        // Appointment: DateOnly -> string
        modelBuilder.Entity<Appointment>()
            .Property(a => a.Date)
            .HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v));

        // Appointment: TimeOnly -> TimeSpan
        modelBuilder.Entity<Appointment>()
            .Property(a => a.StartTime)
            .HasConversion(
                v => v.Ticks,
                v => TimeOnly.FromTimeSpan(TimeSpan.FromTicks(v)))
            .HasColumnType("INTEGER");

        modelBuilder.Entity<Appointment>()
            .Property(a => a.EndTime)
            .HasConversion(
                v => v.Ticks,
                v => TimeOnly.FromTimeSpan(TimeSpan.FromTicks(v)))
            .HasColumnType("INTEGER");
        
        // Evita dupla reserva: um barbeiro não pode ter duas marcações iguais
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.BarberId, a.Date, a.StartTime })
            .IsUnique();

        // BarberWorkingHour: TimeOnly -> TimeSpan
        modelBuilder.Entity<BarberWorkingHour>()
            .Property(w => w.StartTime)
            .HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));

        modelBuilder.Entity<BarberWorkingHour>()
            .Property(w => w.EndTime)
            .HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));

        // Conversão dos novos campos de almoço
        modelBuilder.Entity<BarberWorkingHour>()
            .Property(w => w.LunchStartTime)
            .HasConversion(
                v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : (TimeOnly?)null);

        modelBuilder.Entity<BarberWorkingHour>()
            .Property(w => w.LunchEndTime)
            .HasConversion(
                v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : (TimeOnly?)null);

        // 1 regra por barbeiro/dia da semana
        modelBuilder.Entity<BarberWorkingHour>()
            .HasIndex(w => new { w.BarberId, w.DayOfWeek })
            .IsUnique();

        // BarberTimeOff: DateOnly -> string
        modelBuilder.Entity<BarberTimeOff>()
            .Property(t => t.Date)
            .HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v));

        // 1 folga por barbeiro/data
        modelBuilder.Entity<BarberTimeOff>()
            .HasIndex(t => new { t.BarberId, t.Date })
            .IsUnique();
    }
}