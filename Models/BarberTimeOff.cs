namespace BarberNetBooking.Models;

public class BarberTimeOff
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }

    // Navigation property
    public Barber? Barber { get; set; }
}