using System.Net;
using System.Net.Mail;
using MediPro.Core.Configuration;
using MediPro.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Dashboard.Services;

/// <summary>
/// Sends emails via standard SMTP.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailConfig _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailConfig> config,
        ILogger<SmtpEmailService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        string? attachmentPath = null,
        CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            _logger.LogWarning("Email service is disabled. Skipped sending to {Recipient}.", recipientEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(_config.SmtpHost, _config.SmtpPort)
            {
                Credentials = new NetworkCredential(_config.Username, _config.Password),
                EnableSsl = _config.UseSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_config.FromAddress, _config.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(recipientEmail);

            if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
            {
                message.Attachments.Add(new Attachment(attachmentPath));
            }

            _logger.LogInformation("Sending email to {Recipient} with subject: {Subject}", recipientEmail, subject);
            
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation("Email sent successfully to {Recipient}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipientEmail);
            throw;
        }
    }
}
