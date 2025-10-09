using BarberNetBooking.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberNetBooking.Pages.Admin;

public class LoginModel : PageModel
{
    [BindProperty]
    public string Pin { get; set; } = string.Empty;

    private readonly IConfiguration _cfg;
    public LoginModel(IConfiguration cfg) => _cfg = cfg;

    public void OnGet() { }

    public IActionResult OnPost(string? returnUrl = "/Admin")
    {
        var expected = _cfg["Admin:Pin"];
        if (!string.IsNullOrWhiteSpace(expected) && Pin == expected)
        {
            Response.Cookies.Append("bn_admin", "ok", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
            return LocalRedirect(returnUrl ?? "/Admin");
        }

        ModelState.AddModelError(string.Empty, "PIN inválido");
        return Page();
    }
}