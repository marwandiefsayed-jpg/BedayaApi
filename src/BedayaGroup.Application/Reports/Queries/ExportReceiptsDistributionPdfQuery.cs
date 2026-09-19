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

        int? effectiveShareholderId = req.ShareholderId;
        if (!effectiveShareholderId.HasValue && req.ShareholderIds != null && req.ShareholderIds.Count == 1)
        {
            effectiveShareholderId = req.ShareholderIds[0];
        }

        string? shareholderName = null;
        if (effectiveShareholderId.HasValue)
        {
            var shareholder = await _context.Shareholders
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == effectiveShareholderId.Value, cancellationToken);

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
            .Where(sc => sc.Shareholder != null && sc.Shareholder.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (req.ProjectId.HasValue)
        {
            var pId = req.ProjectId.Value;
            dbQuery = dbQuery.Where(sc => sc.ProjectId == pId || (sc.ProjectId == null && sc.Shareholder.ProjectId == pId));
        }

        if (req.ShareholderIds != null && req.ShareholderIds.Any())
        {
            var shIds = req.ShareholderIds;
            dbQuery = dbQuery.Where(sc => shIds.Contains(sc.ShareholderId));
        }
        else if (effectiveShareholderId.HasValue)
        {
            var shId = effectiveShareholderId.Value;
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
            .OrderBy(sc => sc.Shareholder != null ? sc.Shareholder.Name : "")
            .ThenByDescending(sc => sc.ContributionDate)
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

        // Calculate expected and remaining totals if generating report for a specific shareholder
        decimal totalExpected = 0m;
        decimal totalRemaining = 0m;
        decimal excessCredit = 0m;
        List<ShareholderSummaryItemDto>? shareholderSummariesList = null;

        int? targetSingleShareholderId = req.ShareholderId ?? (req.ShareholderIds != null && req.ShareholderIds.Count == 1 ? (int?)req.ShareholderIds[0] : null);

        if (targetSingleShareholderId.HasValue)
        {
            var shId = targetSingleShareholderId.Value;
            var shareholderEntity = await _context.Shareholders
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == shId, cancellationToken);

            if (shareholderEntity != null)
            {
                shareholderName = shareholderEntity.Name;
            }

            if (shareholderEntity?.ProjectId != null)
            {
                var pInsts = await _context.ProjectInstallments
                    .Where(i => i.ProjectId == shareholderEntity.ProjectId.Value && i.IsActive &&
                        (!i.TargetShareholders.Any() || i.TargetShareholders.Any(target => target.ShareholderId == shId)))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var shAllocations = await _context.ShareholderPaymentAllocations
                    .Where(a => a.ShareholderContribution.ShareholderId == shId)
                    .GroupBy(a => a.ProjectInstallmentId)
                    .Select(g => new { InstallmentId = g.Key, TotalPaid = g.Sum(a => a.AmountAllocated) })
                    .ToListAsync(cancellationToken);

                var paidLookup = shAllocations.ToDictionary(x => x.InstallmentId, x => x.TotalPaid);

                var shPenalties = await _context.ShareholderInstallmentPenalties
                    .Where(p => p.ShareholderId == shId)
                    .GroupBy(p => p.ProjectInstallmentId)
                    .Select(g => new { InstallmentId = g.Key, TotalPenalty = g.Sum(p => p.PenaltyAmount) })
                    .ToListAsync(cancellationToken);

                var penaltyLookup = shPenalties.ToDictionary(x => x.InstallmentId, x => x.TotalPenalty);

                var totalContribs = receiptItems.Where(r => r.ShareholderName == shareholderEntity.Name).Sum(r => r.AmountReceived);
                var pool = totalContribs;

                foreach (var inst in pInsts)
                {
                    var baseReq = inst.AmountPerShare * shareholderEntity.NumberOfShares;
                    var pen = penaltyLookup.GetValueOrDefault(inst.Id, 0m);
                    var reqAmt = baseReq + pen;
                    var paid = Math.Min(pool, reqAmt);
                    var rem = Math.Max(0m, reqAmt - paid);
                    pool = Math.Max(0m, pool - paid);

                    totalExpected += reqAmt;
                    totalRemaining += rem;
                }
                shareholderSummariesList = new List<ShareholderSummaryItemDto>
                {
                    new ShareholderSummaryItemDto(
                        shareholderEntity.Id,
                        shareholderEntity.Name,
                        shareholderEntity.Phone,
                        shareholderEntity.NumberOfShares,
                        shareholderEntity.Project?.Name,
                        totalExpected,
                        totalContribs,
                        totalRemaining
                    )
                };
            }
        }
        else
        {
            var activeShareholdersQuery = _context.Shareholders
                .Include(s => s.Project)
                .Where(s => s.IsActive)
                .AsNoTracking()
                .AsQueryable();

            if (req.ProjectId.HasValue)
            {
                activeShareholdersQuery = activeShareholdersQuery.Where(s => s.ProjectId == req.ProjectId.Value);
            }

            if (req.ShareId.HasValue)
            {
                activeShareholdersQuery = activeShareholdersQuery.Where(s => s.ShareId == req.ShareId.Value);
            }

            if (req.ShareholderIds != null && req.ShareholderIds.Any())
            {
                activeShareholdersQuery = activeShareholdersQuery.Where(s => req.ShareholderIds.Contains(s.Id));
            }

            var activeShareholders = await activeShareholdersQuery.ToListAsync(cancellationToken);
            var projectIds = activeShareholders.Select(s => s.ProjectId).Where(p => p.HasValue).Select(p => p!.Value).Distinct().ToList();

            var allInstallments = await _context.ProjectInstallments
                .Include(i => i.TargetShareholders)
                .Where(i => projectIds.Contains(i.ProjectId) && i.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var allPenalties = await _context.ShareholderInstallmentPenalties
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var allContribs = await _context.ShareholderContributions
                .Where(c => c.Shareholder != null && c.Shareholder.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var summaries = new List<ShareholderSummaryItemDto>();

            foreach (var sh in activeShareholders)
            {
                if (!sh.ProjectId.HasValue) continue;

                var pInsts = allInstallments.Where(i => i.ProjectId == sh.ProjectId.Value &&
                    (!i.TargetShareholders.Any() || i.TargetShareholders.Any(target => target.ShareholderId == sh.Id))).ToList();
                var shPenalties = allPenalties.Where(p => p.ShareholderId == sh.Id).GroupBy(p => p.ProjectInstallmentId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.PenaltyAmount));

                var shTotalContribs = allContribs.Where(c => c.ShareholderId == sh.Id).Sum(c => c.Amount);
                var pool = shTotalContribs;

                decimal shExpected = 0m;
                decimal shRemaining = 0m;

                foreach (var inst in pInsts)
                {
                    var baseReq = inst.AmountPerShare * sh.NumberOfShares;
                    var pen = shPenalties.GetValueOrDefault(inst.Id, 0m);
                    var reqAmt = baseReq + pen;
                    var paid = Math.Min(pool, reqAmt);
                    var rem = Math.Max(0m, reqAmt - paid);
                    pool = Math.Max(0m, pool - paid);

                    shExpected += reqAmt;
                    shRemaining += rem;
                }

                totalExpected += shExpected;
                totalRemaining += shRemaining;

                summaries.Add(new ShareholderSummaryItemDto(
                    sh.Id,
                    sh.Name,
                    sh.Phone,
                    sh.NumberOfShares,
                    sh.Project?.Name,
                    shExpected,
                    shTotalContribs,
                    shRemaining
                ));
            }

            if (req.OnlyOutstandingShareholders)
            {
                summaries = summaries.Where(s => s.TotalRemaining > 0m).ToList();
            }

            shareholderSummariesList = summaries.OrderBy(s => s.Name).ToList();
        }

        var reportData = new ReceiptsDistributionReportDto(
            projectName,
            shareName,
            req.FromDate,
            req.ToDate,
            DateTime.UtcNow.AddHours(3), // Local Egypt Time
            receiptItems,
            shareholderName,
            TotalExpected: totalExpected,
            TotalRemaining: totalRemaining,
            ExcessCredit: excessCredit,
            ShareholderSummaries: shareholderSummariesList
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
