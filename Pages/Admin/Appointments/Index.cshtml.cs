using System.ComponentModel.DataAnnotations;
using BarberNetBooking.Data;
using BarberNetBooking.Infrastructure;
using BarberNetBooking.Models;
using BarberNetBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Pages.Admin.Appointments;

[AdminAuthorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ISlotService _slots;
    public IndexModel(AppDbContext db, ISlotService slots) { _db = db; _slots = slots; }

    public List<Appointment> Items { get; set; } = new();
    public SelectList BarberOptions { get; set; } = default!;
    [BindProperty(SupportsGet = true)] public FilterInput Filters { get; set; } = new();
    public DateOnly DefaultRescheduleDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public class FilterInput
    {
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly? StartTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly? EndTime { get; set; }
        public int? BarberId { get; set; }
        public string? Status { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadBarbersAsync();
        var q = _db.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .AsQueryable();

        if (Filters.StartTime.HasValue) q = q.Where(a => a.Date >= Filters.StartTime.Value);
        if (Filters.EndTime.HasValue) q = q.Where(a => a.Date <= Filters.EndTime.Value);
        if (Filters.BarberId.HasValue) q = q.Where(a => a.BarberId == Filters.BarberId.Value);
        if (!string.IsNullOrWhiteSpace(Filters.Status) && Enum.TryParse<AppointmentStatus>(Filters.Status, out var st))
            q = q.Where(a => a.Status == st);

        Items = await q.OrderBy(a => a.Date).ThenBy(a => a.StartTime).ToListAsync(); // CORRIGIDO
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var a = await _db.Appointments.FindAsync(id);
        if (a == null) return NotFound();
        a.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRescheduleAsync(int id, DateOnly date, TimeOnly time)
    {
        var a = await _db.Appointments.FindAsync(id);
        if (a == null) return NotFound();

        // checa conflito - CORRIGIDO: compara StartTime com time (ambos TimeOnly)
        var conflict = await _db.Appointments.AnyAsync(x => 
            x.Id != id && 
            x.BarberId == a.BarberId && 
            x.Date == date && 
            x.StartTime == time && // CORRIGIDO
            x.Status == AppointmentStatus.Confirmed);
            
        if (conflict)
        {
            ModelState.AddModelError(string.Empty, "Horário em conflito para este barbeiro.");
            await OnGetAsync();
            return Page();
        }
        
        a.Date = date;
        a.StartTime = time; // CORRIGIDO
        a.Time = time.ToTimeSpan(); // CORRIGIDO: mantém campo legado
        a.Status = AppointmentStatus.Confirmed;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadBarbersAsync()
    {
        var barbers = await _db.Barbers.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        BarberOptions = new SelectList(barbers, nameof(Models.Barber.Id), nameof(Models.Barber.Name));
        ViewData["BarberOptions"] = BarberOptions; 
    }
}