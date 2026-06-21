using MediPro.Dicom.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediPro.Worker.Workers;

/// <summary>
/// Background service that manages the lifecycle of the multi-port DICOM server.
/// </summary>
public class DicomHostedService : IHostedService
{
    private readonly DicomServerManager _serverManager;
    private readonly ILogger<DicomHostedService> _logger;

    public DicomHostedService(
        DicomServerManager serverManager,
        ILogger<DicomHostedService> logger)
    {
        _serverManager = serverManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting DicomHostedService...");
        await _serverManager.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping DicomHostedService...");
        await _serverManager.StopAsync(cancellationToken);
    }
}
