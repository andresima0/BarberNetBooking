using System.ComponentModel.DataAnnotations;
using BarberNetBooking.Data;
using BarberNetBooking.Models;
using BarberNetBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public IndexModel(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    // Dados para a view
    public List<Service> Services { get; set; } = new();
    public SelectList ServiceOptions { get; set; } = default!;
    public SelectList BarberOptions { get; set; } = default!;
    public List<BarberWorkingHour> WorkingHours { get; set; } = new(); // agregado (maior disponibilidade)
    public ShopInfo? ShopInfo { get; set; }

    // Mensagens de UI
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // Form de agendamento
    [BindProperty] public BookingInput Input { get; set; } = new();

    public class BookingInput
    {
        [Required] public int ServiceId { get; set; }
        [Required] public int BarberId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [Required] public TimeOnly StartTime { get; set; }

        [Required, EmailAddress] public string CustomerEmail { get; set; } = string.Empty;

        [Required] public string CustomerPhone { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        await LoadOptionsAndInfoAsync();
    }

    // ========= Carregamento de dados para a página =========
    private async Task LoadOptionsAndInfoAsync()
    {
        // Info geral
        ShopInfo = await _db.ShopInfos.AsNoTracking().FirstOrDefaultAsync();

        // Serviços e barbeiros ativos
        Services = await _db.Services.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var barbers = await _db.Barbers.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();

        ServiceOptions = new SelectList(Services, nameof(Service.Id), nameof(Service.Name));
        BarberOptions = new SelectList(barbers, nameof(Barber.Id), nameof(Barber.Name));

        // >>> Agrega horários por MAIOR disponibilidade entre todos os barbeiros ativos
        WorkingHours = barbers.Any()
            ? await AggregateMaxAvailabilityAsync(barbers.Select(b => b.Id).ToList())
            : new List<BarberWorkingHour>();
    }

    // ========= POST: confirmar agendamento =========
    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOptionsAndInfoAsync();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Por favor, corrija os erros do formulário.";
            return Page();
        }

        var service = await _db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == Input.ServiceId && s.IsActive);
        var barber = await _db.Barbers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == Input.BarberId && b.IsActive);

        if (service == null || barber == null)
        {
            ErrorMessage = "Serviço ou barbeiro inválido.";
            return Page();
        }

        // ainda disponível?
        var ok = await IsSlotAvailableAsync(Input.BarberId, Input.Date, Input.StartTime, service.DurationMinutes);
        if (!ok)
        {
            ErrorMessage = "Esse horário não está mais disponível. Escolha outro.";
            return Page();
        }

        var appt = new Appointment
        {
            ServiceId = service.Id,
            BarberId = barber.Id,
            Date = Input.Date,
            StartTime = Input.StartTime,
            EndTime = Input.StartTime.AddMinutes(service.DurationMinutes),
            Time = new TimeSpan(Input.StartTime.Hour, Input.StartTime.Minute, 0),
            CustomerEmail = Input.CustomerEmail.Trim(),
            CustomerPhone = Input.CustomerPhone.Trim(),
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        // ===== ENVIO DO E-MAIL DE CONFIRMAÇÃO =====
        try
        {
            await _emailService.SendAppointmentConfirmationAsync(appt, service, barber);
        }
        catch (Exception ex)
        {
            // Log do erro, mas não quebra o fluxo
            Console.WriteLine($"Erro ao enviar e-mail de confirmação: {ex.Message}");
        }
        // ==========================================

        SuccessMessage = "Agendamento confirmado!";
        ModelState.Clear();
        Input = new BookingInput();
        return Page();
    }

    // ========= GET: ?handler=AvailableSlots =========
    public async Task<IActionResult> OnGetAvailableSlotsAsync(int barberId, int serviceId, string date)
    {
        if (barberId <= 0 || serviceId <= 0 || string.IsNullOrWhiteSpace(date))
            return new JsonResult(new { error = "Parâmetros inválidos." });

        if (!DateOnly.TryParse(date, out var dateOnly))
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out dateOnly))
                return new JsonResult(new { error = "Data inválida." });
        }

        var service = await _db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);
        var barber = await _db.Barbers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == barberId && b.IsActive);

        if (service == null || barber == null)
            return new JsonResult(new { error = "Serviço ou barbeiro inválido." });

        var slots = await BuildAvailableSlotsAsync(barberId, dateOnly, service.DurationMinutes);

        return new JsonResult(new
        {
            slots = slots.Select(t => new
            {
                value = t.ToString("HH\\:mm"),
                text = t.ToString("HH:mm")
            })
        });
    }

    // ========= Helpers de disponibilidade =========
    private async Task<List<TimeOnly>> BuildAvailableSlotsAsync(int barberId, DateOnly date, int durationMinutes)
    {
        var dayOfWeek = date.ToDateTime(TimeOnly.MinValue).DayOfWeek;

        // Folga específica do barbeiro
        var hasTimeOff = await _db.TimeOffs.AsNoTracking()
            .AnyAsync(t => t.BarberId == barberId && t.Date == date);
        if (hasTimeOff) return new List<TimeOnly>();

        // Regra de horário do barbeiro no dia
        var wh = await _db.WorkingHours.AsNoTracking()
            .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dayOfWeek);

        if (wh == null || wh.IsClosed) return new List<TimeOnly>();

        var start = wh.StartTime;
        var end = wh.EndTime;

        // Agendamentos já confirmados
        var taken = await _db.Appointments.AsNoTracking()
            .Where(a => a.BarberId == barberId && a.Date == date && a.Status == AppointmentStatus.Confirmed)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var slot = await _db.Settings.AsNoTracking()
            .Select(s => s.SlotMinutes).FirstOrDefaultAsync();
        if (slot <= 0) slot = 15;

        var list = new List<TimeOnly>();
        for (var t = start; t.AddMinutes(durationMinutes) <= end; t = t.AddMinutes(slot))
        {
            var tEnd = t.AddMinutes(durationMinutes);
            var overlap = taken.Any(a => !(tEnd <= a.StartTime || t >= a.EndTime));
            if (!overlap) list.Add(t);
        }

        return list;
    }

    private async Task<bool> IsSlotAvailableAsync(int barberId, DateOnly date, TimeOnly start, int durationMinutes)
    {
        var slots = await BuildAvailableSlotsAsync(barberId, date, durationMinutes);
        return slots.Contains(start);
    }

    // ========= Agregação: MAIOR disponibilidade por dia =========
    private async Task<List<BarberWorkingHour>> AggregateMaxAvailabilityAsync(List<int> barberIds)
    {
        var rules = await _db.WorkingHours.AsNoTracking()
            .Where(w => barberIds.Contains(w.BarberId))
            .ToListAsync();

        var orderedDays = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        var result = new List<BarberWorkingHour>(7);

        foreach (var day in orderedDays)
        {
            var dayRules = rules.Where(r => r.DayOfWeek == day).ToList();

            if (dayRules.Count == 0 || dayRules.All(r => r.IsClosed))
            {
                result.Add(new BarberWorkingHour
                {
                    DayOfWeek = day,
                    IsClosed = true,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(18, 0),
                    LunchStartTime = null,
                    LunchEndTime = null
                });
                continue;
            }

            // Considera só os barbeiros abertos nesse dia
            var openRules = dayRules.Where(r => !r.IsClosed).ToList();

            // Maior disponibilidade do dia: menor início e maior fim entre os abertos
            var minStart = openRules.Min(r => r.StartTime);
            var maxEnd = openRules.Max(r => r.EndTime);

            // --- Almoço comum (interseção) ---
            var withLunch = openRules
                .Where(r => r.LunchStartTime.HasValue && r.LunchEndTime.HasValue)
                .ToList();

            TimeOnly? lunchStart = null;
            TimeOnly? lunchEnd = null;

            if (withLunch.Any())
            {
                var interStart = withLunch.Max(r => r.LunchStartTime!.Value);
                var interEnd = withLunch.Min(r => r.LunchEndTime!.Value);

                if (interStart < interEnd && interStart > minStart && interEnd < maxEnd)
                {
                    lunchStart = interStart;
                    lunchEnd = interEnd;
                }
            }

            result.Add(new BarberWorkingHour
            {
                DayOfWeek = day,
                IsClosed = false,
                StartTime = minStart,
                EndTime = maxEnd,
                LunchStartTime = lunchStart,
                LunchEndTime = lunchEnd
            });
        }

        return result;
    }

    // ========= Utilitário para a view =========
    public string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Segunda-feira",
        DayOfWeek.Tuesday => "Terça-feira",
        DayOfWeek.Wednesday => "Quarta-feira",
        DayOfWeek.Thursday => "Quinta-feira",
        DayOfWeek.Friday => "Sexta-feira",
        DayOfWeek.Saturday => "Sábado",
        DayOfWeek.Sunday => "Domingo",
        _ => day.ToString()
    };
}