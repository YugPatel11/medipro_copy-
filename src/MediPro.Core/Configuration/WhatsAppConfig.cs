namespace MediPro.Core.Configuration;

/// <summary>
/// WhatsApp Cloud API configuration bound from appsettings.json section "WhatsApp".
/// </summary>
public sealed class WhatsAppConfig
{
    public const string SectionName = "WhatsApp";

    /// <summary>WhatsApp Business API base URL.</summary>
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com/v18.0";

    /// <summary>Phone Number ID from the Meta Business dashboard.</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>Permanent access token for the WhatsApp Business API.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Whether WhatsApp sharing is enabled.</summary>
    public bool Enabled { get; set; } = false;
}
