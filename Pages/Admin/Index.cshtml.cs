using BarberNetBooking.Data;
using BarberNetBooking.Infrastructure;
using BarberNetBooking.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Pages.Admin;

[AdminAuthorize]
public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    public DashboardModel(AppDbContext db) => _db = db;

    public int TodayCount { get; set; }
    public int Next7Count { get; set; }
    public List<(string Name, int Count)> ByBarber { get; set; } = new();

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var end = today.AddDays(7);

        TodayCount = await _db.Appointments
            .Where(a => a.Date == today && a.Status == AppointmentStatus.Confirmed)
            .CountAsync();

        Next7Count = await _db.Appointments
            .Where(a => a.Date > today && a.Date <= end && a.Status == AppointmentStatus.Confirmed)
            .CountAsync();

        var data = await _db.Barbers
            .Select(b => new
            {
                b.Name,
                Count = _db.Appointments.Count(a => a.BarberId == b.Id && a.Date >= today && a.Status == AppointmentStatus.Confirmed)
            }).ToListAsync();

        ByBarber = data.Select(d => (d.Name, d.Count)).ToList();
    }
}