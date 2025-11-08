using BarberNetBooking.Data;
using BarberNetBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Services;

public interface IAvailabilityService
{
    Task<(bool IsAvailable, string? ErrorMessage)> IsBarberAvailableAsync(
        int barberId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeAppointmentId = null);
    
    Task<List<TimeOnly>> GetAvailableTimeSlotsAsync(int barberId, DateOnly date, int serviceDurationMinutes);
}

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _db;

    public AvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Verifica se um barbeiro está disponível em uma data e horário específicos
    /// </summary>
    public async Task<(bool IsAvailable, string? ErrorMessage)> IsBarberAvailableAsync(
        int barberId, 
        DateOnly date, 
        TimeOnly startTime, 
        TimeOnly endTime,
        int? excludeAppointmentId = null)
    {
        // 1. Verifica se existe folga (time off) nesta data
        var hasTimeOff = await _db.TimeOffs
            .AnyAsync(t => t.BarberId == barberId && t.Date == date);

        if (hasTimeOff)
        {
            var timeOff = await _db.TimeOffs
                .FirstOrDefaultAsync(t => t.BarberId == barberId && t.Date == date);
            return (false, $"Barbeiro não disponível nesta data (folga{(string.IsNullOrEmpty(timeOff?.Reason) ? "" : $": {timeOff.Reason}")}).");
        }

        // 2. Obtém as regras de horário de trabalho para o dia da semana
        var dayOfWeek = date.DayOfWeek;
        var workingHour = await _db.WorkingHours
            .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dayOfWeek);

        // Se não há regra configurada, assume horário padrão (9h-18h)
        if (workingHour == null)
        {
            workingHour = new BarberWorkingHour
            {
                BarberId = barberId,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0),
                IsClosed = dayOfWeek == DayOfWeek.Sunday
            };
        }

        if (workingHour.IsClosed)
        {
            return (false, $"Barbeiro não atende neste dia da semana ({GetDayName(dayOfWeek)}).");
        }

        // 3. Verifica se o horário solicitado está dentro do horário de trabalho
        if (startTime < workingHour.StartTime || endTime > workingHour.EndTime)
        {
            return (false, $"Horário fora do expediente. Atendimento: {workingHour.StartTime:HH\\:mm} às {workingHour.EndTime:HH\\:mm}.");
        }

        // 4. Verifica conflito com horário de almoço
        if (workingHour.LunchStartTime.HasValue && workingHour.LunchEndTime.HasValue)
        {
            var lunchStart = workingHour.LunchStartTime.Value;
            var lunchEnd = workingHour.LunchEndTime.Value;

            // Verifica se o agendamento solicitado sobrepõe o horário de almoço
            var overlapsLunch = (startTime >= lunchStart && startTime < lunchEnd) ||  // Início durante almoço
                               (endTime > lunchStart && endTime <= lunchEnd) ||       // Fim durante almoço
                               (startTime <= lunchStart && endTime >= lunchEnd);      // Engloba almoço

            if (overlapsLunch)
            {
                return (false, $"Horário de almoço. Intervalo: {lunchStart:HH\\:mm} às {lunchEnd:HH\\:mm}.");
            }
        }

        // 5. Verifica conflitos com outros agendamentos confirmados
        var hasConflict = await _db.Appointments
            .Where(a => a.BarberId == barberId 
                     && a.Date == date 
                     && a.Status == AppointmentStatus.Confirmed
                     && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
            .AnyAsync(a => 
                (startTime >= a.StartTime && startTime < a.EndTime) ||
                (endTime > a.StartTime && endTime <= a.EndTime) ||
                (startTime <= a.StartTime && endTime >= a.EndTime)
            );

        if (hasConflict)
        {
            return (false, "Já existe um agendamento confirmado neste horário.");
        }

        return (true, null);
    }

    /// <summary>
    /// Obtém os horários disponíveis para um barbeiro em uma data específica
    /// </summary>
    public async Task<List<TimeOnly>> GetAvailableTimeSlotsAsync(
        int barberId, 
        DateOnly date, 
        int serviceDurationMinutes)
    {
        var availableSlots = new List<TimeOnly>();

        // Verifica se há folga
        var hasTimeOff = await _db.TimeOffs
            .AnyAsync(t => t.BarberId == barberId && t.Date == date);

        if (hasTimeOff)
        {
            return availableSlots;
        }

        // Obtém horário de trabalho
        var dayOfWeek = date.DayOfWeek;
        var workingHour = await _db.WorkingHours
            .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dayOfWeek);

        // Se não há regra configurada, assume horário padrão
        if (workingHour == null)
        {
            workingHour = new BarberWorkingHour
            {
                BarberId = barberId,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0),
                IsClosed = dayOfWeek == DayOfWeek.Sunday
            };
        }

        if (workingHour.IsClosed)
        {
            return availableSlots;
        }

        // Obtém agendamentos existentes confirmados
        var existingAppointments = await _db.Appointments
            .Where(a => a.BarberId == barberId 
                     && a.Date == date 
                     && a.Status == AppointmentStatus.Confirmed)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        // Gera slots de 15 em 15 minutos
        var currentTime = workingHour.StartTime;
        var serviceDuration = TimeSpan.FromMinutes(serviceDurationMinutes);
        var endTimeSpan = workingHour.EndTime.ToTimeSpan();

        while (currentTime.ToTimeSpan().Add(serviceDuration) <= endTimeSpan)
        {
            var slotEndTime = TimeOnly.FromTimeSpan(currentTime.ToTimeSpan().Add(serviceDuration));
            
            // Verifica conflito com horário de almoço
            bool overlapsLunch = false;
            if (workingHour.LunchStartTime.HasValue && workingHour.LunchEndTime.HasValue)
            {
                var lunchStart = workingHour.LunchStartTime.Value;
                var lunchEnd = workingHour.LunchEndTime.Value;

                overlapsLunch = (currentTime >= lunchStart && currentTime < lunchEnd) ||
                               (slotEndTime > lunchStart && slotEndTime <= lunchEnd) ||
                               (currentTime <= lunchStart && slotEndTime >= lunchEnd);
            }

            // Verifica se este slot conflita com algum agendamento existente
            var hasConflict = existingAppointments.Any(a =>
                (currentTime >= a.StartTime && currentTime < a.EndTime) ||
                (slotEndTime > a.StartTime && slotEndTime <= a.EndTime) ||
                (currentTime <= a.StartTime && slotEndTime >= a.EndTime)
            );

            if (!hasConflict && !overlapsLunch)
            {
                availableSlots.Add(currentTime);
            }

            // Avança 15 minutos
            currentTime = TimeOnly.FromTimeSpan(currentTime.ToTimeSpan().Add(TimeSpan.FromMinutes(15)));
        }

        return availableSlots;
    }

    private static string GetDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "Domingo",
            DayOfWeek.Monday => "Segunda-feira",
            DayOfWeek.Tuesday => "Terça-feira",
            DayOfWeek.Wednesday => "Quarta-feira",
            DayOfWeek.Thursday => "Quinta-feira",
            DayOfWeek.Friday => "Sexta-feira",
            DayOfWeek.Saturday => "Sábado",
            _ => dayOfWeek.ToString()
        };
    }
}