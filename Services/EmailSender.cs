using Microsoft.AspNetCore.Identity.UI.Services;

namespace PayrollManagementSystem.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // No-op for development (or log to console)
            return Task.CompletedTask;
        }
    }
}