using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() =>
        Ok(await dashboardService.GetStatsAsync());

    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts() =>
        Ok(await dashboardService.GetChartsAsync());
}
