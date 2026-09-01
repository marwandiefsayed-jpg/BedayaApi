using BedayaGroup.Application.Auth.DTOs;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Users.Commands;
using BedayaGroup.Application.Users.DTOs;
using BedayaGroup.Application.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/users")]
[Authorize(Policy = "CompanyOwnerOnly")]
public class UsersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<UserDto>>>> GetUsers([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetUsersQuery(pageIndex, pageSize, search));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await Mediator.Send(new CreateUserCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var result = await Mediator.Send(new UpdateUserCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
