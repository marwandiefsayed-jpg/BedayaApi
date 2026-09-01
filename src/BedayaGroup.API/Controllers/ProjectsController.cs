using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Engineers.Commands;
using BedayaGroup.Application.Engineers.DTOs;
using BedayaGroup.Application.Engineers.Queries;
using BedayaGroup.Application.Floors.Commands;
using BedayaGroup.Application.Floors.DTOs;
using BedayaGroup.Application.Floors.Queries;
using BedayaGroup.Application.Projects.Commands;
using BedayaGroup.Application.Projects.DTOs;
using BedayaGroup.Application.Projects.Queries;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/projects")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<ProjectDto>>>> GetProjects([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] ProjectStatus? status = null)
    {
        var result = await Mediator.Send(new GetProjectsQuery(pageIndex, pageSize, search, status));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProjectById(int id)
    {
        var result = await Mediator.Send(new GetProjectByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var result = await Mediator.Send(new CreateProjectCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CompanyOrProjectOwner")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        var result = await Mediator.Send(new UpdateProjectCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/financial-summary")]
    public async Task<ActionResult<ApiResponse<ProjectFinancialSummaryDto>>> GetProjectFinancialSummary(int id)
    {
        var result = await Mediator.Send(new GetProjectFinancialSummaryQuery(id));
        return Ok(result);
    }

    // Floors under Project
    [HttpGet("{projectId}/floors")]
    public async Task<ActionResult<ApiResponse<List<FloorDto>>>> GetProjectFloors(int projectId)
    {
        var result = await Mediator.Send(new GetFloorsByProjectQuery(projectId));
        return Ok(result);
    }

    [HttpPost("{projectId}/floors")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<FloorDto>>> CreateProjectFloor(int projectId, [FromBody] CreateFloorRequest request)
    {
        if (projectId != request.ProjectId) return BadRequest(ApiResponse.FailureResult("معرف المشروع غير متطابق"));
        var result = await Mediator.Send(new CreateFloorCommand(request));
        if (!result.Success) return BadRequest(result);
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
    [Authorize(Policy = "CompanyOrProjectOwner")]
    public async Task<ActionResult<ApiResponse<ProjectEngineerDto>>> AssignEngineerToProject(int projectId, [FromBody] AssignEngineerToProjectRequest request)
    {
        if (projectId != request.ProjectId) return BadRequest(ApiResponse.FailureResult("معرف المشروع غير متطابق"));
        var result = await Mediator.Send(new AssignEngineerToProjectCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
