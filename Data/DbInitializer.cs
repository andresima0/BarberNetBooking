using BarberNetBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new Service { Name = "Corte masculino", Price = 45.00m, DurationMinutes = 30, IsActive = true },
                new Service { Name = "Barba",            Price = 30.00m, DurationMinutes = 30, IsActive = true },
                new Service { Name = "Corte + Barba",    Price = 70.00m, DurationMinutes = 60, IsActive = true },
                new Service { Name = "Sobrancelha",      Price = 15.00m, DurationMinutes = 15, IsActive = true }
            );
        }

        if (!await db.Barbers.AnyAsync())
        {
            db.Barbers.AddRange(
                new Barber { Name = "Luiz" },
                new Barber { Name = "Carlos" },
                new Barber { Name = "Rafa" }
            );
        }

        await db.SaveChangesAsync();
    }
}