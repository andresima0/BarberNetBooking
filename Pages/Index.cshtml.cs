using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
    private readonly IAvailabilityService _availabilityService;

    public IndexModel(AppDbContext db, IAvailabilityService availabilityService)
    {
        _db = db;
        _availabilityService = availabilityService;
    }

    // Tabela de serviços e combos
    public List<Service> Services { get; set; } = new();
    public SelectList ServiceOptions { get; set; } = default!;
    public SelectList BarberOptions  { get; set; } = default!;

    [BindProperty] public BookingInput Input { get; set; } = new();

    [TempData] public string? ErrorMessage { get; set; }
    [TempData] public string? SuccessMessage { get; set; }

    public class BookingInput
    {
        [Required(ErrorMessage = "Selecione um serviço")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Selecione um barbeiro")]
        public int BarberId { get; set; }

        [Required(ErrorMessage = "Selecione uma data")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required(ErrorMessage = "Selecione um horário")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Informe seu email")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu telefone")]
        [RegularExpression(@"^\(?\d{2}\)?\s?\d{4,5}\-?\d{4}$", ErrorMessage = "Telefone inválido")]
        public string CustomerPhone { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        Services = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Price)
            .ToListAsync();

        ServiceOptions = new SelectList(Services, nameof(Service.Id), nameof(Service.Name));

        var barbers = await _db.Barbers
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();
        BarberOptions = new SelectList(barbers, nameof(Barber.Id), nameof(Barber.Name));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Por favor, corrija os campos destacados.";
            await LoadOptionsAsync();
            return Page();
        }

        if (Input.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            ErrorMessage = "Escolha uma data futura.";
            await LoadOptionsAsync();
            return Page();
        }

        var service = await _db.Services.FindAsync(Input.ServiceId);
        if (service is null || !service.IsActive)
        {
            ErrorMessage = "Serviço inválido.";
            await LoadOptionsAsync();
            return Page();
        }

        var barber = await _db.Barbers.FindAsync(Input.BarberId);
        if (barber is null /* || !barber.IsActive */)
        {
            ErrorMessage = "Barbeiro inválido.";
            await LoadOptionsAsync();
            return Page();
        }

        var endTime = TimeOnly.FromTimeSpan(
            Input.StartTime.ToTimeSpan().Add(TimeSpan.FromMinutes(service.DurationMinutes))
        );

        var (isAvailable, availabilityError) = await _availabilityService.IsBarberAvailableAsync(
            Input.BarberId, Input.Date, Input.StartTime, endTime);

        if (!isAvailable)
        {
            ErrorMessage = availabilityError ?? "Horário não disponível.";
            await LoadOptionsAsync();
            return Page();
        }

        var appointment = new Appointment
        {
            ServiceId     = Input.ServiceId,
            BarberId      = Input.BarberId,
            Date          = Input.Date,
            StartTime     = Input.StartTime,
            EndTime       = endTime,
            Time          = Input.StartTime.ToTimeSpan(), // persistir como TimeSpan (compatível com EF/SQLite)
            CustomerEmail = Input.CustomerEmail.Trim(),
            CustomerPhone = Input.CustomerPhone.Trim(),
            Status        = AppointmentStatus.Confirmed,
            CreatedAtUtc  = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ErrorMessage = "Esse horário acabou de ser reservado. Escolha outro.";
            await LoadOptionsAsync();
            return Page();
        }

        SuccessMessage = "Agendamento realizado com sucesso! Aguardamos você.";
        return RedirectToPage(); // PRG
    }

    private async Task LoadOptionsAsync()
    {
        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var barbers = await _db.Barbers
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();

        ServiceOptions = new SelectList(services, nameof(Service.Id), nameof(Service.Name));
        BarberOptions  = new SelectList(barbers, nameof(Barber.Id), nameof(Barber.Name));
    }

    // Handler chamado via fetch (?handler=AvailableSlots)
    public async Task<IActionResult> OnGetAvailableSlotsAsync(int barberId, int serviceId, string date)
    {
        // Tenta ISO (yyyy-MM-dd), depois pt-BR (dd/MM/yyyy) e por fim parsing padrão
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) &&
            !DateOnly.TryParseExact(date, "dd/MM/yyyy", new CultureInfo("pt-BR"), DateTimeStyles.None, out parsedDate) &&
            !DateOnly.TryParse(date, out parsedDate))
        {
            return new JsonResult(new { error = "Data inválida" });
        }

        var service = await _db.Services.FindAsync(serviceId);
        if (service is null || !service.IsActive)
            return new JsonResult(new { error = "Serviço inválido" });

        var slots = await _availabilityService.GetAvailableTimeSlotsAsync(
            barberId, parsedDate, service.DurationMinutes);

        return new JsonResult(new
        {
            slots = slots.Select(t => new { value = t.ToString("HH:mm"), text = t.ToString("HH:mm") })
        });
    }
}
