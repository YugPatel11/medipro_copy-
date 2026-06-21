using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MediPro.Core.Configuration;
using MediPro.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Dashboard.Services;

/// <summary>
/// Sends messages and documents via the official Meta WhatsApp Cloud API.
/// </summary>
public sealed class WhatsAppCloudService : IWhatsAppService
{
    private readonly WhatsAppConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WhatsAppCloudService> _logger;

    public WhatsAppCloudService(
        IOptions<WhatsAppConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<WhatsAppCloudService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendTextAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            _logger.LogWarning("WhatsApp service is disabled. Skipped sending message to {Number}.", phoneNumber);
            return false;
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = phoneNumber,
            type = "text",
            text = new { body = message }
        };

        return await SendPayloadAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> SendDocumentAsync(string phoneNumber, string documentPath, string? caption = null, CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            _logger.LogWarning("WhatsApp service is disabled. Skipped sending document to {Number}.", phoneNumber);
            return false;
        }

        // Note: For a production app using the Cloud API, you typically upload the media first to get an ID,
        // or provide a public URL. Since this is a local server, providing a public URL directly isn't possible 
        // without an internet-facing proxy. The simplest approach for local files is a multipart upload 
        // to the /media endpoint first, then sending the message with the media ID.
        
        try
        {
            string mediaId = await UploadMediaAsync(documentPath, cancellationToken).ConfigureAwait(false);
            
            if (string.IsNullOrEmpty(mediaId)) return false;

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = phoneNumber,
                type = "document",
                document = new
                {
                    id = mediaId,
                    caption = caption ?? "Diagnostic Report from VMS MediPro"
                }
            };

            return await SendPayloadAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp document to {Number}", phoneNumber);
            return false;
        }
    }

    private async Task<string> UploadMediaAsync(string filePath, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AccessToken);

        var url = $"{_config.ApiBaseUrl}/{_config.PhoneNumberId}/media";
        
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("whatsapp"), "messaging_product");
        
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath, ct));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await client.PostAsync(url, content, ct).ConfigureAwait(false);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("WhatsApp media upload failed: {Error}", error);
            return string.Empty;
        }

        var responseString = await response.Content.ReadAsStringAsync(ct);
        using var jsonDoc = JsonDocument.Parse(responseString);
        return jsonDoc.RootElement.GetProperty("id").GetString() ?? string.Empty;
    }

    private async Task<bool> SendPayloadAsync(object payload, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AccessToken);

        var url = $"{_config.ApiBaseUrl}/{_config.PhoneNumberId}/messages";
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("WhatsApp message failed: {Error}", error);
            return false;
        }

        _logger.LogInformation("WhatsApp message sent successfully.");
        return true;
    }
}
