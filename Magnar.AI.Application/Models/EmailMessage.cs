using System.Net.Mail;

namespace Magnar.AI.Application.Models;

public sealed record EmailMessage(
    string? Body,
    bool IsBodyHtml,
    string Subject,
    MailAddress To,
    MailAddress? From = default)
{
}
