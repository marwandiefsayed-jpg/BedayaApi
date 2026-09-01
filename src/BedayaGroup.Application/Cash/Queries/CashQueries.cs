using BedayaGroup.Application.Cash.DTOs;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Cash.Queries;

public record GetCashStoragesQuery(int? ProjectId = null) : IRequest<ApiResponse<List<CashStorageDto>>>;

public class GetCashStoragesQueryHandler : IRequestHandler<GetCashStoragesQuery, ApiResponse<List<CashStorageDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCashStoragesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<CashStorageDto>>> Handle(GetCashStoragesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.CashStorages
            .Include(cs => cs.Project)
            .Include(cs => cs.CashTransactions)
            .AsNoTracking()
            .AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(cs => cs.ProjectId == request.ProjectId.Value || cs.Type == CashStorageType.Company);
        }

        var storages = await query.ToListAsync(cancellationToken);
        var resultList = new List<CashStorageDto>();

        foreach (var cs in storages)
        {
            var totalIn = cs.CashTransactions
                .Where(t => t.Type == CashTransactionType.CashIn || t.Type == CashTransactionType.AdvanceReturned || t.Type == CashTransactionType.OwnerDeposit || t.Type == CashTransactionType.OtherIncome || t.Type == CashTransactionType.ShareholderContribution)
                .Sum(t => t.Amount);

            var totalOut = cs.CashTransactions
                .Where(t => t.Type == CashTransactionType.CashOut || t.Type == CashTransactionType.ExpensePayment || t.Type == CashTransactionType.AdvanceGiven || t.Type == CashTransactionType.OtherExpense)
                .Sum(t => t.Amount);

            var currentBalance = cs.OpeningBalance + totalIn - totalOut;

            resultList.Add(new CashStorageDto(
                cs.Id,
                cs.Name,
                cs.Type,
                cs.OpeningBalance,
                cs.Location,
                cs.ProjectId,
                cs.Project?.Name,
                cs.IsActive,
                cs.CreatedAt,
                currentBalance
            ));
        }

        return ApiResponse<List<CashStorageDto>>.SuccessResult(resultList);
    }
}

public record GetCashTransactionsQuery(
    int PageIndex = 1,
    int PageSize = 10,
    int? CashStorageId = null,
    int? ProjectId = null,
    CashTransactionType? Type = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<ApiResponse<PaginatedList<CashTransactionDto>>>;

public class GetCashTransactionsQueryHandler : IRequestHandler<GetCashTransactionsQuery, ApiResponse<PaginatedList<CashTransactionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCashTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<CashTransactionDto>>> Handle(GetCashTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.CashTransactions
            .Include(ct => ct.CashStorage)
            .Include(ct => ct.Project)
            .Include(ct => ct.Expense)
            .Include(ct => ct.Advance)
            .Include(ct => ct.CreatedByUser)
            .AsNoTracking()
            .AsQueryable();

        if (request.CashStorageId.HasValue) query = query.Where(ct => ct.CashStorageId == request.CashStorageId.Value);
        if (request.ProjectId.HasValue) query = query.Where(ct => ct.ProjectId == request.ProjectId.Value);
        if (request.Type.HasValue) query = query.Where(ct => ct.Type == request.Type.Value);
        if (request.FromDate.HasValue) query = query.Where(ct => ct.TransactionDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(ct => ct.TransactionDate <= request.ToDate.Value);

        var projectedQuery = query.OrderByDescending(ct => ct.TransactionDate)
            .Select(ct => new CashTransactionDto(
                ct.Id,
                ct.TransactionNumber,
                ct.TransactionDate,
                ct.Type,
                GetArabicTransactionTypeName(ct.Type),
                ct.Amount,
                ct.CashStorageId,
                ct.CashStorage.Name,
                ct.ProjectId,
                ct.Project != null ? ct.Project.Name : null,
                ct.ExpenseId,
                ct.Expense != null ? ct.Expense.ExpenseNumber : null,
                ct.AdvanceId,
                ct.Advance != null ? ct.Advance.AdvanceNumber : null,
                ct.Description,
                ct.ReferenceNumber,
                ct.CreatedByUserId,
                ct.CreatedByUser.FullName,
                ct.CreatedAt,
                ct.Notes
            ));

        var result = await PaginatedList<CashTransactionDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<CashTransactionDto>>.SuccessResult(result);
    }

    private static string GetArabicTransactionTypeName(CashTransactionType type) => type switch
    {
        CashTransactionType.ExpensePayment => "سداد مصروف",
        CashTransactionType.CashIn => "إيراد نقدي",
        CashTransactionType.CashOut => "مصروف نقدي",
        CashTransactionType.AdvanceGiven => "صرف عهدة",
        CashTransactionType.AdvanceReturned => "رد عهدة",
        CashTransactionType.OwnerDeposit => "إيداع مالك الشركة",
        CashTransactionType.OtherIncome => "إيراد آخر",
        CashTransactionType.OtherExpense => "مصروف آخر",
        CashTransactionType.ShareholderContribution => "مساهمة مساهم",
        _ => type.ToString()
    };
}
