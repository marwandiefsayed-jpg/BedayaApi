using System.Text.RegularExpressions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Reports.DTOs;
using BedayaGroup.Application.Suppliers.DTOs;
using BedayaGroup.Application.Suppliers.Queries;
using MediatR;

namespace BedayaGroup.Application.Reports.Queries;

public record ExportSupplierStatementPdfQuery(ExportSupplierStatementPdfRequest Request)
    : IRequest<ApiResponse<ExportPdfResultDto>>;

public class ExportSupplierStatementPdfQueryHandler : IRequestHandler<ExportSupplierStatementPdfQuery, ApiResponse<ExportPdfResultDto>>
{
    private readonly IMediator _mediator;
    private readonly ISupplierStatementPdfGenerator _pdfGenerator;
    private readonly IAuditService _auditService;

    public ExportSupplierStatementPdfQueryHandler(IMediator mediator, ISupplierStatementPdfGenerator pdfGenerator, IAuditService auditService)
    {
        _mediator = mediator;
        _pdfGenerator = pdfGenerator;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ExportPdfResultDto>> Handle(ExportSupplierStatementPdfQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;
        if (request.SupplierId <= 0)
            return ApiResponse<ExportPdfResultDto>.FailureResult("المورد مطلوب");
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
            return ApiResponse<ExportPdfResultDto>.FailureResult("يجب أن يكون تاريخ البداية قبل تاريخ النهاية أو مساوياً له");

        var statementResult = await _mediator.Send(
            new GetSupplierStatementQuery(request.SupplierId, request.FromDate, request.ToDate), cancellationToken);
        if (!statementResult.Success || statementResult.Data is null)
            return ApiResponse<ExportPdfResultDto>.FailureResult(statementResult.Message ?? "تعذر إنشاء كشف حساب المورد");

        var statement = statementResult.Data;
        var bytes = _pdfGenerator.GenerateSupplierStatementPdf(statement, request.FromDate, request.ToDate, DateTime.UtcNow.AddHours(3));
        await _auditService.LogAsync("تصدير كشف حساب مورد PDF", "Supplier", statement.SupplierId.ToString(), null,
            new { request.SupplierId, request.FromDate, request.ToDate, ExportedAt = DateTime.UtcNow }, cancellationToken);

        var name = SanitizeFileName(statement.SupplierName);
        var fileName = $"كشف_حساب_مورد_{name}_{DateTime.Now:yyyy-MM-dd}.pdf";
        return ApiResponse<ExportPdfResultDto>.SuccessResult(new ExportPdfResultDto(bytes, fileName, "application/pdf"));
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        return Regex.Replace(name, $"[{invalid}\\s]+", "_").Trim('_');
    }
}
