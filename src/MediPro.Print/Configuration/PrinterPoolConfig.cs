namespace MediPro.Print.Configuration;

/// <summary>
/// Configuration for the printer pool and load balancer.
/// Bound from the "PrinterPool" section of appsettings.json.
/// </summary>
public sealed class PrinterPoolConfig
{
    public const string SectionName = "PrinterPool";

    /// <summary>List of configured printer names (Windows queue names).</summary>
    public List<PrinterEntry> Printers { get; set; } = new();

    /// <summary>Interval in milliseconds between WMI status polls.</summary>
    public int StatusPollIntervalMs { get; set; } = 2000;

    /// <summary>Maximum retry attempts before marking a print job as failed.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Base delay in milliseconds for exponential backoff when all printers are busy.</summary>
    public int BackoffBaseMs { get; set; } = 500;

    /// <summary>Maximum backoff delay in milliseconds.</summary>
    public int BackoffMaxMs { get; set; } = 10000;

    /// <summary>Default paper size for print jobs.</summary>
    public string DefaultPaperSize { get; set; } = "A4";

    /// <summary>Default print orientation (Portrait/Landscape).</summary>
    public string DefaultOrientation { get; set; } = "Portrait";

    /// <summary>Default margins in hundredths of an inch.</summary>
    public int DefaultMargin { get; set; } = 50;
}

/// <summary>
/// Configuration for a single printer in the pool.
/// </summary>
public sealed class PrinterEntry
{
    /// <summary>Windows printer queue name as it appears in Settings → Printers.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Friendly display name for the dashboard.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Whether this printer is enabled for load balancing.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether this printer uses dry film media.</summary>
    public bool IsDryFilm { get; set; } = false;

    /// <summary>Override paper size for this specific printer.</summary>
    public string? PaperSize { get; set; }

    /// <summary>Priority order — lower numbers are preferred (0 = highest).</summary>
    public int Priority { get; set; } = 0;
}
