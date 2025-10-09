using System.ComponentModel.DataAnnotations;
using BarberNetBooking.Data;
using BarberNetBooking.Infrastructure;
using BarberNetBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Pages.Admin.Availability;

[AdminAuthorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public SelectList BarberOptions { get; set; } = default!;
    [BindProperty(SupportsGet = true)] public int SelectedBarberId { get; set; }

    [BindProperty] public List<WeeklyInput> Week { get; set; } = new();
    public List<BarberTimeOff> TimeOffs { get; set; } = new();

    [BindProperty] public TimeOffInput NewTimeOff { get; set; } = new();

    public class WeeklyInput
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; } = new(9,0);
        public TimeOnly EndTime { get; set; } = new(18,0);
        public bool IsClosed { get; set; }
    }

    public class TimeOffInput
    {
        [Required] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string? Reason { get; set; }
    }

    public async Task OnGetAsync()
    {
        var barbers = await _db.Barbers.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        BarberOptions = new SelectList(barbers, nameof(Barber.Id), nameof(Barber.Name));
        if (SelectedBarberId == 0 && barbers.Any()) SelectedBarberId = barbers.First().Id;

        await LoadWeekAsync();
        TimeOffs = await _db.TimeOffs.Where(t => t.BarberId == SelectedBarberId)
            .OrderBy(t => t.Date).ToListAsync();
    }

    private async Task LoadWeekAsync()
    {
        Week.Clear();
        var rules = await _db.WorkingHours.Where(w => w.BarberId == SelectedBarberId).ToListAsync();
        foreach (DayOfWeek d in Enum.GetValues(typeof(DayOfWeek)))
        {
            var r = rules.FirstOrDefault(x => x.DayOfWeek == d);
            Week.Add(new WeeklyInput
            {
                DayOfWeek = d,
                StartTime = r?.StartTime ?? new TimeOnly(9,0),
                EndTime = r?.EndTime ?? new TimeOnly(18,0),
                IsClosed = r?.IsClosed ?? (d == DayOfWeek.Sunday)
            });
        }
    }

    public async Task<IActionResult> OnPostSaveWeekAsync()
    {
        var existing = await _db.WorkingHours.Where(w => w.BarberId == SelectedBarberId).ToListAsync();
        foreach (var item in Week)
        {
            var r = existing.FirstOrDefault(x => x.DayOfWeek == item.DayOfWeek);
            if (r == null)
            {
                _db.WorkingHours.Add(new BarberWorkingHour
                {
                    BarberId = SelectedBarberId, 
                    DayOfWeek = item.DayOfWeek, 
                    StartTime = item.StartTime, 
                    EndTime = item.EndTime, 
                    IsClosed = item.IsClosed
                });
            }
            else
            {
                r.StartTime = item.StartTime; 
                r.EndTime = item.EndTime; 
                r.IsClosed = item.IsClosed;
            }
        }
        await _db.SaveChangesAsync();
        return RedirectToPage(new { SelectedBarberId });
    }

    public async Task<IActionResult> OnPostAddTimeOffAsync()
    {
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }
        _db.TimeOffs.Add(new BarberTimeOff { BarberId = SelectedBarberId, Date = NewTimeOff.Date, Reason = NewTimeOff.Reason });
        await _db.SaveChangesAsync();
        return RedirectToPage(new { SelectedBarberId });
    }

    public async Task<IActionResult> OnPostDeleteTimeOffAsync(int id)
    {
        var t = await _db.TimeOffs.FindAsync(id);
        if (t != null) { _db.TimeOffs.Remove(t); await _db.SaveChangesAsync(); }
        return RedirectToPage(new { SelectedBarberId });
    }
}