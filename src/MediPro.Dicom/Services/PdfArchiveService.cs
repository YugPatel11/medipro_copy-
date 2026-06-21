using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using MediPro.Dicom.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MediPro.Dicom.Services;

/// <summary>
/// Generates high-resolution PDF archives from processed DICOM images using QuestPDF.
/// Supports multi-image grid layouts (2x2) with clinic headers and patient info.
/// </summary>
public sealed class PdfArchiveService : IArchiveService
{
    private readonly DicomServerConfig _config;
    private readonly ILogger<PdfArchiveService> _logger;

    public PdfArchiveService(
        IOptions<DicomServerConfig> config,
        ILogger<PdfArchiveService> logger)
    {
        _config = config.Value;
        _logger = logger;

        // Configure QuestPDF license (Community license for open-source use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public async Task<string> GeneratePdfAsync(
        IReadOnlyList<PrintJob> jobs,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (jobs.Count == 0)
            throw new ArgumentException("At least one print job is required.", nameof(jobs));

        Directory.CreateDirectory(outputDirectory);

        var firstJob = jobs[0];
        var pdfFileName = $"{firstJob.StudyInstanceUid}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        var pdfPath = Path.Combine(outputDirectory, pdfFileName);

        _logger.LogInformation(
            "Generating multi-image PDF for study {StudyUid} with {Count} images",
            firstJob.StudyInstanceUid, jobs.Count);

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // --- Header with clinic info ---
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item()
                                    .Text(firstJob.InstitutionName.Length > 0
                                        ? firstJob.InstitutionName
                                        : "VMS MediPro Diagnostic Center")
                                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                                inner.Item()
                                    .Text($"Patient: {firstJob.PatientName}  |  ID: {firstJob.PatientId}")
                                    .FontSize(10);

                                inner.Item()
                                    .Text($"Study: {firstJob.StudyDescription}  |  Modality: {firstJob.Modality}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                                if (firstJob.ReferringPhysicianName.Length > 0)
                                {
                                    inner.Item()
                                        .Text($"Ref. Physician: {firstJob.ReferringPhysicianName}")
                                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                                }
                            });

                            row.ConstantItem(120).AlignRight().Column(inner =>
                            {
                                inner.Item()
                                    .Text(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"))
                                    .FontSize(8).FontColor(Colors.Grey.Medium);

                                inner.Item()
                                    .Text($"Images: {jobs.Count}")
                                    .FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });

                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // --- Image grid (2x2 layout) ---
                    page.Content().Column(content =>
                    {
                        var chunked = jobs
                            .Select((j, i) => new { Job = j, Index = i })
                            .GroupBy(x => x.Index / 2) // 2 images per row
                            .ToList();

                        foreach (var rowGroup in chunked)
                        {
                            content.Item().PaddingVertical(3).Row(row =>
                            {
                                foreach (var item in rowGroup)
                                {
                                    row.RelativeItem().Padding(3).Column(imgCol =>
                                    {
                                        if (File.Exists(item.Job.RenderedImagePath))
                                        {
                                            imgCol.Item()
                                                .Border(0.5f)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .Image(item.Job.RenderedImagePath)
                                                .FitArea();
                                        }
                                        else
                                        {
                                            imgCol.Item()
                                                .Height(200)
                                                .Background(Colors.Grey.Lighten3)
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Text("[Image Not Found]")
                                                .FontSize(8);
                                        }
                                    });
                                }

                                // Pad empty cell if odd number in row
                                if (rowGroup.Count() == 1)
                                    row.RelativeItem();
                            });
                        }
                    });

                    // --- Footer ---
                    page.Footer().AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated by VMS MediPro  •  ")
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                            text.CurrentPageNumber().FontSize(7);
                            text.Span(" / ").FontSize(7);
                            text.TotalPages().FontSize(7);
                        });
                });
            })
            .GeneratePdf(pdfPath);

        }, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("PDF generated: {PdfPath}", pdfPath);
        return pdfPath;
    }

    /// <inheritdoc />
    public async Task<string> GenerateSinglePdfAsync(
        PrintJob job,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        return await GeneratePdfAsync(new[] { job }, outputDirectory, cancellationToken)
            .ConfigureAwait(false);
    }
}
