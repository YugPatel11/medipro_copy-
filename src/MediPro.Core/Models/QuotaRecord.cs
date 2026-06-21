namespace MediPro.Core.Models;

/// <summary>
/// Firebase Firestore document snapshot for a clinic's print quota.
/// Path: clinics/{licenseKey}
/// </summary>
public sealed class QuotaRecord
{
    /// <summary>Unique clinic license key (matches Firestore document ID).</summary>
    public required string LicenseKey { get; init; }

    /// <summary>Remaining prints available before the server blocks new jobs.</summary>
    public long RemainingPrints { get; set; }

    /// <summary>Total prints purchased across all recharges.</summary>
    public long TotalPrints { get; set; }

    /// <summary>Total prints consumed (TotalPrints - RemainingPrints).</summary>
    public long ConsumedPrints => TotalPrints - RemainingPrints;

    /// <summary>Whether the quota allows printing.</summary>
    public bool HasQuota => RemainingPrints > 0;

    /// <summary>Clinic name for display purposes.</summary>
    public string ClinicName { get; init; } = string.Empty;

    /// <summary>Last time the Firestore document was updated.</summary>
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Last time quota was recharged from the FlutterFlow mobile app.</summary>
    public DateTime? LastRechargeUtc { get; set; }

    /// <summary>Local cache staleness indicator — set to true when a remote update is detected.</summary>
    public bool IsSynced { get; set; } = true;
}
