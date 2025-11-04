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
    private readonly IEmailService _emailService;

    public IndexModel(AppDbContext db, ISlotService slots, IEmailService emailService)
    {
        _db = db;
        _slots = slots;
        _emailService = emailService;
    }

    public List<Appointment> Items { get; set; } = new();
    public SelectList BarberOptions { get; set; } = default!;
    [BindProperty(SupportsGet = true)] public FilterInput Filters { get; set; } = new();
    public DateOnly DefaultRescheduleDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData]
    public string? ErrorMessage { get; set; }

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

        Items = await q.OrderBy(a => a.Date).ThenBy(a => a.StartTime).ToListAsync();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var a = await _db.Appointments
            .Include(x => x.Service)
            .Include(x => x.Barber)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return NotFound();

        a.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        // Envia e-mail de cancelamento
        try
        {
            if (a.Service != null && a.Barber != null)
            {
                await _emailService.SendAppointmentCancellationAsync(a, a.Service, a.Barber);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar e-mail de cancelamento: {ex.Message}");
        }

        SuccessMessage = "Agendamento cancelado com sucesso.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRescheduleAsync(int id, DateOnly date, TimeOnly time)
    {
        var a = await _db.Appointments
            .Include(x => x.Service)
            .Include(x => x.Barber)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return NotFound();

        // Salva dados antigos para o e-mail
        var oldDate = a.Date;
        var oldTime = a.StartTime;

        // checa conflito
        var conflict = await _db.Appointments.AnyAsync(x => 
            x.Id != id && 
            x.BarberId == a.BarberId && 
            x.Date == date && 
            x.StartTime == time && 
            x.Status == AppointmentStatus.Confirmed);
            
        if (conflict)
        {
            ErrorMessage = "Horário em conflito para este barbeiro.";
            await OnGetAsync();
            return Page();
        }
        
        a.Date = date;
        a.StartTime = time;
        a.Time = time.ToTimeSpan();
        a.Status = AppointmentStatus.Confirmed;
        await _db.SaveChangesAsync();

        // Envia e-mail de remarcação
        try
        {
            if (a.Service != null && a.Barber != null)
            {
                await _emailService.SendAppointmentRescheduleAsync(a, a.Service, a.Barber, oldDate, oldTime);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar e-mail de remarcação: {ex.Message}");
        }

        SuccessMessage = "Agendamento remarcado com sucesso.";
        return RedirectToPage();
    }

    /// <summary>
    /// Handler para deletar múltiplos agendamentos
    /// </summary>
    public async Task<IActionResult> OnPostDeleteMultipleAsync(List<int> selectedIds)
    {
        if (selectedIds == null || !selectedIds.Any())
        {
            ErrorMessage = "Nenhum agendamento foi selecionado.";
            return RedirectToPage();
        }

        try
        {
            var appointmentsToDelete = await _db.Appointments
                .Include(a => a.Service)
                .Include(a => a.Barber)
                .Where(a => selectedIds.Contains(a.Id))
                .ToListAsync();

            if (!appointmentsToDelete.Any())
            {
                ErrorMessage = "Nenhum agendamento válido foi encontrado.";
                return RedirectToPage();
            }

            var count = appointmentsToDelete.Count;

            // Envia e-mails de cancelamento antes de deletar
            foreach (var appt in appointmentsToDelete)
            {
                try
                {
                    if (appt.Service != null && appt.Barber != null && appt.Status == AppointmentStatus.Confirmed)
                    {
                        await _emailService.SendAppointmentCancellationAsync(appt, appt.Service, appt.Barber);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao enviar e-mail para agendamento {appt.Id}: {ex.Message}");
                }
            }

            _db.Appointments.RemoveRange(appointmentsToDelete);
            await _db.SaveChangesAsync();

            SuccessMessage = count == 1 
                ? "1 agendamento foi apagado com sucesso." 
                : $"{count} agendamentos foram apagados com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao apagar agendamentos: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task LoadBarbersAsync()
    {
        var barbers = await _db.Barbers.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        BarberOptions = new SelectList(barbers, nameof(Models.Barber.Id), nameof(Models.Barber.Name));
        ViewData["BarberOptions"] = BarberOptions; 
    }
}