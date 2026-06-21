using System.Collections.Concurrent;
using System.Threading.Channels;
using FellowOakDicom;
using FellowOakDicom.Network;
using MediPro.Core.Models;
using MediPro.Dicom.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Dicom.Services;

/// <summary>
/// Orchestrates multiple DICOM SCP listeners across configured ports.
/// Each port runs an independent, non-blocking async TCP listener.
/// </summary>
public sealed class DicomServerManager : IDisposable
{
    private readonly DicomServerConfig _config;
    private readonly ILogger<DicomServerManager> _logger;
    private readonly ChannelWriter<DicomJob> _jobChannel;
    private readonly ConcurrentDictionary<int, IDicomServer> _servers = new();
    private bool _disposed;

    public DicomServerManager(
        IOptions<DicomServerConfig> config,
        ChannelWriter<DicomJob> jobChannel,
        ILogger<DicomServerManager> logger)
    {
        _config = config.Value;
        _jobChannel = jobChannel;
        _logger = logger;
    }

    /// <summary>
    /// Starts DICOM SCP listeners on all configured and enabled ports.
    /// Each listener is fully independent and non-blocking.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Ensure storage directories exist
        Directory.CreateDirectory(_config.TempStoragePath);
        Directory.CreateDirectory(_config.ArchivePath);
        Directory.CreateDirectory(_config.OutputPath);
        Directory.CreateDirectory(_config.PdfOutputPath);

        // Configure the static SCP dependencies
        CStoreScp.Configure(
            _jobChannel,
            _config.TempStoragePath,
            _logger);

        var enabledPorts = _config.Ports.Where(p => p.Enabled).ToList();

        if (enabledPorts.Count == 0)
        {
            _logger.LogWarning("No DICOM ports are configured or enabled. Server will not receive any data.");
            return;
        }

        _logger.LogInformation("Starting DICOM SCP on {Count} port(s)...", enabledPorts.Count);

        foreach (var portConfig in enabledPorts)
        {
            try
            {
                // Each DicomServer.Create call starts an independent async TCP listener
                // on the thread pool — fully non-blocking and parallel.
                var server = DicomServerFactory.Create<CStoreScp>(
                    portConfig.Port,
                    userState: portConfig);

                _servers.TryAdd(portConfig.Port, server);

                _logger.LogInformation(
                    "DICOM SCP started on port {Port} (AE: {AeTitle}, Modality: {Modality}) — {Desc}",
                    portConfig.Port,
                    portConfig.AeTitle,
                    portConfig.ExpectedModality.Length > 0 ? portConfig.ExpectedModality : "Any",
                    portConfig.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to start DICOM SCP on port {Port}: {Message}",
                    portConfig.Port, ex.Message);
            }
        }

        _logger.LogInformation(
            "DICOM Server Manager started — {Active}/{Total} ports active",
            _servers.Count, enabledPorts.Count);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops all active DICOM SCP listeners.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping DICOM Server Manager ({Count} active listeners)...", _servers.Count);

        foreach (var (port, server) in _servers)
        {
            try
            {
                server.Dispose();
                _logger.LogInformation("DICOM SCP on port {Port} stopped", port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping DICOM SCP on port {Port}", port);
            }
        }

        _servers.Clear();
        _logger.LogInformation("DICOM Server Manager stopped");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the currently active DICOM server ports.
    /// </summary>
    public IReadOnlyCollection<int> ActivePorts => _servers.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Checks if a specific port is actively listening.
    /// </summary>
    public bool IsPortActive(int port) => _servers.ContainsKey(port);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var server in _servers.Values)
        {
            try { server.Dispose(); }
            catch { /* best-effort cleanup */ }
        }

        _servers.Clear();
    }
}
