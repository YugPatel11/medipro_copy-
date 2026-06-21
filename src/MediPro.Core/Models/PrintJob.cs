namespace MediPro.Core.Models;

/// <summary>
/// Represents a processed, print-ready image derived from a DicomJob.
/// Contains the rendered bitmap data, target paper configuration,
/// and metadata needed by the print spooler.
/// </summary>
public sealed class PrintJob
{
    public Guid PrintJobId { get; init; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Reference back to the originating DICOM job.</summary>
    public Guid OriginatingDicomJobId { get; init; }

    /// <summary>Absolute path to the rendered image file (BMP/JPEG) ready for printing.</summary>
    public required string RenderedImagePath { get; init; }

    /// <summary>Absolute path to the archived PDF (set after PDF generation).</summary>
    public string? ArchivedPdfPath { get; set; }

    /// <summary>Target DPI for printing (default 300 for high-quality, 508 for dry film).</summary>
    public int TargetDpi { get; init; } = 300;

    /// <summary>Image width in pixels after processing.</summary>
    public int WidthPixels { get; init; }

    /// <summary>Image height in pixels after processing.</summary>
    public int HeightPixels { get; init; }

    /// <summary>Whether this job targets dry film output (PET medical film).</summary>
    public bool IsDryFilm { get; init; }

    /// <summary>Whether this is a color image (e.g., Doppler ultrasound) vs grayscale.</summary>
    public bool IsColor { get; init; }

    /// <summary>Modality of the source DICOM (CT, MR, US, CR, DX, etc.).</summary>
    public string Modality { get; init; } = string.Empty;

    /// <summary>Patient info carried forward for print header/PNDT logging.</summary>
    public string PatientName { get; init; } = string.Empty;
    public string PatientId { get; init; } = string.Empty;
    public string StudyDescription { get; init; } = string.Empty;
    public string ReferringPhysicianName { get; init; } = string.Empty;
    public string InstitutionName { get; init; } = string.Empty;

    /// <summary>Study Instance UID for grouping multi-image print layouts.</summary>
    public string StudyInstanceUid { get; init; } = string.Empty;

    /// <summary>Current status of this print job.</summary>
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Queued;

    /// <summary>Name of the printer that processed this job (set after dispatch).</summary>
    public string? AssignedPrinter { get; set; }

    /// <summary>Error message if the job failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Timestamp when printing completed.</summary>
    public DateTime? CompletedUtc { get; set; }
}

public enum PrintJobStatus
{
    Queued = 0,
    Processing = 1,
    Printing = 2,
    Completed = 3,
    Failed = 4,
    QuotaExhausted = 5
}
