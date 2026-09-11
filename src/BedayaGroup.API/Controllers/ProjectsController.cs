using BedayaGroup.Application.Advances.DTOs;
using BedayaGroup.Application.Advances.Queries;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Engineers.Commands;
using BedayaGroup.Application.Engineers.DTOs;
using BedayaGroup.Application.Engineers.Queries;
using BedayaGroup.Application.Expenses.DTOs;
using BedayaGroup.Application.Expenses.Queries;
using BedayaGroup.Application.Projects.Commands;
using BedayaGroup.Application.Projects.DTOs;
using BedayaGroup.Application.Projects.Queries;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Application.Shareholders.Queries;
using BedayaGroup.Application.Suppliers.DTOs;
using BedayaGroup.Application.Suppliers.Queries;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/projects")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<ProjectDto>>>> GetProjects([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetProjectsQuery(pageIndex, pageSize, search));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProjectById(int id)
    {
        var result = await Mediator.Send(new GetProjectByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var result = await Mediator.Send(new CreateProjectCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        var result = await Mediator.Send(new UpdateProjectCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProject(int id)
    {
        var result = await Mediator.Send(new DeleteProjectCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/financial-summary")]
    public async Task<ActionResult<ApiResponse<ProjectFinancialSummaryDto>>> GetProjectFinancialSummary(int id)
    {
        var result = await Mediator.Send(new GetProjectFinancialSummaryQuery(id));
        return Ok(result);
    }

    [HttpGet("{projectId}/daily-expenses")]
    public async Task<ActionResult<ApiResponse<List<DailyExpenseGroupDto>>>> GetProjectDailyExpenses(int projectId, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetDailyExpensesByProjectQuery(projectId, fromDate, toDate));
        return Ok(result);
    }

    [HttpGet("{projectId}/shareholders")]
    public async Task<ActionResult<ApiResponse<PaginatedList<ShareholderDto>>>> GetProjectShareholders(int projectId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetShareholdersQuery(pageIndex, pageSize, null, projectId));
        return Ok(result);
    }

    // Suppliers under Project
    [HttpGet("{projectId}/suppliers")]
    public async Task<ActionResult<ApiResponse<PaginatedList<SupplierDto>>>> GetProjectSuppliers(int projectId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, [FromQuery] SupplierType? type = null)
    {
        var result = await Mediator.Send(new GetSuppliersQuery(pageIndex, pageSize, search, type, projectId));
        return Ok(result);
    }

    // Advances (عهود) under Project
    [HttpGet("{projectId}/advances")]
    public async Task<ActionResult<ApiResponse<PaginatedList<AdvanceDto>>>> GetProjectAdvances(int projectId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50, [FromQuery] AdvanceStatus? status = null)
    {
        var result = await Mediator.Send(new GetAdvancesQuery(pageIndex, pageSize, null, projectId, status));
        return Ok(result);
    }

    // Engineers assigned to Project
    [HttpGet("{projectId}/engineers")]
    public async Task<ActionResult<ApiResponse<List<ProjectEngineerDto>>>> GetProjectEngineers(int projectId)
    {
        var result = await Mediator.Send(new GetProjectEngineersQuery(projectId));
        return Ok(result);
    }

    [HttpPost("{projectId}/engineers")]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<ProjectEngineerDto>>> AssignEngineerToProject(int projectId, [FromBody] AssignEngineerToProjectRequest request)
    {
        if (projectId != request.ProjectId) return BadRequest(ApiResponse.FailureResult("معرف المشروع غير متطابق"));
        var result = await Mediator.Send(new AssignEngineerToProjectCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
