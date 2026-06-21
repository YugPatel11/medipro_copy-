
using System.Drawing;
using System.Drawing.Printing;
using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using Microsoft.Extensions.Logging;

namespace MediPro.Print.Services;

/// <summary>
/// Submits rendered images directly to the Windows Print Spooler using System.Drawing.Printing.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class PrintSpoolerService : IPrintSpooler
{
    private readonly ILogger<PrintSpoolerService> _logger;

    public PrintSpoolerService(ILogger<PrintSpoolerService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> PrintAsync(PrintJob job, string printerName, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            if (!File.Exists(job.RenderedImagePath))
            {
                _logger.LogError("Image file not found: {Path}", job.RenderedImagePath);
                return Task.FromResult(false);
            }

            // Using Task.Run to keep the UI/calling thread responsive since Print() can block
            Task.Run(() =>
            {
                try
                {
                    using var printDoc = new PrintDocument();
                    printDoc.PrinterSettings.PrinterName = printerName;

                    if (!printDoc.PrinterSettings.IsValid)
                    {
                        _logger.LogError("Printer {PrinterName} is not valid or not found.", printerName);
                        tcs.SetResult(false);
                        return;
                    }

                    // Apply dry film defaults if requested
                    if (job.IsDryFilm)
                    {
                        printDoc.DefaultPageSettings.Color = false;
                        printDoc.DefaultPageSettings.PrinterResolution = printDoc.PrinterSettings.PrinterResolutions
                            .Cast<PrinterResolution>()
                            .FirstOrDefault(r => r.X == 508) ?? printDoc.DefaultPageSettings.PrinterResolution;
                    }
                    else
                    {
                        printDoc.DefaultPageSettings.Color = job.IsColor;
                    }

                    // Define the PrintPage event handler
                    printDoc.PrintPage += (sender, args) =>
                    {
                        if (args.Graphics is null) return;
                        
                        using var img = Image.FromFile(job.RenderedImagePath);

                        // Scale the image to fit within the printable area margins
                        var m = args.MarginBounds;
                        
                        // Simple aspect ratio scaling
                        float imgAspect = (float)img.Width / img.Height;
                        float marginAspect = (float)m.Width / m.Height;

                        float drawWidth = m.Width;
                        float drawHeight = m.Height;

                        if (imgAspect > marginAspect)
                        {
                            drawHeight = m.Width / imgAspect;
                        }
                        else
                        {
                            drawWidth = m.Height * imgAspect;
                        }

                        float x = m.Left + (m.Width - drawWidth) / 2;
                        float y = m.Top + (m.Height - drawHeight) / 2;

                        args.Graphics.DrawImage(img, x, y, drawWidth, drawHeight);
                        args.HasMorePages = false; // Only 1 page per job in this design
                    };

                    _logger.LogInformation("Submitting job {JobId} to spooler queue for {PrinterName}",
                        job.PrintJobId, printerName);

                    printDoc.Print(); // Blocks until spooled
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during printing job {JobId} to {PrinterName}", 
                        job.PrintJobId, printerName);
                    tcs.SetResult(false);
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PrintDocument for job {JobId}", job.PrintJobId);
            tcs.SetResult(false);
        }

        return tcs.Task;
    }
}
