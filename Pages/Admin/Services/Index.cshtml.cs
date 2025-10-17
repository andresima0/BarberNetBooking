using BarberNetBooking.Data;
using BarberNetBooking.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberNetBooking.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BarberNetBooking.Pages.Admin.Services
{
    [AdminAuthorize] // CRÍTICO: Protege a página
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public IndexModel(AppDbContext db) => _db = db;

        // Garantir que Services nunca seja nulo
        public List<Service> Services { get; set; } = new();

        // Garantir que ServiceOptions e BarberOptions nunca sejam nulos
        public SelectList ServiceOptions { get; set; } = new SelectList(Enumerable.Empty<Service>());
        public SelectList BarberOptions { get; set; } = new SelectList(Enumerable.Empty<Barber>());

        [BindProperty] public Service Input { get; set; } = new Service();

        [TempData]
        public string? SuccessMessage { get; set; }
        
        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Carrega a lista de serviços, garantindo que não seja nula
            Services = await _db.Services.AsNoTracking().OrderBy(s => s.Price).ToListAsync();

            // Preenche as opções de serviços e barbeiros com listas vazias, caso não haja resultados
            ServiceOptions = new SelectList(Services, nameof(Service.Id), nameof(Service.Name));

            var barbers = await _db.Barbers.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
            BarberOptions = new SelectList(barbers, nameof(Barber.Id), nameof(Barber.Name));
        }

        public async Task<IActionResult> OnPostSaveChangesAsync(int serviceId)
        {
            var serviceToUpdate = await _db.Services.FindAsync(serviceId);

            if (serviceToUpdate == null)
            {
                ErrorMessage = "Serviço não encontrado.";
                await OnGetAsync();
                return Page();
            }

            // Pega os valores específicos deste serviço
            var name = Request.Form[$"Service[{serviceId}].Name"].ToString();
            var priceStr = Request.Form[$"Service[{serviceId}].Price"].ToString();
            var durationStr = Request.Form[$"Service[{serviceId}].DurationMinutes"].ToString();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "O nome do serviço não pode estar vazio.";
                await OnGetAsync();
                return Page();
            }

            serviceToUpdate.Name = name.Trim();
            serviceToUpdate.Price = decimal.TryParse(priceStr, out var price) ? price : serviceToUpdate.Price;
            serviceToUpdate.DurationMinutes = int.TryParse(durationStr, out var duration) ? duration : serviceToUpdate.DurationMinutes;

            await _db.SaveChangesAsync();
            SuccessMessage = "Serviço atualizado com sucesso!";
    
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var serviceToDelete = await _db.Services.FindAsync(id);

            if (serviceToDelete != null)
            {
                // Verifica se há agendamentos usando este serviço
                var hasAppointments = await _db.Appointments
                    .AnyAsync(a => a.ServiceId == id && a.Status == AppointmentStatus.Confirmed);

                if (hasAppointments)
                {
                    ErrorMessage = "Não é possível remover um serviço que possui agendamentos.";
                    await OnGetAsync();
                    return Page();
                }

                _db.Services.Remove(serviceToDelete);
                await _db.SaveChangesAsync();
                SuccessMessage = "Serviço removido com sucesso!";
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Verifique os campos e tente novamente.";
                await OnGetAsync();
                return Page();
            }

            // Validação adicional
            if (Input.Price <= 0)
            {
                ErrorMessage = "O preço deve ser maior que zero.";
                await OnGetAsync();
                return Page();
            }

            if (Input.DurationMinutes <= 0)
            {
                ErrorMessage = "A duração deve ser maior que zero.";
                await OnGetAsync();
                return Page();
            }

            _db.Services.Add(Input);
            await _db.SaveChangesAsync();

            SuccessMessage = "Novo serviço adicionado!";
            ModelState.Clear();
            Input = new Service(); // Limpa o formulário após sucesso
            
            return RedirectToPage("./Index");
        }
    }
}