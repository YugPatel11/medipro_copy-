using MediPro.Core.Models;

namespace MediPro.Core.Interfaces;

/// <summary>
/// Generates PDF archives from processed DICOM images using QuestPDF.
/// Supports multi-image grid layouts with clinic headers.
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Generates a high-resolution PDF from one or more print jobs
    /// belonging to the same study, arranged in a configurable grid layout.
    /// </summary>
    /// <param name="jobs">Print jobs to include in the PDF (same study).</param>
    /// <param name="outputDirectory">Directory to save the PDF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Absolute path to the generated PDF file.</returns>
    Task<string> GeneratePdfAsync(
        IReadOnlyList<PrintJob> jobs,
        string outputDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a single-image PDF for a standalone print job.
    /// </summary>
    Task<string> GenerateSinglePdfAsync(
        PrintJob job,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
