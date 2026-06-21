

namespace MediPro.Core.Models;

/// <summary>
/// Represents an incoming DICOM file received by the C-STORE SCP.
/// Carries the raw file path and extracted header metadata for downstream processing.
/// </summary>
public sealed class DicomJob
{
    public Guid JobId { get; init; } = Guid.NewGuid();
    public DateTime ReceivedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Absolute path to the stored .dcm file on disk.</summary>
    public required string FilePath { get; init; }

    /// <summary>DICOM SOP Instance UID — unique per image.</summary>
    public required string SopInstanceUid { get; init; }

    /// <summary>Calling AE Title of the modality that sent the image.</summary>
    public string CallingAeTitle { get; init; } = string.Empty;

    /// <summary>Called AE Title on the local server side.</summary>
    public string CalledAeTitle { get; init; } = string.Empty;

    /// <summary>Port on which this file was received.</summary>
    public int ReceivedPort { get; init; }

    // --- Key DICOM header fields (extracted on reception) ---

    /// <summary>Modality type (CT, MR, US, CR, DX, MG, OT, etc.).</summary>
    public string Modality { get; init; } = string.Empty;

    /// <summary>Patient name from DICOM tag (0010,0010).</summary>
    public string PatientName { get; init; } = string.Empty;

    /// <summary>Patient ID from DICOM tag (0010,0020).</summary>
    public string PatientId { get; init; } = string.Empty;

    /// <summary>Study description from DICOM tag (0008,1030).</summary>
    public string StudyDescription { get; init; } = string.Empty;

    /// <summary>Body Part Examined from DICOM tag (0018,0015).</summary>
    public string BodyPartExamined { get; init; } = string.Empty;

    /// <summary>Photometric Interpretation — MONOCHROME1, MONOCHROME2, RGB, YBR_FULL, etc.</summary>
    public string PhotometricInterpretation { get; init; } = string.Empty;

    /// <summary>Bits Allocated per pixel sample (8, 12, or 16).</summary>
    public int BitsAllocated { get; init; }

    /// <summary>Window Center tag (0028,1050) — used for LUT mapping.</summary>
    public double WindowCenter { get; init; }

    /// <summary>Window Width tag (0028,1051) — used for LUT mapping.</summary>
    public double WindowWidth { get; init; }

    /// <summary>Study Instance UID for grouping images into a single study.</summary>
    public string StudyInstanceUid { get; init; } = string.Empty;

    /// <summary>Series Instance UID.</summary>
    public string SeriesInstanceUid { get; init; } = string.Empty;

    /// <summary>Referring Physician Name (0008,0090) — needed for PNDT compliance.</summary>
    public string ReferringPhysicianName { get; init; } = string.Empty;

    /// <summary>Institution Name (0008,0080).</summary>
    public string InstitutionName { get; init; } = string.Empty;
}
