using System.ComponentModel.DataAnnotations;

namespace BarberNetBooking.Models;

public class BarberTimeOff
{
    public int Id { get; set; }
    public int BarberId { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateOnly Date { get; set; }

    public string? Reason { get; set; }

    // Navigation property
    public Barber? Barber { get; set; }
}