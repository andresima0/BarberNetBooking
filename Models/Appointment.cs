using System.ComponentModel.DataAnnotations;

namespace BarberNetBooking.Models;

public class Appointment
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int BarberId { get; set; }
    
    // Campos de data/hora
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateOnly Date { get; set; }
    
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public TimeOnly StartTime { get; set; }
    
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public TimeOnly EndTime { get; set; }
    
    // Campo legado (mantém compatibilidade)
    public TimeSpan Time { get; set; }
    
    // Dados do cliente
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Status e metadata
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Service? Service { get; set; }
    public Barber? Barber { get; set; }
}