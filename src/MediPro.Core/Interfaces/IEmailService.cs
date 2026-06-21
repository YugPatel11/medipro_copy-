namespace MediPro.Core.Interfaces;

/// <summary>
/// Sends diagnostic PDFs and reports via SMTP email.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a PDF attachment to the specified recipient.
    /// </summary>
    /// <param name="recipientEmail">Destination email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">HTML or plain-text body content.</param>
    /// <param name="attachmentPath">Absolute path to the PDF to attach.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        string? attachmentPath = null,
        CancellationToken cancellationToken = default);
}
