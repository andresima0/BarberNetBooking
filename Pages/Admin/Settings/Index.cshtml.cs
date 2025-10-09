using System.ComponentModel.DataAnnotations;
using BarberNetBooking.Data;
using BarberNetBooking.Infrastructure;
using BarberNetBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BarberNetBooking.Pages.Admin.Settings;

[AdminAuthorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Range(5, 120)] public int SlotMinutes { get; set; } = 30;
    }

    public async Task OnGetAsync()
    {
        var s = await _db.Settings.FirstOrDefaultAsync() ?? new ShopSetting();
        Input.SlotMinutes = s.SlotMinutes;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }
        var s = await _db.Settings.FirstOrDefaultAsync();
        if (s == null)
        {
            s = new ShopSetting { SlotMinutes = Input.SlotMinutes };
            _db.Settings.Add(s);
        }
        else s.SlotMinutes = Input.SlotMinutes;

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}