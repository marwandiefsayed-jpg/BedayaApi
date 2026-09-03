using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.Commands;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Application.Shareholders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/shareholders")]
[Authorize]
public class ShareholdersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<ShareholderDto>>>> GetShareholders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] int? projectId = null)
    {
        var result = await Mediator.Send(new GetShareholdersQuery(pageIndex, pageSize, search, projectId));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ShareholderDto>>> GetShareholderById(int id)
    {
        var result = await Mediator.Send(new GetShareholderByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ShareholderDto>>> CreateShareholder([FromBody] CreateShareholderRequest request)
    {
        var result = await Mediator.Send(new CreateShareholderCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ShareholderDto>>> UpdateShareholder(int id, [FromBody] UpdateShareholderRequest request)
    {
        var result = await Mediator.Send(new UpdateShareholderCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id}/contributions")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ShareholderContributionDto>>> RecordContribution(int id, [FromBody] RecordShareholderContributionRequest request)
    {
        if (id != request.ShareholderId) return BadRequest(ApiResponse.FailureResult("معرف المساهم غير متطابق"));
        var result = await Mediator.Send(new RecordShareholderContributionCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/statement")]
    public async Task<ActionResult<ApiResponse<ShareholderStatementDto>>> GetShareholderStatement(int id)
    {
        var result = await Mediator.Send(new GetShareholderStatementQuery(id));
        return Ok(result);
    }
}
