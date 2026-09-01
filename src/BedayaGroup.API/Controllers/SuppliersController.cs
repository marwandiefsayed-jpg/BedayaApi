using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Suppliers.Commands;
using BedayaGroup.Application.Suppliers.DTOs;
using BedayaGroup.Application.Suppliers.Queries;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<SupplierDto>>>> GetSuppliers([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] SupplierType? type = null)
    {
        var result = await Mediator.Send(new GetSuppliersQuery(pageIndex, pageSize, search, type));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplierById(int id)
    {
        var result = await Mediator.Send(new GetSupplierByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        var result = await Mediator.Send(new CreateSupplierCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
    {
        var result = await Mediator.Send(new UpdateSupplierCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/statement")]
    public async Task<ActionResult<ApiResponse<SupplierStatementDto>>> GetSupplierStatement(int id, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetSupplierStatementQuery(id, fromDate, toDate));
        return Ok(result);
    }
}
