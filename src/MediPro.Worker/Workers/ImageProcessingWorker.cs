using System.Threading.Channels;
using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using MediPro.Dicom.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Worker.Workers;

/// <summary>
/// Background service that consumes the DicomJob channel,
/// runs the Image Processor pipeline in parallel, and pushes results
/// to the PrintJob channel.
/// </summary>
public class ImageProcessingWorker : BackgroundService
{
    private readonly ChannelReader<DicomJob> _dicomJobReader;
    private readonly ChannelWriter<PrintJob> _printJobWriter;
    private readonly IDicomImageProcessor _imageProcessor;
    private readonly DicomServerConfig _config;
    private readonly ILogger<ImageProcessingWorker> _logger;

    public ImageProcessingWorker(
        ChannelReader<DicomJob> dicomJobReader,
        ChannelWriter<PrintJob> printJobWriter,
        IDicomImageProcessor imageProcessor,
        IOptions<DicomServerConfig> config,
        ILogger<ImageProcessingWorker> logger)
    {
        _dicomJobReader = dicomJobReader;
        _printJobWriter = printJobWriter;
        _imageProcessor = imageProcessor;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImageProcessingWorker started with {Count} concurrent threads.", _config.ProcessingWorkerCount);

        // Spawn N concurrent consumer tasks
        var tasks = new List<Task>();
        for (int i = 0; i < _config.ProcessingWorkerCount; i++)
        {
            tasks.Add(Task.Run(() => ProcessJobsAsync(stoppingToken), stoppingToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ProcessJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in _dicomJobReader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogDebug("Dequeued DicomJob {JobId} for processing", job.JobId);
                    
                    var printJob = await _imageProcessor.ProcessAsync(job, stoppingToken).ConfigureAwait(false);
                    
                    await _printJobWriter.WriteAsync(printJob, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process DicomJob {JobId}", job.JobId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
    }
}
