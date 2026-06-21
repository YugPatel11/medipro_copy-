namespace MediPro.Core.Interfaces;

/// <summary>
/// Sends diagnostic PDFs via WhatsApp Cloud API.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Sends a document (PDF) to a WhatsApp number via the Cloud API.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number in E.164 format (e.g., +919876543210).</param>
    /// <param name="documentPath">Absolute path to the PDF document.</param>
    /// <param name="caption">Optional caption/message to accompany the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> SendDocumentAsync(
        string phoneNumber,
        string documentPath,
        string? caption = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a text-only message to a WhatsApp number.
    /// </summary>
    Task<bool> SendTextAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);
}
