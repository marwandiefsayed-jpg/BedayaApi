using BedayaGroup.Application.AuditLogs.DTOs;
using BedayaGroup.Application.AuditLogs.Queries;
using BedayaGroup.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/auditlogs")]
[Authorize(Policy = "CompanyOwnerOnly")]
public class AuditLogsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetAuditLogsQuery(pageIndex, pageSize, userId, entityName, action, fromDate, toDate));
        return Ok(result);
    }
}
