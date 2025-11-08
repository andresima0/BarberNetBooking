using System.ComponentModel.DataAnnotations;
using BarberNetBooking.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BarberNetBooking.Pages.Admin.Settings;

[AdminAuthorize]
public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public IndexModel(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    [BindProperty] 
    public PinChangeInput Input { get; set; } = new();

    // Mudança: Não usar [TempData] - usar propriedades normais
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public class PinChangeInput
    {
        [Required(ErrorMessage = "Digite o PIN atual")]
        [Display(Name = "PIN Atual")]
        public string CurrentPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Digite o novo PIN")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "O PIN deve ter entre 4 e 20 caracteres")]
        [Display(Name = "Novo PIN")]
        public string NewPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme o novo PIN")]
        [Compare("NewPin", ErrorMessage = "Os PINs não conferem")]
        [Display(Name = "Confirmar Novo PIN")]
        public string ConfirmPin { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // Apenas exibe a página - mensagens sempre limpas
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Por favor, corrija os erros no formulário.";
            return Page();
        }

        // Valida PIN atual
        var currentPin = _configuration["Admin:Pin"];
        if (string.IsNullOrWhiteSpace(currentPin) || Input.CurrentPin != currentPin)
        {
            ErrorMessage = "PIN atual incorreto.";
            return Page();
        }

        // Valida novo PIN diferente do atual
        if (Input.NewPin == Input.CurrentPin)
        {
            ErrorMessage = "O novo PIN deve ser diferente do atual.";
            return Page();
        }

        try
        {
            // Tenta usar user-secrets primeiro (se disponível em desenvolvimento)
            if (_env.IsDevelopment())
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"user-secrets set \"Admin:Pin\" \"{Input.NewPin}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = _env.ContentRootPath
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        // Invalida o cookie
                        Response.Cookies.Delete("bn_admin", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict
                        });

                        // Usa TempData apenas para passar para a página de login
                        TempData["LoginMessage"] = "PIN alterado com sucesso! Faça login com o novo PIN.";
                        return RedirectToPage("/Admin/Login");
                    }
                }
                catch
                {
                    // Se falhar com user-secrets, tenta atualizar appsettings.json
                }
            }

            // Atualiza o appsettings.json (fallback ou em produção)
            await UpdateAppSettingsJsonAsync();
            
            // Invalida o cookie atual
            Response.Cookies.Delete("bn_admin", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            // Usa TempData apenas para passar para a página de login
            TempData["LoginMessage"] = "PIN alterado com sucesso! Faça login com o novo PIN.";
            return RedirectToPage("/Admin/Login");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao alterar PIN: {ex.Message}";
            return Page();
        }
    }

    private async Task UpdateAppSettingsJsonAsync()
    {
        var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        
        if (!System.IO.File.Exists(appSettingsPath))
        {
            throw new FileNotFoundException("Arquivo appsettings.json não encontrado");
        }

        var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
        
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            
            bool adminFound = false;
            
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "Admin")
                {
                    adminFound = true;
                    writer.WriteStartObject("Admin");
                    
                    // Copia outras propriedades do Admin se existirem
                    foreach (var adminProp in property.Value.EnumerateObject())
                    {
                        if (adminProp.Name == "Pin")
                        {
                            writer.WriteString("Pin", Input.NewPin);
                        }
                        else
                        {
                            adminProp.WriteTo(writer);
                        }
                    }
                    
                    writer.WriteEndObject();
                }
                else
                {
                    property.WriteTo(writer);
                }
            }
            
            // Se não existe seção Admin, cria
            if (!adminFound)
            {
                writer.WriteStartObject("Admin");
                writer.WriteString("Pin", Input.NewPin);
                writer.WriteEndObject();
            }
            
            writer.WriteEndObject();
        }
        
        var newJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        await System.IO.File.WriteAllTextAsync(appSettingsPath, newJson);
    }
}