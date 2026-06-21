namespace MediPro.Core.Models;

/// <summary>
/// PNDT (Pre-Natal Diagnostic Technique Act) compliance log entry.
/// Legally required audit trail for all obstetric ultrasound prints.
/// </summary>
public sealed class PndtLogEntry
{
    public long Id { get; set; }
    public Guid PrintJobId { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    // --- Patient Information ---
    public required string PatientName { get; init; }
    public required string PatientId { get; init; }
    public string PatientAge { get; init; } = string.Empty;
    public string PatientGender { get; init; } = string.Empty;

    // --- Clinical Information ---
    public required string ReferringPhysicianName { get; init; }
    public required string InstitutionName { get; init; }
    public string StudyDescription { get; init; } = string.Empty;
    public string BodyPartExamined { get; init; } = string.Empty;

    // --- Print Details ---
    public int ImageCount { get; init; }
    public string PrinterName { get; init; } = string.Empty;
    public string Modality { get; init; } = "US";

    // --- Compliance Fields ---
    /// <summary>Whether the PNDT Form-F was generated for this print.</summary>
    public bool FormFGenerated { get; set; }

    /// <summary>Digital signature hash of the log entry for tamper detection.</summary>
    public string? IntegrityHash { get; set; }

    /// <summary>Operator/technician who initiated the study.</summary>
    public string OperatorName { get; init; } = string.Empty;

    /// <summary>Indication/reason for the ultrasound examination.</summary>
    public string Indication { get; init; } = string.Empty;
}
