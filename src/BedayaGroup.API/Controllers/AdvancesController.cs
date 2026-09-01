using BedayaGroup.Application.Advances.Commands;
using BedayaGroup.Application.Advances.DTOs;
using BedayaGroup.Application.Advances.Queries;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/advances")]
[Authorize]
public class AdvancesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<AdvanceDto>>>> GetAdvances(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? engineerId = null,
        [FromQuery] int? projectId = null,
        [FromQuery] AdvanceStatus? status = null)
    {
        var result = await Mediator.Send(new GetAdvancesQuery(pageIndex, pageSize, engineerId, projectId, status));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AdvanceDto>>> GetAdvanceById(int id)
    {
        var result = await Mediator.Send(new GetAdvanceByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<AdvanceDto>>> CreateAdvance([FromBody] CreateAdvanceRequest request)
    {
        var result = await Mediator.Send(new CreateAdvanceCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id}/settle")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<AdvanceDto>>> SettleAdvance(int id, [FromBody] SettleAdvanceRequest request)
    {
        if (id != request.AdvanceId) return BadRequest(ApiResponse.FailureResult("معرف العهدة غير متطابق"));
        var result = await Mediator.Send(new SettleAdvanceCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/statement")]
    public async Task<ActionResult<ApiResponse<AdvanceStatementDto>>> GetAdvanceStatement(int id)
    {
        var result = await Mediator.Send(new GetAdvanceStatementQuery(id));
        return Ok(result);
    }
}
