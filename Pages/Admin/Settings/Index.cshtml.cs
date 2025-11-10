using System.ComponentModel.DataAnnotations;
using BarberNetBooking.Data;
using BarberNetBooking.Infrastructure;
using BarberNetBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BarberNetBooking.Pages.Admin.Settings;

[AdminAuthorize]
public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;

    public IndexModel(IConfiguration configuration, IWebHostEnvironment env, AppDbContext db)
    {
        _configuration = configuration;
        _env = env;
        _db = db;
    }

    [BindProperty] public PinChangeInput Input { get; set; } = new();
    [BindProperty] public ShopInfoInputModel ShopInfo { get; set; } = new();
    [BindProperty] public IFormFile? LogoFile { get; set; }
    [BindProperty] public bool RemoveLogo { get; set; }

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

    public class ShopInfoInputModel
    {
        [StringLength(100)]
        public string? SiteName { get; set; } 
        
        [StringLength(512)]
        public string? LogoPath { get; set; } 
        
        [StringLength(200, ErrorMessage = "O slogan deve ter no máximo 200 caracteres")]
        public string? Slogan { get; set; }

        [StringLength(2000, ErrorMessage = "O texto deve ter no máximo 2000 caracteres")]
        public string? AboutUs { get; set; }

        [StringLength(100)]
        public string? Phone { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? Instagram { get; set; }

        [StringLength(100)]
        public string? Facebook { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        // Mantido como 2 (sigla), mas agora normalizamos para UPPER no post
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Use a sigla do estado (ex: SP)")]
        public string? State { get; set; }

        [StringLength(10)]
        public string? ZipCode { get; set; }

        // Relaxado: URLs de embed do Google costumam ser longas
        [StringLength(4096, ErrorMessage = "O link do mapa é muito longo")]
        [Url(ErrorMessage = "Informe um link de mapa válido (URL)")]
        public string? MapEmbedUrl { get; set; }
    }

    public async Task OnGetAsync()
    {
        // Carrega as informações da barbearia
        var shopInfo = await _db.ShopInfos.AsNoTracking().FirstOrDefaultAsync();

        if (shopInfo != null)
        {
            ShopInfo = new ShopInfoInputModel
            {
                SiteName  = shopInfo.SiteName,
                LogoPath  = shopInfo.LogoPath,
                Slogan = shopInfo.Slogan,
                AboutUs = shopInfo.AboutUs,
                Phone = shopInfo.Phone,
                Email = shopInfo.Email,
                Instagram = shopInfo.Instagram,
                Facebook = shopInfo.Facebook,
                Address = shopInfo.Address,
                City = shopInfo.City,
                State = shopInfo.State,
                ZipCode = shopInfo.ZipCode,
                MapEmbedUrl = shopInfo.MapEmbedUrl
            };
        }
    }

    public async Task<IActionResult> OnPostSaveShopInfoAsync()
    {
        // Ignore a seção de PIN
        ModelState.Remove("Input.CurrentPin");
        ModelState.Remove("Input.NewPin");
        ModelState.Remove("Input.ConfirmPin");

        // Normalização ANTES da validação
        ShopInfo.Slogan      = ShopInfo.Slogan?.Trim();
        ShopInfo.AboutUs     = ShopInfo.AboutUs?.Trim();
        ShopInfo.Phone       = ShopInfo.Phone?.Trim();
        ShopInfo.Email       = ShopInfo.Email?.Trim();
        ShopInfo.Instagram   = ShopInfo.Instagram?.Trim();
        ShopInfo.Facebook    = ShopInfo.Facebook?.Trim();
        ShopInfo.Address     = ShopInfo.Address?.Trim();
        ShopInfo.City        = ShopInfo.City?.Trim();
        ShopInfo.State       = ShopInfo.State?.Trim()?.ToUpperInvariant();
        ShopInfo.ZipCode     = ShopInfo.ZipCode?.Trim();
        ShopInfo.MapEmbedUrl = ShopInfo.MapEmbedUrl?.Trim();

        // Revalida com dados normalizados
        ModelState.Clear();
        if (!TryValidateModel(ShopInfo, nameof(ShopInfo)))
        {
            ErrorMessage = "Por favor, corrija os erros no formulário.";
            await OnGetAsync(); // Recarrega dados atuais para a tela
            return Page();
        }

        // Persiste no banco (cria ou atualiza o único registro)
        var entity = await _db.ShopInfos.FirstOrDefaultAsync();
        if (entity == null)
        {
            entity = new BarberNetBooking.Models.ShopInfo();
            _db.ShopInfos.Add(entity);
        }
        
        // ===== Branding =====
        entity.SiteName = string.IsNullOrWhiteSpace(ShopInfo.SiteName)
            ? null
            : ShopInfo.SiteName.Trim();

        // Upload/remoção de logo
        if (RemoveLogo && !string.IsNullOrWhiteSpace(entity.LogoPath))
        {
            TryDeleteFileFromWwwRoot(entity.LogoPath);
            entity.LogoPath = null;
        }
        else if (LogoFile != null && LogoFile.Length > 0)
        {
            var okTypes = new[] { "image/png", "image/jpeg", "image/webp", "image/svg+xml" };
            if (!okTypes.Contains(LogoFile.ContentType))
            {
                ErrorMessage = "Formato de imagem inválido. Use PNG, JPG, WEBP ou SVG.";
                await OnGetAsync();
                return Page();
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "branding");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(LogoFile.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            var fileName = $"logo-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(uploadsDir, fileName);
            using (var fs = System.IO.File.Create(fullPath))
                await LogoFile.CopyToAsync(fs);

            // remove o anterior
            if (!string.IsNullOrWhiteSpace(entity.LogoPath))
                TryDeleteFileFromWwwRoot(entity.LogoPath);

            entity.LogoPath = $"/uploads/branding/{fileName}";
        }
        // =====================

        // Demais campos
        entity.Slogan      = ShopInfo.Slogan;
        entity.AboutUs     = ShopInfo.AboutUs;
        entity.Phone       = ShopInfo.Phone;
        entity.Email       = ShopInfo.Email;
        entity.Instagram   = ShopInfo.Instagram;
        entity.Facebook    = ShopInfo.Facebook;
        entity.Address     = ShopInfo.Address;
        entity.City        = ShopInfo.City;
        entity.State       = ShopInfo.State;
        entity.ZipCode     = ShopInfo.ZipCode;
        entity.MapEmbedUrl = ShopInfo.MapEmbedUrl;
        entity.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        SuccessMessage = "Informações salvas com sucesso.";
        await OnGetAsync(); // Recarrega para refletir o que foi salvo
        return Page();
    }

    public async Task<IActionResult> OnPostChangePinAsync()
    {
        // Valida apenas os campos de Input (PIN)
        ModelState.Remove("ShopInfo.Slogan");
        ModelState.Remove("ShopInfo.AboutUs");
        ModelState.Remove("ShopInfo.Phone");
        ModelState.Remove("ShopInfo.Email");
        ModelState.Remove("ShopInfo.Instagram");
        ModelState.Remove("ShopInfo.Facebook");
        ModelState.Remove("ShopInfo.Address");
        ModelState.Remove("ShopInfo.City");
        ModelState.Remove("ShopInfo.State");
        ModelState.Remove("ShopInfo.ZipCode");
        ModelState.Remove("ShopInfo.MapEmbedUrl");

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Por favor, corrija os erros no formulário.";
            await OnGetAsync();
            return Page();
        }

        // Valida PIN atual
        var currentPin = _configuration["Admin:Pin"];
        if (string.IsNullOrWhiteSpace(currentPin) || Input.CurrentPin != currentPin)
        {
            ErrorMessage = "PIN atual incorreto.";
            await OnGetAsync();
            return Page();
        }

        // Valida novo PIN diferente do atual
        if (Input.NewPin == Input.CurrentPin)
        {
            ErrorMessage = "O novo PIN deve ser diferente do atual.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            // Tenta usar user-secrets primeiro (em desenvolvimento)
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

                        TempData["LoginMessage"] = "PIN alterado com sucesso! Faça login com o novo PIN.";
                        return RedirectToPage("/Admin/Login");
                    }
                }
                catch
                {
                    // Se falhar com user-secrets, tenta atualizar appsettings.json
                }
            }

            // Atualiza o appsettings.json (fallback ou produção)
            await UpdateAppSettingsJsonAsync();

            // Invalida o cookie atual
            Response.Cookies.Delete("bn_admin", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            TempData["LoginMessage"] = "PIN alterado com sucesso! Faça login com o novo PIN.";
            return RedirectToPage("/Admin/Login");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao alterar PIN: {ex.Message}";
            await OnGetAsync();
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
        using (var writer =
               new System.Text.Json.Utf8JsonWriter(stream,
                   new System.Text.Json.JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            bool adminFound = false;

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "Admin")
                {
                    adminFound = true;
                    writer.WriteStartObject("Admin");

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
    
    private void TryDeleteFileFromWwwRoot(string relativePath)
    {
        try
        {
            var path = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            var full = Path.Combine(_env.WebRootPath, path);
            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }
        catch { /* não quebra a página se falhar */ }
    }
}
