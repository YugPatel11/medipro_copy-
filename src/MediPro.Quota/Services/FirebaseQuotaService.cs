using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using MediPro.Quota.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Quota.Services;

/// <summary>
/// Manages print quotas using Firebase Firestore. Maintains a real-time listener
/// to detect remote recharges from the FlutterFlow mobile app instantly.
/// </summary>
public sealed class FirebaseQuotaService : IQuotaService, IDisposable
{
    private readonly FirebaseConfig _config;
    private readonly ILogger<FirebaseQuotaService> _logger;
    private FirestoreDb? _db;
    private QuotaRecord? _cachedQuota;
    private FirestoreChangeListener? _listener;
    private bool _initialized;

    public event EventHandler<QuotaRecord>? QuotaRecharged;
    public event EventHandler? QuotaExhausted;

    public FirebaseQuotaService(
        IOptions<FirebaseConfig> config,
        ILogger<FirebaseQuotaService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    private void EnsureInitialized()
    {
        if (_initialized || !_config.Enabled) return;

        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                if (string.IsNullOrWhiteSpace(_config.ServiceAccountPath) || !File.Exists(_config.ServiceAccountPath))
                {
                    _logger.LogWarning("Firebase Service Account JSON not found at {Path}. Quota checks disabled.", 
                        _config.ServiceAccountPath);
                    return;
                }

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _config.ServiceAccountPath);

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(_config.ServiceAccountPath),
                    ProjectId = _config.ProjectId
                });
            }

            _db = FirestoreDb.Create(_config.ProjectId);
            _initialized = true;
            _logger.LogInformation("Firebase SDK initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase SDK.");
        }
    }

    /// <inheritdoc />
    public async Task<QuotaRecord?> GetCurrentQuotaAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled) return new QuotaRecord { LicenseKey = "DEV", RemainingPrints = 9999, TotalPrints = 9999 };
        EnsureInitialized();
        if (_db is null) return null;

        if (_cachedQuota is not null && _cachedQuota.IsSynced)
            return _cachedQuota;

        try
        {
            var docRef = _db.Collection("clinics").Document(_config.ClinicLicenseKey);
            var snapshot = await docRef.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

            if (snapshot.Exists)
            {
                UpdateCacheFromSnapshot(snapshot);
                return _cachedQuota;
            }
            else
            {
                _logger.LogWarning("Quota document {Key} not found in Firestore.", _config.ClinicLicenseKey);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quota from Firestore.");
            return _cachedQuota; // Return stale cache on network failure
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasQuotaAsync(CancellationToken cancellationToken = default)
    {
        var quota = await GetCurrentQuotaAsync(cancellationToken).ConfigureAwait(false);
        return quota?.HasQuota ?? false;
    }

    /// <inheritdoc />
    public async Task<long> DecrementAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled) return 9999;
        EnsureInitialized();
        if (_db is null || _cachedQuota is null) return 0;

        try
        {
            // Update local cache optimistically
            _cachedQuota.RemainingPrints--;
            long newRemaining = _cachedQuota.RemainingPrints;

            if (newRemaining <= 0)
            {
                QuotaExhausted?.Invoke(this, EventArgs.Empty);
            }

            // Sync with Firestore asynchronously (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var docRef = _db.Collection("clinics").Document(_config.ClinicLicenseKey);
                    await docRef.UpdateAsync("RemainingPrints", FieldValue.Increment(-1)).ConfigureAwait(false);
                    _logger.LogDebug("Firestore quota decremented successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrement quota in Firestore.");
                    _cachedQuota.IsSynced = false;
                }
            });

            return newRemaining;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during quota decrement.");
            return _cachedQuota.RemainingPrints;
        }
    }

    /// <inheritdoc />
    public Task StartListenerAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled) return Task.CompletedTask;
        EnsureInitialized();
        if (_db is null) return Task.CompletedTask;

        var docRef = _db.Collection("clinics").Document(_config.ClinicLicenseKey);
        
        _listener = docRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                long oldRemaining = _cachedQuota?.RemainingPrints ?? 0;
                UpdateCacheFromSnapshot(snapshot);

                if (_cachedQuota!.RemainingPrints > oldRemaining)
                {
                    _logger.LogInformation("Remote quota recharge detected! New balance: {Remaining}", 
                        _cachedQuota.RemainingPrints);
                    
                    QuotaRecharged?.Invoke(this, _cachedQuota);
                }
            }
        });

        _logger.LogInformation("Started Firestore real-time listener for quota document {Key}", _config.ClinicLicenseKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopListenerAsync()
    {
        _listener?.StopAsync();
        _logger.LogInformation("Stopped Firestore real-time listener.");
        return Task.CompletedTask;
    }

    private void UpdateCacheFromSnapshot(DocumentSnapshot snapshot)
    {
        _cachedQuota = new QuotaRecord
        {
            LicenseKey = snapshot.Id,
            RemainingPrints = snapshot.TryGetValue<long>("RemainingPrints", out var rem) ? rem : 0,
            TotalPrints = snapshot.TryGetValue<long>("TotalPrints", out var tot) ? tot : 0,
            ClinicName = snapshot.TryGetValue<string>("ClinicName", out var name) ? name : string.Empty,
            LastUpdatedUtc = DateTime.UtcNow,
            IsSynced = true
        };
    }

    public void Dispose()
    {
        _listener?.StopAsync().GetAwaiter().GetResult();
    }
}
