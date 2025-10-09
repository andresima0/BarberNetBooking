using BarberNetBooking.Data;
using BarberNetBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Services;

public interface ISlotService
{
    Task<IReadOnlyList<TimeOnly>> GetAvailableSlotsAsync(int barberId, DateOnly date, int? serviceDuration = null);
}

public sealed class SlotService : ISlotService
{
    private readonly AppDbContext _db;
    public SlotService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TimeOnly>> GetAvailableSlotsAsync(int barberId, DateOnly date, int? serviceDuration = null)
    {
        var setting = await _db.Settings.AsNoTracking().FirstOrDefaultAsync()
                      ?? new ShopSetting { SlotMinutes = 30 };

        var dow = date.ToDateTime(TimeOnly.MinValue).DayOfWeek;
        var rule = await _db.WorkingHours.AsNoTracking()
            .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dow);

        var hasTimeOff = await _db.TimeOffs.AsNoTracking()
            .AnyAsync(t => t.BarberId == barberId && t.Date == date);

        if (rule == null || rule.IsClosed || hasTimeOff)
            return Array.Empty<TimeOnly>();

        var slot = TimeSpan.FromMinutes(setting.SlotMinutes);
        var duration = TimeSpan.FromMinutes(serviceDuration ?? 30);

        var times = new List<TimeOnly>();
        for (var t = rule.StartTime.ToTimeSpan(); t + duration <= rule.EndTime.ToTimeSpan(); t += slot)
            times.Add(TimeOnly.FromTimeSpan(t));

        var taken = await _db.Appointments.AsNoTracking()
            .Where(a => a.BarberId == barberId && a.Date == date && a.Status == AppointmentStatus.Confirmed)
            .Select(a => a.StartTime) // CORRIGIDO: era a.Time, agora é a.StartTime
            .ToListAsync();

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        return times
            .Where(t => !taken.Contains(t))
            .Where(t => date > today || t > TimeOnly.FromDateTime(now))
            .ToList();
    }
}