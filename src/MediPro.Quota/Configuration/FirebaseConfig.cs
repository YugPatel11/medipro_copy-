namespace MediPro.Quota.Configuration;

/// <summary>
/// Configuration for the Firebase SDK used for Quota synchronization.
/// Bound from the "Firebase" section of appsettings.json.
/// </summary>
public sealed class FirebaseConfig
{
    public const string SectionName = "Firebase";

    /// <summary>Absolute path to the Firebase Service Account JSON credentials file.</summary>
    public string ServiceAccountPath { get; set; } = string.Empty;

    /// <summary>The Firebase Project ID.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>The unique license key for this specific clinic/server.</summary>
    public string ClinicLicenseKey { get; set; } = string.Empty;

    /// <summary>Whether Firebase quota sync is enabled.</summary>
    public bool Enabled { get; set; } = true;
}
