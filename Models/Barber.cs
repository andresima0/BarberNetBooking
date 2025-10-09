namespace BarberNetBooking.Models;

public class Barber
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<BarberWorkingHour> WorkingHours { get; set; } = new List<BarberWorkingHour>();
    public ICollection<BarberTimeOff> TimeOffs { get; set; } = new List<BarberTimeOff>();
}