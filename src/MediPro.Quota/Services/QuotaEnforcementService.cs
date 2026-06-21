using MediPro.Core.Interfaces;
using MediPro.Quota.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Quota.Services;

/// <summary>
/// Service that gates print jobs based on remaining clinic quota.
/// Will block/pause job dispatching if quota reaches zero until a remote recharge occurs.
/// </summary>
public sealed class QuotaEnforcementService
{
    private readonly IQuotaService _quotaService;
    private readonly FirebaseConfig _config;
    private readonly ILogger<QuotaEnforcementService> _logger;

    public QuotaEnforcementService(
        IQuotaService quotaService,
        IOptions<FirebaseConfig> config,
        ILogger<QuotaEnforcementService> logger)
    {
        _quotaService = quotaService;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks quota before allowing a print job to proceed.
    /// If quota is exhausted, this method blocks/awaits until a recharge event fires.
    /// </summary>
    public async Task WaitUntilQuotaAvailableAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled) return;

        bool hasQuota = await _quotaService.HasQuotaAsync(cancellationToken).ConfigureAwait(false);
        if (hasQuota) return;

        _logger.LogWarning("Quota exhausted! Pausing print pipeline until recharge is detected...");

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Register for cancellation
        using var reg = cancellationToken.Register(() => tcs.TrySetCanceled());

        // Event handler to resume when recharged
        void OnRecharged(object? sender, MediPro.Core.Models.QuotaRecord record)
        {
            _logger.LogInformation("Recharge detected! Resuming print pipeline...");
            tcs.TrySetResult();
        }

        _quotaService.QuotaRecharged += OnRecharged;

        try
        {
            // Re-check just in case it recharged right before we subscribed
            if (await _quotaService.HasQuotaAsync(cancellationToken).ConfigureAwait(false))
                return;

            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _quotaService.QuotaRecharged -= OnRecharged;
        }
    }
}
