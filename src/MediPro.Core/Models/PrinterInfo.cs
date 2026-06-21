namespace MediPro.Core.Models;

/// <summary>
/// Represents the real-time status of a physical printer
/// as queried via WMI Win32_Printer.
/// </summary>
public sealed class PrinterInfo
{
    /// <summary>Windows printer queue name (e.g., "HP LaserJet Pro MFP M428").</summary>
    public required string Name { get; init; }

    /// <summary>WMI PrinterStatus code. 3=Idle, 4=Printing, 5=Warmup, 7=Offline.</summary>
    public PrinterStatusCode Status { get; set; } = PrinterStatusCode.Unknown;

    /// <summary>Whether the printer is currently accepting new jobs.</summary>
    public bool IsAvailable => Status == PrinterStatusCode.Idle;

    /// <summary>Number of jobs currently in the Windows spooler queue.</summary>
    public int QueuedJobCount { get; set; }

    /// <summary>Share name for network printers (empty for local).</summary>
    public string ShareName { get; init; } = string.Empty;

    /// <summary>Port name (e.g., USB001, IP_192.168.1.100).</summary>
    public string PortName { get; init; } = string.Empty;

    /// <summary>Whether this printer is configured for dry film output.</summary>
    public bool IsDryFilmPrinter { get; init; }

    /// <summary>Configured paper size name (e.g., "A4", "Letter", "14x17").</summary>
    public string PaperSize { get; init; } = "A4";

    /// <summary>Last time the status was polled.</summary>
    public DateTime LastPolledUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Cumulative print count since last reset.</summary>
    public long TotalPrintCount { get; set; }
}

/// <summary>
/// WMI Win32_Printer.PrinterStatus values.
/// </summary>
public enum PrinterStatusCode
{
    Unknown = 0,
    Other = 1,
    NoError = 2,
    Idle = 3,
    Printing = 4,
    Warmup = 5,
    StoppedPrinting = 6,
    Offline = 7,
    Paused = 8,
    Error = 9,
    Busy = 10,
    NotAvailable = 11,
    Waiting = 12,
    Processing = 13,
    Initializing = 14,
    PowerSave = 15,
    PendingDeletion = 16,
    IOActive = 17,
    ManualFeed = 18
}
