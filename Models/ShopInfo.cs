using System.ComponentModel.DataAnnotations;

namespace BarberNetBooking.Models;

public class ShopInfo
{
    public int Id { get; set; }
    
    [StringLength(100)]
    public string? SiteName { get; set; }

    // caminho relativo em wwwroot (wwwroot/logo_dark.png")
    [StringLength(512)]
    public string? LogoPath { get; set; }
    
    [StringLength(200)]
    public string? Slogan { get; set; }
    
    [StringLength(2000)]
    public string? AboutUs { get; set; }
    
    [StringLength(100)]
    public string? Phone { get; set; }
    
    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [StringLength(100)]
    public string? Instagram { get; set; }
    
    [StringLength(100)]
    public string? Facebook { get; set; }
    
    [StringLength(300)]
    public string? Address { get; set; }
    
    [StringLength(50)]
    public string? City { get; set; }
    
    [StringLength(2)]
    public string? State { get; set; }
    
    [StringLength(10)]
    public string? ZipCode { get; set; }
    
    // Para incorporar mapa do Google Maps
    [StringLength(4096, ErrorMessage = "O link do mapa é muito longo")]
    [Url(ErrorMessage = "Informe um link de mapa válido (URL)")]
    public string? MapEmbedUrl { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}