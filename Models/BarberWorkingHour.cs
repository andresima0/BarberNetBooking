namespace BarberNetBooking.Models;

public class BarberWorkingHour
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsClosed { get; set; }

    // Navigation property
    public Barber? Barber { get; set; }
}