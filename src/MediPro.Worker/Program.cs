using System.Threading.Channels;
using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using MediPro.Dicom.Configuration;
using MediPro.Dicom.Services;
using MediPro.Print.Configuration;
using MediPro.Print.Services;
using MediPro.Quota.Configuration;
using MediPro.Quota.Services;
using MediPro.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateDefaultBuilder(args);

// Ensure it can run as a Windows Service if installed via sc.exe
builder.UseWindowsService(options =>
{
    options.ServiceName = "VMS MediPro DICOM Engine";
});

// Configure Serilog for rolling file and console logs
builder.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(@"C:\MediPro\Logs\medipro-.txt", rollingInterval: RollingInterval.Day));

builder.ConfigureServices((context, services) =>
{
    // Bind configuration sections
    services.Configure<DicomServerConfig>(context.Configuration.GetSection(DicomServerConfig.SectionName));
    services.Configure<PrinterPoolConfig>(context.Configuration.GetSection(PrinterPoolConfig.SectionName));
    services.Configure<FirebaseConfig>(context.Configuration.GetSection(FirebaseConfig.SectionName));

    // Register In-Memory Channels (Thread-safe queues)
    // DicomJob channel (unbounded)
    services.AddSingleton(Channel.CreateUnbounded<DicomJob>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = false }));
    services.AddSingleton(provider => provider.GetRequiredService<Channel<DicomJob>>().Reader);
    services.AddSingleton(provider => provider.GetRequiredService<Channel<DicomJob>>().Writer);

    // PrintJob channel (unbounded)
    services.AddSingleton(Channel.CreateUnbounded<PrintJob>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = false }));
    services.AddSingleton(provider => provider.GetRequiredService<Channel<PrintJob>>().Reader);
    services.AddSingleton(provider => provider.GetRequiredService<Channel<PrintJob>>().Writer);

    // Register Core Subsystems
    services.AddSingleton<IDicomImageProcessor, DicomImageProcessor>();
    services.AddSingleton<IPrintSpooler, PrintSpoolerService>();
    services.AddSingleton<IPrinterStatusProvider, WmiPrinterStatusProvider>();
    services.AddSingleton<PrintLoadBalancer>();

    services.AddSingleton<IQuotaService, FirebaseQuotaService>();
    services.AddSingleton<QuotaEnforcementService>();

    // Register DICOM Server Manager
    services.AddSingleton<DicomServerManager>();

    // Register Hosted Services (Background Workers)
    services.AddHostedService<DicomHostedService>();       // Starts multi-port listener
    services.AddHostedService<ImageProcessingWorker>();    // Consumes DicomJobs -> Processes -> Produces PrintJobs
    services.AddHostedService<PrintQueueWorker>();         // Consumes PrintJobs -> Enforces Quota -> Prints
});

var host = builder.Build();

// Ensure Firebase listener starts up with the host
var quotaService = host.Services.GetRequiredService<IQuotaService>();
_ = quotaService.StartListenerAsync();

await host.RunAsync();
