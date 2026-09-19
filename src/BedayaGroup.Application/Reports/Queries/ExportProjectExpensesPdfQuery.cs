using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Reports.Queries;

public record ExportProjectExpensesPdfQuery(int? ProjectId, string? MaterialName = null) : IRequest<ApiResponse<ExportPdfResultDto>>;

public class ExportProjectExpensesPdfQueryHandler : IRequestHandler<ExportProjectExpensesPdfQuery, ApiResponse<ExportPdfResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IReceiptsPdfGenerator _pdfGenerator;
    private readonly IAuditService _auditService;

    public ExportProjectExpensesPdfQueryHandler(
        IApplicationDbContext context,
        IReceiptsPdfGenerator pdfGenerator,
        IAuditService auditService)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ExportPdfResultDto>> Handle(ExportProjectExpensesPdfQuery query, CancellationToken cancellationToken)
    {
        string? projectName = null;
        if (query.ProjectId.HasValue && query.ProjectId.Value > 0)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == query.ProjectId.Value, cancellationToken);

            if (project == null)
            {
                throw new NotFoundException("المشروع المحدد غير موجود");
            }
            projectName = project.Name;
        }

        var expensesQuery = _context.Expenses.AsQueryable();

        if (query.ProjectId.HasValue && query.ProjectId.Value > 0)
        {
            expensesQuery = expensesQuery.Where(e => e.ProjectId == query.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.MaterialName))
        {
            var mat = query.MaterialName.Trim().ToLower();
            expensesQuery = expensesQuery.Where(e => e.MaterialName != null && e.MaterialName.ToLower().Contains(mat));
        }

        var expenses = await expensesQuery
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var expenseItems = expenses.Select(e => new ProjectExpensePdfItemDto(
            e.Id,
            e.ExpenseNumber,
            e.Description,
            e.MaterialName ?? "",
            e.Unit ?? "",
            e.Quantity,
            e.UnitPrice,
            e.TotalAmount,
            e.ExpenseDate
        )).ToList();

        var totalAmount = expenseItems.Sum(e => e.TotalAmount);

        var reportData = new ProjectExpensesPdfReportDto(
            projectName,
            query.MaterialName?.Trim(),
            DateTime.UtcNow.AddHours(3),
            expenseItems,
            totalAmount
        );

        var pdfBytes = _pdfGenerator.GenerateProjectExpensesPdf(reportData);

        await _auditService.LogAsync(
            action: "Export Project Expenses PDF",
            entityName: "Project",
            entityId: query.ProjectId?.ToString() ?? "All",
            oldValues: null,
            newValues: new { query.ProjectId, query.MaterialName, TotalAmount = totalAmount, Count = expenseItems.Count },
            cancellationToken: cancellationToken
        );

        var sanitizedProjectName = !string.IsNullOrEmpty(projectName)
            ? projectName.Replace(" ", "_").Replace("/", "-")
            : "كافة_المشاريع";
        
        var matTag = !string.IsNullOrWhiteSpace(query.MaterialName)
            ? $"_مادة_{query.MaterialName.Trim().Replace(" ", "_")}"
            : "";

        var fileName = $"تقرير_مصاريف_{sanitizedProjectName}{matTag}_{DateTime.Now:yyyy-MM-dd}.pdf";

        return ApiResponse<ExportPdfResultDto>.SuccessResult(new ExportPdfResultDto(pdfBytes, fileName, "application/pdf"));
    }
}
