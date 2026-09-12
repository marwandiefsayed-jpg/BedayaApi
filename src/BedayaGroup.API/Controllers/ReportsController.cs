using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Reports.DTOs;
using BedayaGroup.Application.Reports.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BedayaGroup.API.Controllers;

[Route("api/reports")]
[Authorize]
public class ReportsController : ApiControllerBase
{
    /// <summary>
    /// تصدير تقرير سجل المقبوضات وتوزيع المبالغ بصيغة PDF
    /// POST /api/reports/receipts-distribution/export-pdf
    /// </summary>
    [HttpPost("receipts-distribution/export-pdf")]
    [Authorize(Policy = "FinancialWriteAccess")]
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

    [HttpPost("supplier-statement/export-pdf")]
    [Authorize(Policy = "FinancialWriteAccess")]
    public async Task<IActionResult> ExportSupplierStatementPdf([FromBody] ExportSupplierStatementPdfRequest request)
    {
        var result = await Mediator.Send(new ExportSupplierStatementPdfQuery(request));
        if (!result.Success || result.Data == null)
            return BadRequest(result);

        return File(result.Data.FileBytes, result.Data.ContentType, result.Data.FileName);
    }
}
