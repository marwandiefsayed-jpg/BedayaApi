using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Reports.DTOs;
using BedayaGroup.Application.Reports.Queries;
using BedayaGroup.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/reports")]
[Authorize]
public class ReportsController : ApiControllerBase
{
    [HttpPost("storage-activity/export-pdf")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<IActionResult> ExportStorageActivityPdf(
        [FromQuery] int? projectId,
        [FromQuery] int? cashStorageId,
        [FromQuery] CashTransactionType? type,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? descriptionSearch,
        [FromQuery] bool sortByNewest = true)
    {
        var result = await Mediator.Send(new ExportStorageActivityPdfQuery(projectId, cashStorageId, type, fromDate, toDate, descriptionSearch, sortByNewest));
        if (!result.Success || result.Data == null) return BadRequest(result);
        return File(result.Data.FileBytes, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// تصدير تقرير سجل المقبوضات وتوزيع المبالغ بصيغة PDF
    /// POST /api/reports/receipts-distribution/export-pdf
    /// </summary>
    [HttpPost("receipts-distribution/export-pdf")]
    [Authorize(Policy = "ShareholdersAccess")]
    public async Task<IActionResult> ExportReceiptsDistributionPdf([FromBody] ExportReceiptsDistributionPdfRequest request)
    {
        var result = await Mediator.Send(new ExportReceiptsDistributionPdfQuery(request));
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
