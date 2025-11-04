using System.Net;
using System.Net.Mail;
using BarberNetBooking.Models;
using Microsoft.Extensions.Configuration;

namespace BarberNetBooking.Services;

public interface IEmailService
{
    Task SendAppointmentConfirmationAsync(Appointment appointment, Service service, Barber barber);
    Task SendAppointmentCancellationAsync(Appointment appointment, Service service, Barber barber);
    Task SendAppointmentRescheduleAsync(Appointment appointment, Service service, Barber barber, DateOnly oldDate, TimeOnly oldTime);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAppointmentConfirmationAsync(Appointment appointment, Service service, Barber barber)
    {
        var subject = "✅ Agendamento Confirmado - BarberNet";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #0b0f14, #0f172a); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
        .info-box {{ background: white; padding: 20px; margin: 15px 0; border-radius: 8px; border-left: 4px solid #3b82f6; }}
        .info-row {{ display: flex; justify-content: space-between; margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #e5e7eb; }}
        .label {{ font-weight: bold; color: #6b7280; }}
        .value {{ color: #111827; }}
        .footer {{ background: #f3f4f6; padding: 20px; text-align: center; font-size: 12px; color: #6b7280; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 24px; background: #3b82f6; color: white; text-decoration: none; border-radius: 6px; margin-top: 15px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Agendamento Confirmado!</h1>
            <p>Seu horário foi reservado com sucesso</p>
        </div>
        
        <div class='content'>
            <p>Olá,</p>
            <p>Seu agendamento na <strong>BarberNet</strong> foi confirmado com sucesso!</p>
            
            <div class='info-box'>
                <h3>📋 Detalhes do Agendamento</h3>
                
                <div class='info-row'>
                    <span class='label'>Serviço:</span>
                    <span class='value'>{service.Name}</span>
                </div>
                
                <div class='info-row'>
                    <span class='label'>Barbeiro:</span>
                    <span class='value'>{barber.Name}</span>
                </div>
                
                <div class='info-row'>
                    <span class='label'>Data:</span>
                    <span class='value'>{appointment.Date:dd/MM/yyyy}</span>
                </div>
                
                <div class='info-row'>
                    <span class='label'>Horário:</span>
                    <span class='value'>{appointment.StartTime:HH:mm}</span>
                </div>
                
                <div class='info-row'>
                    <span class='label'>Duração:</span>
                    <span class='value'>{service.DurationMinutes} minutos</span>
                </div>
                
                <div class='info-row'>
                    <span class='label'>Valor:</span>
                    <span class='value'>R$ {service.Price:F2}</span>
                </div>
            </div>
            
            <p><strong>⏰ Importante:</strong> Por favor, chegue com 5 minutos de antecedência.</p>
            <p>Em caso de imprevistos, entre em contato conosco o quanto antes.</p>
        </div>
        
        <div class='footer'>
            <p>Este é um e-mail automático. Por favor, não responda.</p>
            <p>© {DateTime.Now.Year} BarberNet - Todos os direitos reservados</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(appointment.CustomerEmail, subject, body);
    }

    public async Task SendAppointmentCancellationAsync(Appointment appointment, Service service, Barber barber)
    {
        var subject = "❌ Agendamento Cancelado - BarberNet";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ef4444, #dc2626); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
        .info-box {{ background: white; padding: 20px; margin: 15px 0; border-radius: 8px; border-left: 4px solid #ef4444; }}
        .footer {{ background: #f3f4f6; padding: 20px; text-align: center; font-size: 12px; color: #6b7280; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Agendamento Cancelado</h1>
        </div>
        
        <div class='content'>
            <p>Olá,</p>
            <p>Informamos que seu agendamento na <strong>BarberNet</strong> foi cancelado.</p>
            
            <div class='info-box'>
                <h3>📋 Detalhes do Agendamento Cancelado</h3>
                <p><strong>Serviço:</strong> {service.Name}</p>
                <p><strong>Barbeiro:</strong> {barber.Name}</p>
                <p><strong>Data:</strong> {appointment.Date:dd/MM/yyyy}</p>
                <p><strong>Horário:</strong> {appointment.StartTime:HH:mm}</p>
            </div>
            
            <p>Se você não solicitou este cancelamento, entre em contato conosco imediatamente.</p>
            <p>Ficamos à disposição para agendar um novo horário quando desejar.</p>
        </div>
        
        <div class='footer'>
            <p>Este é um e-mail automático. Por favor, não responda.</p>
            <p>© {DateTime.Now.Year} BarberNet - Todos os direitos reservados</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(appointment.CustomerEmail, subject, body);
    }

    public async Task SendAppointmentRescheduleAsync(Appointment appointment, Service service, Barber barber, DateOnly oldDate, TimeOnly oldTime)
    {
        var subject = "🔄 Agendamento Remarcado - BarberNet";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f59e0b, #d97706); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
        .info-box {{ background: white; padding: 20px; margin: 15px 0; border-radius: 8px; border-left: 4px solid #f59e0b; }}
        .old-info {{ text-decoration: line-through; color: #9ca3af; }}
        .new-info {{ color: #059669; font-weight: bold; }}
        .footer {{ background: #f3f4f6; padding: 20px; text-align: center; font-size: 12px; color: #6b7280; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔄 Agendamento Remarcado</h1>
        </div>
        
        <div class='content'>
            <p>Olá,</p>
            <p>Seu agendamento na <strong>BarberNet</strong> foi remarcado.</p>
            
            <div class='info-box'>
                <h3>📋 Horário Anterior</h3>
                <p class='old-info'><strong>Data:</strong> {oldDate:dd/MM/yyyy}</p>
                <p class='old-info'><strong>Horário:</strong> {oldTime:HH:mm}</p>
            </div>
            
            <div class='info-box'>
                <h3>✅ Novo Horário</h3>
                <p class='new-info'><strong>Data:</strong> {appointment.Date:dd/MM/yyyy}</p>
                <p class='new-info'><strong>Horário:</strong> {appointment.StartTime:HH:mm}</p>
                <p><strong>Serviço:</strong> {service.Name}</p>
                <p><strong>Barbeiro:</strong> {barber.Name}</p>
                <p><strong>Duração:</strong> {service.DurationMinutes} minutos</p>
            </div>
            
            <p><strong>⏰ Importante:</strong> Por favor, chegue com 5 minutos de antecedência.</p>
        </div>
        
        <div class='footer'>
            <p>Este é um e-mail automático. Por favor, não responda.</p>
            <p>© {DateTime.Now.Year} BarberNet - Todos os direitos reservados</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(appointment.CustomerEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"] ?? "BarberNet";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                _logger.LogWarning("Configurações de e-mail não encontradas. E-mail não será enviado.");
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? smtpUser, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            
            _logger.LogInformation("E-mail enviado com sucesso para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail para {Email}", toEmail);
            // Não lança exceção para não quebrar o fluxo principal
        }
    }
}