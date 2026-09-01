using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Storages.Commands;
using BedayaGroup.Application.Storages.DTOs;
using BedayaGroup.Application.Storages.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/storages")]
[Authorize]
public class StoragesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StorageDto>>>> GetStorages([FromQuery] int? projectId = null)
    {
        var result = await Mediator.Send(new GetStoragesQuery(projectId));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwnerOnly")]
    public async Task<ActionResult<ApiResponse<StorageDto>>> CreateStorage([FromBody] CreateStorageRequest request)
    {
        var result = await Mediator.Send(new CreateStorageCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<ApiResponse<PaginatedList<StorageTransactionDto>>>> GetStorageTransactions(
        int id,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? materialName = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetStorageTransactionsQuery(id, pageIndex, pageSize, materialName, fromDate, toDate));
        return Ok(result);
    }

    [HttpPost("{id}/transactions")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<ActionResult<ApiResponse<StorageTransactionDto>>> RecordStorageTransaction(int id, [FromBody] RecordStorageTransactionRequest request)
    {
        if (id != request.StorageId) return BadRequest(ApiResponse.FailureResult("معرف المخزن غير متطابق"));
        var result = await Mediator.Send(new RecordStorageTransactionCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}/balances")]
    public async Task<ActionResult<ApiResponse<List<StorageMaterialBalanceDto>>>> GetStorageBalances(int id)
    {
        var result = await Mediator.Send(new GetStorageBalancesQuery(id));
        return Ok(result);
    }
}
