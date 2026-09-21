using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Expenses.DTOs;
using BedayaGroup.Application.Expenses.Queries;
using BedayaGroup.Application.Projects.Commands;
using BedayaGroup.Application.Projects.DTOs;
using BedayaGroup.Application.Projects.Queries;
using BedayaGroup.Application.Reports.Queries;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Application.Shareholders.Queries;
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
    [Authorize(Policy = "CompanyOwnerOnly")]
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
    [Authorize(Policy = "ExpensesAccess")]
    public async Task<ActionResult<ApiResponse<List<DailyExpenseGroupDto>>>> GetProjectDailyExpenses(int projectId, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetDailyExpensesByProjectQuery(projectId, fromDate, toDate));
        return Ok(result);
    }

    [HttpGet("{projectId}/shareholders")]
    [Authorize(Policy = "ShareholdersAccess")]
    public async Task<ActionResult<ApiResponse<PaginatedList<ShareholderDto>>>> GetProjectShareholders(int projectId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetShareholdersQuery(pageIndex, pageSize, null, projectId));
        return Ok(result);
    }

    [HttpPost("{projectId}/export-expenses-pdf")]
    [Authorize(Policy = "ExpensesAccess")]
    public async Task<IActionResult> ExportProjectExpensesPdf(int projectId, [FromQuery] string? materialName = null, [FromQuery] string? descriptionSearch = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, [FromQuery] bool sortByNewest = true)
    {
        var result = await Mediator.Send(new ExportProjectExpensesPdfQuery(projectId, materialName, descriptionSearch, fromDate, toDate, sortByNewest));
        if (!result.Success || result.Data == null)
        {
            return BadRequest(result);
        }

        return File(
            result.Data.FileBytes,
            result.Data.ContentType,
            result.Data.FileName
        );
    }
}
