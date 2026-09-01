using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Expenses.Commands;
using BedayaGroup.Application.Expenses.DTOs;
using BedayaGroup.Application.Expenses.Queries;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/expenses")]
[Authorize]
public class ExpensesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<ExpenseDto>>>> GetExpenses(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? projectId = null,
        [FromQuery] int? floorId = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] ExpenseStatus? status = null)
    {
        var result = await Mediator.Send(new GetExpensesQuery(pageIndex, pageSize, projectId, floorId, supplierId, fromDate, toDate, status));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetExpenseById(int id)
    {
        var result = await Mediator.Send(new GetExpenseByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var result = await Mediator.Send(new CreateExpenseCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        var result = await Mediator.Send(new UpdateExpenseCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
