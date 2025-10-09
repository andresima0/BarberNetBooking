using System.ComponentModel.DataAnnotations;
using BarberNetBooking.Data;
using BarberNetBooking.Infrastructure;
using BarberNetBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Pages.Admin.Barbers;

[AdminAuthorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Barber> Barbers { get; set; } = new();
    public Dictionary<int,int> FutureCounts { get; set; } = new();

    [BindProperty]
    public BarberInput Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public class BarberInput
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(80, ErrorMessage = "O nome deve ter no máximo 80 caracteres.")]
        public string Name { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Barbers = await _db.Barbers.OrderBy(b => b.Name).ToListAsync();
        FutureCounts = await _db.Appointments
            .Where(a => a.Date >= today && a.Status == AppointmentStatus.Confirmed)
            .GroupBy(a => a.BarberId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Verifique os campos e tente novamente.";
            await OnGetAsync();
            return Page();
        }

        var trimmedName = Input.Name.Trim();
        
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ErrorMessage = "O nome do barbeiro não pode estar vazio.";
            await OnGetAsync();
            return Page();
        }

        _db.Barbers.Add(new Barber { Name = trimmedName });
        await _db.SaveChangesAsync();
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveChangesAsync(int barberId)
    {
        var barberToUpdate = await _db.Barbers.FindAsync(barberId);

        if (barberToUpdate == null)
        {
            ErrorMessage = "Barbeiro não encontrado.";
            await OnGetAsync();
            return Page();
        }

        // Pega o valor específico deste barbeiro
        var name = Request.Form[$"Barber[{barberId}].Name"].ToString();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "O nome do barbeiro não pode estar vazio.";
            await OnGetAsync();
            return Page();
        }

        barberToUpdate.Name = name.Trim();
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var barberToDelete = await _db.Barbers.FindAsync(id);

        if (barberToDelete == null)
        {
            ErrorMessage = "Barbeiro não encontrado.";
            await OnGetAsync();
            return Page();
        }

        // Verifica se o barbeiro tem agendamentos futuros
        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasFutureAppointments = await _db.Appointments
            .AnyAsync(a => a.BarberId == id && a.Date >= today && a.Status == AppointmentStatus.Confirmed);

        if (hasFutureAppointments)
        {
            ErrorMessage = "Não é possível remover um barbeiro que possui agendamentos futuros.";
            await OnGetAsync();
            return Page();
        }

        _db.Barbers.Remove(barberToDelete);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }
}