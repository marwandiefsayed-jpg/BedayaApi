using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Application.Shares.Commands;
using BedayaGroup.Application.Shares.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/shares")]
[Authorize(Policy = "ShareholdersAccess")]
public class SharesController : ApiControllerBase
{
    // ============== Shares ==============

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ShareDto>>>> GetShares()
    {
        var result = await Mediator.Send(new GetSharesQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ShareDto>>> CreateShare([FromBody] CreateShareRequest request)
    {
        var result = await Mediator.Send(new CreateShareCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ShareDto>>> UpdateShare(int id, [FromBody] UpdateShareRequest request)
    {
        var result = await Mediator.Send(new UpdateShareCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

[Route("api/projects/{projectId}/installments")]
[Authorize(Policy = "ShareholdersAccess")]
public class ProjectInstallmentsController : ApiControllerBase
{
    // ============== Project Installments ==============

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProjectInstallmentDto>>>> GetInstallments(int projectId)
    {
        var result = await Mediator.Send(new GetProjectInstallmentsQuery(projectId));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ProjectInstallmentDto>>> CreateInstallment(int projectId, [FromBody] CreateProjectInstallmentRequest request)
    {
        if (projectId != request.ProjectId) return BadRequest(ApiResponse.FailureResult("معرف المشروع غير متطابق"));
        var result = await Mediator.Send(new CreateProjectInstallmentCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{installmentId}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ProjectInstallmentDto>>> UpdateInstallment(int projectId, int installmentId, [FromBody] UpdateProjectInstallmentRequest request)
    {
        var result = await Mediator.Send(new UpdateProjectInstallmentCommand(installmentId, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("manual")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ProjectInstallmentDto>>> CreateManualInstallment(int projectId, [FromBody] CreateManualInstallmentForShareholdersRequest request)
    {
        if (projectId != request.ProjectId) return BadRequest(ApiResponse.FailureResult("معرف المشروع غير متطابق"));
        var result = await Mediator.Send(new CreateManualInstallmentForShareholdersCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{installmentId}")]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteInstallment(int projectId, int installmentId)

    {
        var result = await Mediator.Send(new DeleteProjectInstallmentCommand(installmentId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
