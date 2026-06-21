using MediPro.Core.Models;

namespace MediPro.Core.Interfaces;

/// <summary>
/// Routes processed print jobs to physical printers via the Windows Print Spooler.
/// </summary>
public interface IPrintSpooler
{
    /// <summary>
    /// Sends a print-ready image to the designated printer.
    /// </summary>
    /// <param name="job">Print job containing the rendered image path and configuration.</param>
    /// <param name="printerName">Target Windows printer queue name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the print was dispatched successfully.</returns>
    Task<bool> PrintAsync(PrintJob job, string printerName, CancellationToken cancellationToken = default);
}
