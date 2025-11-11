using BusinessObject.DTOs.Financial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.FinancialDashboard;
using System.Security.Claims;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FinancialDashboardController : ControllerBase
{
    private readonly IFinancialDashboardService _service;
    private readonly ILogger<FinancialDashboardController> _logger;

    public FinancialDashboardController(
        IFinancialDashboardService service,
        ILogger<FinancialDashboardController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get financial overview for a club
    /// GET: api/FinancialDashboard/club/{clubId}?semesterId={semesterId}
    /// </summary>
    [HttpGet("club/{clubId}")]
    public async Task<IActionResult> GetOverview(int clubId, [FromQuery] int? semesterId = null)
    {
        try
        {
            var overview = await _service.GetOverviewAsync(clubId, semesterId);
            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial overview for club {ClubId}, semester {SemesterId}", 
                clubId, semesterId);
            return StatusCode(500, new { message = "An error occurred while fetching financial overview", error = ex.Message });
        }
    }

    /// <summary>
    /// Get income vs expenses chart data
    /// GET: api/FinancialDashboard/club/{clubId}/chart?year={year}
    /// </summary>
    [HttpGet("club/{clubId}/chart")]
    public async Task<IActionResult> GetIncomeExpenseChart(int clubId, [FromQuery] int? year = null)
    {
        try
        {
            var chartData = await _service.GetIncomeExpenseChartAsync(clubId, year);
            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart data for club {ClubId}, year {Year}", 
                clubId, year);
            return StatusCode(500, new { message = "An error occurred while fetching chart data", error = ex.Message });
        }
    }

    /// <summary>
    /// Get income sources breakdown
    /// GET: api/FinancialDashboard/club/{clubId}/income-sources?semesterId={semesterId}
    /// </summary>
    [HttpGet("club/{clubId}/income-sources")]
    public async Task<IActionResult> GetIncomeSources(int clubId, [FromQuery] int? semesterId = null)
    {
        try
        {
            var sources = await _service.GetIncomeSourcesAsync(clubId, semesterId);
            return Ok(sources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting income sources for club {ClubId}, semester {SemesterId}", 
                clubId, semesterId);
            return StatusCode(500, new { message = "An error occurred while fetching income sources", error = ex.Message });
        }
    }
}

