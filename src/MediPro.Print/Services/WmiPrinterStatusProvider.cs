
using System.Management;
using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using MediPro.Print.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Print.Services;

/// <summary>
/// Queries real-time printer hardware statuses using Windows Management Instrumentation (WMI).
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class WmiPrinterStatusProvider : IPrinterStatusProvider
{
    private readonly PrinterPoolConfig _config;
    private readonly ILogger<WmiPrinterStatusProvider> _logger;

    public WmiPrinterStatusProvider(
        IOptions<PrinterPoolConfig> config,
        ILogger<WmiPrinterStatusProvider> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PrinterInfo>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
    {
        var activePrinters = _config.Printers.Where(p => p.Enabled).Select(p => p.Name).ToList();
        
        if (activePrinters.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<PrinterInfo>>(Array.Empty<PrinterInfo>());
        }

        var results = new List<PrinterInfo>();
        
        // Use WQL IN clause to fetch all configured printers in one query
        var names = string.Join(",", activePrinters.Select(n => $"'{n.Replace("'", "\\'")}'"));
        var query = $"SELECT Name, PrinterStatus, PortName, ShareName FROM Win32_Printer WHERE Name IN ({names})";
        
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            using var collection = searcher.Get();

            foreach (var obj in collection)
            {
                var name = obj["Name"]?.ToString() ?? string.Empty;
                var statusCode = obj["PrinterStatus"] != null 
                    ? Convert.ToInt32(obj["PrinterStatus"]) 
                    : 0;

                var configEntry = _config.Printers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                results.Add(new PrinterInfo
                {
                    Name = name,
                    Status = (PrinterStatusCode)statusCode,
                    PortName = obj["PortName"]?.ToString() ?? string.Empty,
                    ShareName = obj["ShareName"]?.ToString() ?? string.Empty,
                    IsDryFilmPrinter = configEntry?.IsDryFilm ?? false,
                    PaperSize = configEntry?.PaperSize ?? _config.DefaultPaperSize
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WMI query failed for Win32_Printer");
        }

        return Task.FromResult<IReadOnlyList<PrinterInfo>>(results);
    }

    /// <inheritdoc />
    public async Task<PrinterInfo?> GetStatusAsync(string printerName, CancellationToken cancellationToken = default)
    {
        var all = await GetAllStatusesAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<PrinterInfo?> GetFirstAvailableAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllStatusesAsync(cancellationToken).ConfigureAwait(false);
        
        // Filter out idle printers, and sort by priority
        var idlePrinters = all.Where(p => p.IsAvailable).ToList();

        if (idlePrinters.Count == 0) return null;

        var ordered = idlePrinters.OrderBy(p => 
        {
            var conf = _config.Printers.FirstOrDefault(c => c.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
            return conf?.Priority ?? int.MaxValue;
        }).ToList();

        return ordered.FirstOrDefault();
    }
}
