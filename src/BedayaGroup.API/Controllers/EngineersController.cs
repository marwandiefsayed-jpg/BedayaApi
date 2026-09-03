using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Engineers.Commands;
using BedayaGroup.Application.Engineers.DTOs;
using BedayaGroup.Application.Engineers.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/engineers")]
[Authorize]
public class EngineersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<EngineerDto>>>> GetEngineers([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetEngineersQuery(pageIndex, pageSize, search));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<EngineerDto>>> CreateEngineer([FromBody] CreateEngineerRequest request)
    {
        var result = await Mediator.Send(new CreateEngineerCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<EngineerDto>>> UpdateEngineer(int id, [FromBody] UpdateEngineerRequest request)
    {
        var result = await Mediator.Send(new UpdateEngineerCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
