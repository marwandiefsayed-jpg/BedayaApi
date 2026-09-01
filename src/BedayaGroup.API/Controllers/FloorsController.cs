using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Floors.Commands;
using BedayaGroup.Application.Floors.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/floors")]
[Authorize]
public class FloorsController : ApiControllerBase
{
    [HttpPut("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<FloorDto>>> UpdateFloor(int id, [FromBody] UpdateFloorRequest request)
    {
        var result = await Mediator.Send(new UpdateFloorCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
