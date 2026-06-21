using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using MediPro.Print.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Print.Services;

/// <summary>
/// Routes print jobs to available printers based on WMI hardware status queries.
/// Implements exponential backoff if all printers are currently busy.
/// </summary>
public sealed class PrintLoadBalancer
{
    private readonly IPrinterStatusProvider _statusProvider;
    private readonly IPrintSpooler _spooler;
    private readonly PrinterPoolConfig _config;
    private readonly ILogger<PrintLoadBalancer> _logger;

    public PrintLoadBalancer(
        IPrinterStatusProvider statusProvider,
        IPrintSpooler spooler,
        IOptions<PrinterPoolConfig> config,
        ILogger<PrintLoadBalancer> logger)
    {
        _statusProvider = statusProvider;
        _spooler = spooler;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches a job to the first available printer. If all printers are busy,
    /// it waits and retries using exponential backoff.
    /// </summary>
    public async Task<bool> DispatchWithRetryAsync(PrintJob job, CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        int delayMs = _config.BackoffBaseMs;

        while (attempt < _config.MaxRetryAttempts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            var availablePrinter = await _statusProvider.GetFirstAvailableAsync(cancellationToken).ConfigureAwait(false);

            if (availablePrinter is not null)
            {
                _logger.LogInformation(
                    "Job {JobId} routed to {PrinterName} (Attempt {Attempt}/{Max})",
                    job.PrintJobId, availablePrinter.Name, attempt, _config.MaxRetryAttempts);

                job.AssignedPrinter = availablePrinter.Name;
                job.Status = PrintJobStatus.Printing;

                bool success = await _spooler.PrintAsync(job, availablePrinter.Name, cancellationToken).ConfigureAwait(false);
                
                if (success)
                {
                    job.Status = PrintJobStatus.Completed;
                    job.CompletedUtc = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    _logger.LogWarning("Spooler failed to accept job {JobId} for {PrinterName}", job.PrintJobId, availablePrinter.Name);
                }
            }
            else
            {
                _logger.LogInformation("All printers busy. Job {JobId} waiting {Delay}ms...", job.PrintJobId, delayMs);
            }

            // Exponential backoff
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            delayMs = Math.Min(delayMs * 2, _config.BackoffMaxMs);
        }

        _logger.LogError("Failed to dispatch job {JobId} after {Max} attempts. All printers busy or spooling failed.",
            job.PrintJobId, _config.MaxRetryAttempts);

        job.Status = PrintJobStatus.Failed;
        job.ErrorMessage = "Max retry attempts reached. No available printers.";
        return false;
    }
}
