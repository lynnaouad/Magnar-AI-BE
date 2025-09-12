using System.Net;
using System.Net.Mail;
using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Models;
using Microsoft.Extensions.Options;
using Serilog;

namespace Magnar.Recruitment.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailConfiguration _defaultSettings;
    private readonly SmtpClient _client;

    public EmailService(IOptions<EmailConfiguration> options)
    {
        _defaultSettings = options.Value;
        _client = new SmtpClient
        {
            Host = options.Value.Host ?? string.Empty,
            Port = options.Value.Port,
            Credentials = new NetworkCredential(options.Value.MailUid, options.Value.Password),
            EnableSsl = options.Value.SSLEnabled,
            Timeout = options.Value.Timeout,
        };
    }

    public async Task SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = HandleSendCompleted();
        var message = new MailMessage
        {
            From = new MailAddress(_defaultSettings.MailUid ?? string.Empty, _defaultSettings.FromDisplayName ?? string.Empty),
            IsBodyHtml = emailMessage.IsBodyHtml,
            Subject = emailMessage.Subject,
            Body = emailMessage.Body,
        };
        message.To.Add(emailMessage.To);

        _client.SendAsync(message, null);

        await task;
    }

    private Task<bool> HandleSendCompleted()
    {
        var tcs = new TaskCompletionSource<bool>();

        _client.SendCompleted += (sender, e) =>
        {
            if (e.Error is not null)
            {
                Log.Error(e.Error, e.Error?.Message ?? string.Empty);
            }

            if (e.UserState is MailMessage userMessage)
            {
                userMessage.Dispose();
            }

            tcs.SetResult(true);
        };

        return tcs.Task;
    }
}
