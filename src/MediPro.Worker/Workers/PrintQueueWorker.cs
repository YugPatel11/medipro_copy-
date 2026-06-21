using System.Threading.Channels;
using MediPro.Core.Models;
using MediPro.Print.Services;
using MediPro.Quota.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediPro.Worker.Workers;

/// <summary>
/// Background service that consumes the PrintJob channel, checks quota via QuotaEnforcementService,
/// and dispatches the job to the PrintLoadBalancer.
/// </summary>
public class PrintQueueWorker : BackgroundService
{
    private readonly ChannelReader<PrintJob> _printJobReader;
    private readonly PrintLoadBalancer _loadBalancer;
    private readonly QuotaEnforcementService _quotaEnforcement;
    private readonly ILogger<PrintQueueWorker> _logger;

    public PrintQueueWorker(
        ChannelReader<PrintJob> printJobReader,
        PrintLoadBalancer loadBalancer,
        QuotaEnforcementService quotaEnforcement,
        ILogger<PrintQueueWorker> logger)
    {
        _printJobReader = printJobReader;
        _loadBalancer = loadBalancer;
        _quotaEnforcement = quotaEnforcement;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrintQueueWorker started.");

        try
        {
            await foreach (var job in _printJobReader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogDebug("Dequeued PrintJob {JobId}", job.PrintJobId);

                    // 1. Enforce Quota (blocks if out of quota until recharge)
                    await _quotaEnforcement.WaitUntilQuotaAvailableAsync(stoppingToken).ConfigureAwait(false);

                    // 2. Dispatch to Spooler via Load Balancer (handles WMI checks and exponential backoff)
                    bool success = await _loadBalancer.DispatchWithRetryAsync(job, stoppingToken).ConfigureAwait(false);

                    if (success)
                    {
                        _logger.LogInformation("Job {JobId} successfully printed.", job.PrintJobId);
                    }
                    else
                    {
                        _logger.LogError("Job {JobId} failed to print. Final status: {Status}", job.PrintJobId, job.Status);
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // Graceful exit
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error processing PrintJob {JobId}", job.PrintJobId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        
        _logger.LogInformation("PrintQueueWorker stopped.");
    }
}
