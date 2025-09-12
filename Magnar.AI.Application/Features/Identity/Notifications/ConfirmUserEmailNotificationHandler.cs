using System.Net;
using System.Net.Mail;
using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Magnar.AI.Application.Features.Identity.Notifications;

public sealed record ConfirmUserEmailNotification(string Email, string UserId, string Token) : INotification;

public class ConfirmUserEmailNotificationHandler : INotificationHandler<ConfirmUserEmailNotification>
{
    private readonly IEmailService emailService;
    private readonly IStringLocalizer<SharedResource> stringLocalizer;
    private readonly UrlsConfiguration urlsConfiguration;

    public ConfirmUserEmailNotificationHandler(
        IEmailService emailService,
        IStringLocalizer<SharedResource> stringLocalizer,
        IOptions<UrlsConfiguration> urlsConfiguration)
    {
        this.emailService = emailService;
        this.stringLocalizer = stringLocalizer;
        this.urlsConfiguration = urlsConfiguration.Value;
    }

    public async Task Handle(ConfirmUserEmailNotification notification, CancellationToken cancellationToken)
    {
        EmailMessage message = CreateEmailConfirmationMessage(notification.Email, notification.UserId, notification.Token);
        await emailService.SendEmailAsync(message, cancellationToken);
    }

    private EmailMessage CreateEmailConfirmationMessage(string emailTo, string userId, string token)
    {
        Dictionary<string, string> tokens = new()
        {
            {
                Constants.PlaceHolders.BaseUrl,
                urlsConfiguration.WebUrl.ToString().Trim('/') ?? string.Empty
            },
            { Constants.PlaceHolders.Token, WebUtility.UrlEncode(token) },
            { Constants.PlaceHolders.UserId, userId },
        };

        return new EmailMessage(
            stringLocalizer[Constants.Localization.EmailConfirmationBody].Value.ReplaceEmailTokens(tokens),
            true,
            stringLocalizer[Constants.Localization.EmailConfirmationSubject],
            new MailAddress(emailTo));
    }
}
