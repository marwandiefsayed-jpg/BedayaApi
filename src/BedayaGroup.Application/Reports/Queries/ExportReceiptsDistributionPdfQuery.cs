using System.Text.RegularExpressions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Reports.Queries;

public record ExportReceiptsDistributionPdfQuery(ExportReceiptsDistributionPdfRequest Request)
    : IRequest<ApiResponse<ExportPdfResultDto>>;

public class ExportReceiptsDistributionPdfQueryHandler
    : IRequestHandler<ExportReceiptsDistributionPdfQuery, ApiResponse<ExportPdfResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IReceiptsPdfGenerator _pdfGenerator;
    private readonly IAuditService _auditService;

    public ExportReceiptsDistributionPdfQueryHandler(
        IApplicationDbContext context,
        IReceiptsPdfGenerator pdfGenerator,
        IAuditService auditService)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ExportPdfResultDto>> Handle(
        ExportReceiptsDistributionPdfQuery query,
        CancellationToken cancellationToken)
    {
        var req = query.Request;

        // 1. Validate Date Range
        if (req.FromDate.HasValue && req.ToDate.HasValue && req.FromDate.Value > req.ToDate.Value)
        {
            return ApiResponse<ExportPdfResultDto>.FailureResult("تاريخ البداية يجب أن يكون قبل أو يساوي تاريخ النهاية");
        }

        // 2. Fetch Filter Display Names
        string? projectName = null;
        if (req.ProjectId.HasValue)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, cancellationToken);

            if (project == null)
            {
                return ApiResponse<ExportPdfResultDto>.FailureResult("المشروع المحدد غير موجود");
            }
            projectName = project.Name;
        }

        string? shareName = null;
        if (req.ShareId.HasValue)
        {
            var share = await _context.Shares
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == req.ShareId.Value, cancellationToken);

            if (share == null)
            {
                return ApiResponse<ExportPdfResultDto>.FailureResult("السهم المحدد غير موجود");
            }
            shareName = share.Name;
        }

        string? shareholderName = null;
        if (req.ShareholderId.HasValue)
        {
            var shareholder = await _context.Shareholders
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == req.ShareholderId.Value, cancellationToken);

            if (shareholder == null)
            {
                return ApiResponse<ExportPdfResultDto>.FailureResult("المساهم المحدد غير موجود");
            }

            // Display data is always taken from the database, never from the client request.
            shareholderName = shareholder.Name;
        }

        // 3. Build EF Core Query for Contributions (Payments)
        var dbQuery = _context.ShareholderContributions
            .Include(sc => sc.Shareholder)
                .ThenInclude(s => s.Share)
            .Include(sc => sc.Shareholder)
                .ThenInclude(s => s.Project)
            .Include(sc => sc.Project)
            .AsNoTracking()
            .AsQueryable();

        if (req.ProjectId.HasValue)
        {
            var pId = req.ProjectId.Value;
            dbQuery = dbQuery.Where(sc => sc.ProjectId == pId || (sc.ProjectId == null && sc.Shareholder.ProjectId == pId));
        }

        if (req.ShareholderId.HasValue)
        {
            var shId = req.ShareholderId.Value;
            dbQuery = dbQuery.Where(sc => sc.ShareholderId == shId);
        }

        if (req.ShareId.HasValue)
        {
            var sId = req.ShareId.Value;
            dbQuery = dbQuery.Where(sc => sc.Shareholder.ShareId == sId);
        }

        if (req.FromDate.HasValue)
        {
            var fromDate = req.FromDate.Value.Date;
            dbQuery = dbQuery.Where(sc => sc.ContributionDate >= fromDate);
        }

        if (req.ToDate.HasValue)
        {
            var toDate = req.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            dbQuery = dbQuery.Where(sc => sc.ContributionDate <= toDate);
        }

        var contributions = await dbQuery
            .OrderBy(sc => sc.ContributionDate)
            .ThenBy(sc => sc.Id)
            .ToListAsync(cancellationToken);

        // 4. Fetch Stored Payment Allocations (Without Recalculating)
        var contributionIds = contributions.Select(c => c.Id).ToList();

        var allocationsList = await _context.ShareholderPaymentAllocations
            .Include(a => a.ProjectInstallment)
            .Where(a => contributionIds.Contains(a.ShareholderContributionId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allocationsByContribution = allocationsList
            .GroupBy(a => a.ShareholderContributionId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => new AllocationItemDto(
                    a.ProjectInstallmentId,
                    a.ProjectInstallment?.Name ?? $"دفعة #{a.ProjectInstallmentId}",
                    a.AmountAllocated
                )).ToList()
            );

        // 5. Map to DTOs
        var receiptItems = contributions.Select(sc => new ReceiptItemDto(
            sc.Id,
            sc.ContributionDate,
            sc.Shareholder?.Name ?? "غير محدد",
            sc.Shareholder?.Phone,
            sc.Project?.Name ?? sc.Shareholder?.Project?.Name,
            sc.Shareholder?.NumberOfShares ?? 0,
            sc.Amount,
            sc.Description,
            allocationsByContribution.GetValueOrDefault(sc.Id, new List<AllocationItemDto>())
        )).ToList();

        var reportData = new ReceiptsDistributionReportDto(
            projectName,
            shareName,
            req.FromDate,
            req.ToDate,
            DateTime.UtcNow.AddHours(3), // Local Egypt Time
            receiptItems,
            shareholderName
        );

        // 6. Generate PDF Document in Memory
        var pdfBytes = _pdfGenerator.GenerateReceiptsDistributionPdf(reportData);

        // 7. Audit Logging
        await _auditService.LogAsync(
            action: "Export Receipts Distribution PDF",
            entityName: "Report",
            entityId: "ReceiptsDistribution",
            oldValues: null,
            newValues: new
            {
                req.ProjectId,
                req.ShareholderId,
                req.ShareId,
                req.FromDate,
                req.ToDate,
                ExportedAt = DateTime.UtcNow
            },
            cancellationToken: cancellationToken
        );

        // 8. Format Filename
        var dateStamp = DateTime.Now.ToString("yyyy-MM-dd");
        var fileName = BuildFileName(projectName, shareholderName, shareName, dateStamp);

        var result = new ExportPdfResultDto(pdfBytes, fileName, "application/pdf");
        return ApiResponse<ExportPdfResultDto>.SuccessResult(result);
    }

    private static string BuildFileName(
        string? projectName,
        string? shareholderName,
        string? shareName,
        string dateStamp)
    {
        var parts = new List<string> { "سجل_المقبوضات_وتوزيع_المبالغ" };

        if (!string.IsNullOrWhiteSpace(shareholderName)) parts.Add($"المساهم_{SanitizeFileName(shareholderName)}");
        if (!string.IsNullOrWhiteSpace(projectName)) parts.Add($"المشروع_{SanitizeFileName(projectName)}");
        if (!string.IsNullOrWhiteSpace(shareName)) parts.Add($"السهم_{SanitizeFileName(shareName)}");
        parts.Add(dateStamp);

        return $"{string.Join("_", parts)}.pdf";
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"[{0}\s]+", invalidChars);
        return Regex.Replace(name, invalidRegStr, "_").Trim('_');
    }
}
