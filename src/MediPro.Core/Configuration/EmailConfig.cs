namespace MediPro.Core.Configuration;

/// <summary>
/// SMTP email configuration bound from appsettings.json section "Email".
/// </summary>
public sealed class EmailConfig
{
    public const string SectionName = "Email";

    /// <summary>SMTP server hostname (e.g., smtp.gmail.com).</summary>
    public string SmtpHost { get; set; } = "smtp.gmail.com";

    /// <summary>SMTP server port (587 for TLS, 465 for SSL).</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Whether to use SSL/TLS for the SMTP connection.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>SMTP authentication username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP authentication password or app-specific password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Sender display name.</summary>
    public string FromName { get; set; } = "VMS MediPro";

    /// <summary>Sender email address.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Whether email functionality is enabled.</summary>
    public bool Enabled { get; set; } = false;
}
