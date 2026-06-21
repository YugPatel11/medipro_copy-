using MediPro.Core.Models;

namespace MediPro.Core.Interfaces;

/// <summary>
/// Manages cloud-synced print quota via Firebase Firestore.
/// Enforces the pay-per-print licensing model.
/// </summary>
public interface IQuotaService
{
    /// <summary>
    /// Gets the current quota snapshot for the configured clinic license.
    /// </summary>
    Task<QuotaRecord?> GetCurrentQuotaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the clinic has remaining print quota.
    /// </summary>
    Task<bool> HasQuotaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements the print quota by 1 after a successful print.
    /// Updates both the local cache and Firestore asynchronously.
    /// </summary>
    /// <returns>The new remaining count after decrement.</returns>
    Task<long> DecrementAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the real-time Firestore listener that watches for
    /// remote quota recharges from the FlutterFlow mobile app.
    /// </summary>
    Task StartListenerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the real-time Firestore listener.
    /// </summary>
    Task StopListenerAsync();

    /// <summary>
    /// Event raised when quota is recharged remotely (by the FlutterFlow app).
    /// </summary>
    event EventHandler<QuotaRecord>? QuotaRecharged;

    /// <summary>
    /// Event raised when quota is exhausted (RemainingPrints reaches 0).
    /// </summary>
    event EventHandler? QuotaExhausted;
}
