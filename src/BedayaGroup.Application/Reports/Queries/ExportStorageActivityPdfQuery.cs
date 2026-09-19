using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Reports.DTOs;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Reports.Queries;

public record ExportStorageActivityPdfQuery(int? ProjectId, int? CashStorageId = null, CashTransactionType? Type = null) : IRequest<ApiResponse<ExportPdfResultDto>>;

public class ExportStorageActivityPdfQueryHandler(IApplicationDbContext context, IReceiptsPdfGenerator pdfGenerator) : IRequestHandler<ExportStorageActivityPdfQuery, ApiResponse<ExportPdfResultDto>>
{
    public async Task<ApiResponse<ExportPdfResultDto>> Handle(ExportStorageActivityPdfQuery request, CancellationToken cancellationToken)
    {
        var transactions = context.CashTransactions.Include(t => t.CashStorage).Include(t => t.Project).AsNoTracking();
        if (request.ProjectId.HasValue) transactions = transactions.Where(t => t.ProjectId == request.ProjectId && t.CashStorage.Type == CashStorageType.Project);
        else transactions = transactions.Where(t => t.CashStorage.Type != CashStorageType.Project);
        if (request.CashStorageId.HasValue) transactions = transactions.Where(t => t.CashStorageId == request.CashStorageId.Value);
        if (request.Type.HasValue) transactions = transactions.Where(t => t.Type == request.Type.Value);
        var rows = await transactions.OrderByDescending(t => t.TransactionDate).Select(t => new StorageActivityPdfItemDto(
            t.TransactionDate,
            t.Type.ToString(),
            t.Project != null ? t.Project.Name : null,
            t.Amount,
            t.Type == CashTransactionType.ShareholderContribution
                ? "مساهمة من الساهم: " + context.ShareholderContributions
                    .Where(c => c.TransactionId == t.Id)
                    .Select(c => c.Shareholder.Name)
                    .FirstOrDefault()
                : t.Expense != null
                    ? "سداد مصروف: " + (t.Expense.MaterialName ?? t.Expense.Description)
                    : t.Description ?? "",
            t.ReferenceNumber)).ToListAsync(cancellationToken);
        var projectName = request.ProjectId.HasValue ? await context.Projects.Where(p => p.Id == request.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken) : null;
        var title = request.ProjectId.HasValue ? "خزينة المشروع" : "خزائن الشركة";
        return ApiResponse<ExportPdfResultDto>.SuccessResult(new ExportPdfResultDto(pdfGenerator.GenerateStorageActivityPdf(new StorageActivityPdfReportDto(title, projectName, DateTime.UtcNow.AddHours(3), rows)), $"حركة_{title}_{DateTime.Now:yyyy-MM-dd}.pdf", "application/pdf"));
    }
}
