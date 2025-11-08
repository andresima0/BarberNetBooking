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
    
    [BindProperty(SupportsGet = true)] 
    public int SelectedBarberId { get; set; }

    [BindProperty] 
    public List<WeeklyInput> Week { get; set; } = new();
    
    public List<BarberTimeOff> TimeOffs { get; set; } = new();

    [BindProperty] 
    public TimeOffInput NewTimeOff { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData]
    public string? ErrorMessage { get; set; }

    public class WeeklyInput
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; } = new(9, 0);
        public TimeOnly EndTime { get; set; } = new(18, 0);
        public TimeOnly? LunchStartTime { get; set; } = new(12, 0);
        public TimeOnly? LunchEndTime { get; set; } = new(13, 0);
        public bool IsClosed { get; set; }
    }

    public class TimeOffInput
    {
        [Required(ErrorMessage = "A data é obrigatória")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        
        public string? Reason { get; set; }
    }

    public async Task OnGetAsync()
    {
        var barbers = await _db.Barbers.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        
        if (SelectedBarberId == 0 && barbers.Any()) 
        {
            SelectedBarberId = barbers.First().Id;
        }
        
        // Define o barbeiro selecionado no SelectList
        BarberOptions = new SelectList(barbers, nameof(Barber.Id), nameof(Barber.Name), SelectedBarberId);

        await LoadWeekAsync();
        
        TimeOffs = await _db.TimeOffs
            .Where(t => t.BarberId == SelectedBarberId)
            .OrderBy(t => t.Date)
            .ToListAsync();
    }

    private async Task LoadWeekAsync()
    {
        Week.Clear();
        var rules = await _db.WorkingHours
            .Where(w => w.BarberId == SelectedBarberId)
            .ToListAsync();

        // Ordena os dias da semana começando pela segunda-feira
        var orderedDays = new[] 
        { 
            DayOfWeek.Monday, 
            DayOfWeek.Tuesday, 
            DayOfWeek.Wednesday, 
            DayOfWeek.Thursday, 
            DayOfWeek.Friday, 
            DayOfWeek.Saturday, 
            DayOfWeek.Sunday 
        };

        foreach (var day in orderedDays)
        {
            var rule = rules.FirstOrDefault(x => x.DayOfWeek == day);
            Week.Add(new WeeklyInput
            {
                DayOfWeek = day,
                StartTime = rule?.StartTime ?? new TimeOnly(9, 0),
                EndTime = rule?.EndTime ?? new TimeOnly(18, 0),
                LunchStartTime = rule?.LunchStartTime ?? new TimeOnly(12, 0),
                LunchEndTime = rule?.LunchEndTime ?? new TimeOnly(13, 0),
                IsClosed = rule?.IsClosed ?? (day == DayOfWeek.Sunday)
            });
        }
    }

    public async Task<IActionResult> OnPostSaveWeekAsync()
    {
        // IMPORTANTE: Recarrega o SelectedBarberId do form hidden
        if (SelectedBarberId == 0)
        {
            ErrorMessage = "Selecione um barbeiro.";
            await OnGetAsync();
            return Page();
        }

        // Valida horários
        foreach (var item in Week)
        {
            if (!item.IsClosed && item.StartTime >= item.EndTime)
            {
                ErrorMessage = $"Horário inválido para {GetDayName(item.DayOfWeek)}: início deve ser antes do fim.";
                await OnGetAsync();
                return Page();
            }

            // Valida horário de almoço
            if (item.LunchStartTime.HasValue && item.LunchEndTime.HasValue)
            {
                if (item.LunchStartTime >= item.LunchEndTime)
                {
                    ErrorMessage = $"Horário de almoço inválido para {GetDayName(item.DayOfWeek)}: início deve ser antes do fim.";
                    await OnGetAsync();
                    return Page();
                }

                if (item.LunchStartTime < item.StartTime || item.LunchEndTime > item.EndTime)
                {
                    ErrorMessage = $"O horário de almoço de {GetDayName(item.DayOfWeek)} deve estar dentro do horário de trabalho.";
                    await OnGetAsync();
                    return Page();
                }
            }

            // Se apenas um dos campos de almoço foi preenchido
            if (item.LunchStartTime.HasValue != item.LunchEndTime.HasValue)
            {
                ErrorMessage = $"Para {GetDayName(item.DayOfWeek)}: preencha ambos os horários de almoço ou deixe ambos vazios.";
                await OnGetAsync();
                return Page();
            }
        }

        var existing = await _db.WorkingHours
            .Where(w => w.BarberId == SelectedBarberId)
            .ToListAsync();

        foreach (var item in Week)
        {
            // Busca a regra existente para ESTE barbeiro e ESTE dia
            var rule = existing.FirstOrDefault(x => 
                x.BarberId == SelectedBarberId && 
                x.DayOfWeek == item.DayOfWeek);
            
            if (rule == null)
            {
                // Cria nova regra para ESTE barbeiro
                _db.WorkingHours.Add(new BarberWorkingHour
                {
                    BarberId = SelectedBarberId,
                    DayOfWeek = item.DayOfWeek,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    LunchStartTime = item.LunchStartTime,
                    LunchEndTime = item.LunchEndTime,
                    IsClosed = item.IsClosed
                });
            }
            else
            {
                // Atualiza regra existente
                rule.StartTime = item.StartTime;
                rule.EndTime = item.EndTime;
                rule.LunchStartTime = item.LunchStartTime;
                rule.LunchEndTime = item.LunchEndTime;
                rule.IsClosed = item.IsClosed;
            }
        }

        await _db.SaveChangesAsync();
        SuccessMessage = $"Horários do barbeiro salvos com sucesso!";
        
        return RedirectToPage(new { SelectedBarberId });
    }

    public async Task<IActionResult> OnPostAddTimeOffAsync()
    {
        if (SelectedBarberId == 0)
        {
            ErrorMessage = "Selecione um barbeiro.";
            await OnGetAsync();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Preencha a data corretamente.";
            await OnGetAsync();
            return Page();
        }

        // Verifica se já existe folga nesta data
        var existingTimeOff = await _db.TimeOffs
            .FirstOrDefaultAsync(t => t.BarberId == SelectedBarberId && t.Date == NewTimeOff.Date);

        if (existingTimeOff != null)
        {
            ErrorMessage = "Já existe uma folga cadastrada para esta data.";
            await OnGetAsync();
            return Page();
        }

        _db.TimeOffs.Add(new BarberTimeOff
        {
            BarberId = SelectedBarberId,
            Date = NewTimeOff.Date,
            Reason = NewTimeOff.Reason?.Trim()
        });

        await _db.SaveChangesAsync();
        SuccessMessage = "Folga adicionada com sucesso!";
        
        return RedirectToPage(new { SelectedBarberId });
    }

    public async Task<IActionResult> OnPostDeleteTimeOffAsync(int id)
    {
        var timeOff = await _db.TimeOffs.FindAsync(id);
        
        if (timeOff != null)
        {
            _db.TimeOffs.Remove(timeOff);
            await _db.SaveChangesAsync();
            SuccessMessage = "Folga removida com sucesso!";
        }
        
        return RedirectToPage(new { SelectedBarberId });
    }

    public string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "Domingo",
        DayOfWeek.Monday => "Segunda-feira",
        DayOfWeek.Tuesday => "Terça-feira",
        DayOfWeek.Wednesday => "Quarta-feira",
        DayOfWeek.Thursday => "Quinta-feira",
        DayOfWeek.Friday => "Sexta-feira",
        DayOfWeek.Saturday => "Sábado",
        _ => day.ToString()
    };
}