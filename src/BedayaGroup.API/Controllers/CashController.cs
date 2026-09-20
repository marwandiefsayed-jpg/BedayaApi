using BedayaGroup.Application.Cash.Commands;
using BedayaGroup.Application.Cash.DTOs;
using BedayaGroup.Application.Cash.Queries;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/cash")]
[Authorize]
public class CashController : ApiControllerBase
{
    [HttpGet("storages")]
    public async Task<ActionResult<ApiResponse<List<CashStorageDto>>>> GetCashStorages([FromQuery] int? projectId = null)
    {
        var result = await Mediator.Send(new GetCashStoragesQuery(projectId));
        return Ok(result);
    }

    [HttpPost("storages")]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<CashStorageDto>>> CreateCashStorage([FromBody] CreateCashStorageRequest request)
    {
        var result = await Mediator.Send(new CreateCashStorageCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PaginatedList<CashTransactionDto>>>> GetCashTransactions(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? cashStorageId = null,
        [FromQuery] int? projectId = null,
        [FromQuery] CashTransactionType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? descriptionSearch = null)
    {
        var result = await Mediator.Send(new GetCashTransactionsQuery(pageIndex, pageSize, cashStorageId, projectId, type, fromDate, toDate, descriptionSearch));
        return Ok(result);
    }

    [HttpPost("transactions")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<CashTransactionDto>>> RecordCashTransaction([FromBody] RecordCashTransactionRequest request)
    {
        var result = await Mediator.Send(new RecordCashTransactionCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("transactions/{transactionId}")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCompanyCashTransaction(int transactionId)
    {
        var result = await Mediator.Send(new DeleteCompanyCashTransactionCommand(transactionId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("payments/expense")]
    [Authorize(Policy = "ExpensesAccess")]
    public async Task<ActionResult<ApiResponse<CashTransactionDto>>> RecordExpensePayment([FromBody] RecordExpensePaymentRequest request)
    {
        var result = await Mediator.Send(new RecordExpensePaymentCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("payments/expense/{transactionId}")]
    [Authorize(Policy = "ExpensesAccess")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteExpensePayment(int transactionId)
    {
        var result = await Mediator.Send(new DeleteExpensePaymentCommand(transactionId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
