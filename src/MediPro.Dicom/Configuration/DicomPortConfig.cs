namespace MediPro.Dicom.Configuration;

/// <summary>
/// Configuration for a single DICOM listening port.
/// Multiple instances are configured in the "DicomPorts" array of appsettings.json.
/// </summary>
public sealed class DicomPortConfig
{
    /// <summary>TCP port number to listen on (e.g., 104, 105, 106, 107).</summary>
    public int Port { get; set; }

    /// <summary>Application Entity Title for this listener.</summary>
    public string AeTitle { get; set; } = "MEDIPRO";

    /// <summary>Description for dashboard display (e.g., "CT Scanner", "Ultrasound").</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Expected modality on this port (CT, US, MR, DX, CR, MG, etc.). Empty = accept all.</summary>
    public string ExpectedModality { get; set; } = string.Empty;

    /// <summary>Whether this port is actively listening.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum concurrent associations allowed on this port.</summary>
    public int MaxConcurrentAssociations { get; set; } = 50;
}

/// <summary>
/// Root configuration section for the DICOM server.
/// Bound from the "DicomServer" section of appsettings.json.
/// </summary>
public sealed class DicomServerConfig
{
    public const string SectionName = "DicomServer";

    /// <summary>List of ports to listen on.</summary>
    public List<DicomPortConfig> Ports { get; set; } = new();

    /// <summary>Temporary directory for storing received .dcm files before processing.</summary>
    public string TempStoragePath { get; set; } = @"C:\MediPro\DicomTemp";

    /// <summary>Permanent archive directory for processed DICOM files.</summary>
    public string ArchivePath { get; set; } = @"C:\MediPro\Archive";

    /// <summary>Output directory for rendered print-ready images.</summary>
    public string OutputPath { get; set; } = @"C:\MediPro\Output";

    /// <summary>PDF output directory.</summary>
    public string PdfOutputPath { get; set; } = @"C:\MediPro\PDFs";

    /// <summary>Number of parallel image processing workers.</summary>
    public int ProcessingWorkerCount { get; set; } = 4;

    /// <summary>Default target DPI for rendered images.</summary>
    public int DefaultDpi { get; set; } = 300;

    /// <summary>DPI for dry film output.</summary>
    public int DryFilmDpi { get; set; } = 508;

    /// <summary>Default output image format.</summary>
    public string OutputFormat { get; set; } = "BMP";
}
