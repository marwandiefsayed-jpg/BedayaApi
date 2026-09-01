using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Dashboard.DTOs;
using BedayaGroup.Application.Dashboard.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/dashboard")]
[Authorize]
public class DashboardController : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
    {
        var result = await Mediator.Send(new GetDashboardSummaryQuery());
        return Ok(result);
    }
}
