namespace Magnar.AI.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken);
}
