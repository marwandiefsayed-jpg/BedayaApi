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

    [HttpDelete("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteShareholder(int id)
    {
        var result = await Mediator.Send(new DeleteShareholderCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/contributions")]
    public async Task<ActionResult<ApiResponse<PaginatedList<ShareholderContributionDto>>>> GetShareholderContributions(int id, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetShareholderContributionsQuery(id, pageIndex, pageSize));
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

    [HttpPut("contributions/{contributionId}")]
    [HttpPut("{id}/contributions/{contributionId}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ShareholderContributionDto>>> UpdateContribution(int contributionId, [FromBody] UpdateShareholderContributionRequest request)
    {
        var result = await Mediator.Send(new UpdateShareholderContributionCommand(contributionId, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("contributions/{contributionId}")]
    [HttpDelete("{id}/contributions/{contributionId}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteContribution(int contributionId)
    {
        var result = await Mediator.Send(new DeleteShareholderContributionCommand(contributionId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/statement")]
    public async Task<ActionResult<ApiResponse<ShareholderStatementDto>>> GetShareholderStatement(int id)
    {
        var result = await Mediator.Send(new GetShareholderStatementQuery(id));
        return Ok(result);
    }

    // ── Penalty Endpoints (غرامة تأخر) ───────────────────────────────

    /// <summary>
    /// إضافة غرامة تأخر على مساهم لدفعة معينة (Admin only)
    /// POST /api/shareholders/{id}/penalties
    /// </summary>
    [HttpPost("{id}/penalties")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ShareholderInstallmentPenaltyDto>>> AddPenalty(
        int id, [FromBody] AddShareholderPenaltyRequest request)
    {
        if (id != request.ShareholderId)
            return BadRequest(ApiResponse.FailureResult("معرف المساهم غير متطابق"));

        var result = await Mediator.Send(new AddShareholderPenaltyCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// حذف غرامة تأخر (Admin only)
    /// DELETE /api/shareholders/penalties/{penaltyId}
    /// </summary>
    [HttpDelete("penalties/{penaltyId}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePenalty(int penaltyId)
    {
        var result = await Mediator.Send(new DeleteShareholderPenaltyCommand(penaltyId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

