using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Expenses.Commands;
using BedayaGroup.Application.Expenses.DTOs;
using BedayaGroup.Application.Expenses.Queries;
using BedayaGroup.Application.Reports.Queries;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/expenses")]
[Authorize(Policy = "ExpensesAccess")]
public class ExpensesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<ExpenseDto>>>> GetExpenses(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? projectId = null,
        [FromQuery] int? storageId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] ExpenseStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetExpensesQuery(pageIndex, pageSize, projectId, storageId, fromDate, toDate, status, search));
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

    [HttpGet("daily")]
    public async Task<ActionResult<ApiResponse<List<DailyExpenseGroupDto>>>> GetDailyExpenses(
        [FromQuery] int projectId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetDailyExpensesByProjectQuery(projectId, fromDate, toDate));
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

    [HttpDelete("{id}")]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteExpense(int id)
    {
        var result = await Mediator.Send(new DeleteExpenseCommand(id));
        return Ok(result);
    }

    [HttpPost("export-pdf")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<IActionResult> ExportExpensesPdf([FromQuery] int? projectId = null, [FromQuery] string? materialName = null, [FromQuery] string? descriptionSearch = null)
    {
        var result = await Mediator.Send(new ExportProjectExpensesPdfQuery(projectId, materialName, descriptionSearch));
        if (!result.Success || result.Data == null) return BadRequest(result);
        return File(result.Data.FileBytes, result.Data.ContentType, result.Data.FileName);
    }
}
