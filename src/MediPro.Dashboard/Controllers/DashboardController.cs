using MediPro.Core.Interfaces;
using MediPro.Dashboard.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediPro.Dashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IQuotaService _quotaService;
    private readonly IPrinterStatusProvider _printerStatusProvider;
    private readonly MediProDbContext _dbContext;

    public DashboardController(IQuotaService quotaService, IPrinterStatusProvider printerStatusProvider, MediProDbContext dbContext)
    {
        _quotaService = quotaService;
        _printerStatusProvider = printerStatusProvider;
        _dbContext = dbContext;
    }

    [HttpGet("quota")]
    public async Task<IActionResult> GetQuota()
    {
        var quota = await _quotaService.GetCurrentQuotaAsync();
        if (quota == null) return NotFound();
        return Ok(quota);
    }

    [HttpGet("printers")]
    public async Task<IActionResult> GetPrinters()
    {
        var printers = await _printerStatusProvider.GetAllStatusesAsync();
        return Ok(printers);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _dbContext.PndtLogs
            .OrderByDescending(x => x.TimestampUtc)
            .Take(100)
            .ToListAsync();
            
        return Ok(logs);
    }
}
