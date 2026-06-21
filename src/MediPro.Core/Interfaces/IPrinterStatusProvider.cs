using MediPro.Core.Models;

namespace MediPro.Core.Interfaces;

/// <summary>
/// Queries real-time printer hardware statuses via WMI Win32_Printer.
/// </summary>
public interface IPrinterStatusProvider
{
    /// <summary>
    /// Gets the current status of all configured printers in the pool.
    /// </summary>
    Task<IReadOnlyList<PrinterInfo>> GetAllStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a specific printer by name.
    /// </summary>
    Task<PrinterInfo?> GetStatusAsync(string printerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the first idle (available) printer in the pool.
    /// Returns null if all printers are busy/offline.
    /// </summary>
    Task<PrinterInfo?> GetFirstAvailableAsync(CancellationToken cancellationToken = default);
}
