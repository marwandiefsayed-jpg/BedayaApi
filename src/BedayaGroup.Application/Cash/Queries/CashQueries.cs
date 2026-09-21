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
        if (!await _context.CashStorages.AnyAsync(cs => cs.Type != CashStorageType.Project, cancellationToken))
        {
            _context.CashStorages.AddRange(
                new Domain.Entities.CashStorage
                {
                    Name = "خزينة المكتب",
                    Type = CashStorageType.Company,
                    OpeningBalance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Domain.Entities.CashStorage
                {
                    Name = "خزينة بنكية",
                    Type = CashStorageType.Calculator,
                    OpeningBalance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await _context.SaveChangesAsync(cancellationToken);
        }

        var query = _context.CashStorages
            .Include(cs => cs.Project)
            .Include(cs => cs.CashTransactions)
            .AsNoTracking()
            .AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(cs => cs.Type == CashStorageType.Project && cs.ProjectId == request.ProjectId.Value);
        }
        else
        {
            // Manual company storages only; project storages are fetched per project.
            query = query.Where(cs => cs.Type != CashStorageType.Project);
        }

        var storages = await query.ToListAsync(cancellationToken);
        var resultList = new List<CashStorageDto>();

        foreach (var cs in storages)
        {
            var totalIn = cs.CashTransactions
                .Where(t => t.Type == CashTransactionType.CashIn || t.Type == CashTransactionType.OwnerDeposit || t.Type == CashTransactionType.OtherIncome || t.Type == CashTransactionType.ShareholderContribution)
                .Sum(t => t.Amount);

            var totalOut = cs.CashTransactions
                .Where(t => t.Type == CashTransactionType.CashOut || t.Type == CashTransactionType.ExpensePayment || t.Type == CashTransactionType.OtherExpense)
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
    DateTime? ToDate = null,
    string? DescriptionSearch = null,
    bool SortByNewest = true
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
            .Include(ct => ct.CreatedByUser)
            .AsNoTracking()
            .AsQueryable();

        if (request.CashStorageId.HasValue) query = query.Where(ct => ct.CashStorageId == request.CashStorageId.Value);
        if (request.ProjectId.HasValue)
        {
            // A project's storage activity belongs exclusively to that project's vault.
            query = query.Where(ct => ct.ProjectId == request.ProjectId.Value && ct.CashStorage.Type == CashStorageType.Project);
        }
        else
        {
            // The company cash page must never expose project-vault activity, even when a storage id is supplied.
            query = query.Where(ct => ct.CashStorage.Type != CashStorageType.Project);
        }
        if (request.Type.HasValue) query = query.Where(ct => ct.Type == request.Type.Value);
        if (!string.IsNullOrWhiteSpace(request.DescriptionSearch))
        {
            var search = request.DescriptionSearch.Trim().ToLower();
            query = query.Where(ct =>
                (ct.Description != null && ct.Description.ToLower().Contains(search)) ||
                (ct.Expense != null && ((ct.Expense.MaterialName != null && ct.Expense.MaterialName.ToLower().Contains(search)) || ct.Expense.Description.ToLower().Contains(search))));
        }
        if (request.FromDate.HasValue)
        {
            var startOfDay = request.FromDate.Value.Date;
            query = query.Where(ct => ct.TransactionDate >= startOfDay);
        }
        if (request.ToDate.HasValue)
        {
            var dayAfterEnd = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(ct => ct.TransactionDate < dayAfterEnd);
        }

        var orderedQuery = request.SortByNewest
            ? query.OrderByDescending(ct => ct.TransactionDate)
            : query.OrderBy(ct => ct.TransactionDate);

        var projectedQuery = orderedQuery
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
                ct.Type == CashTransactionType.ShareholderContribution
                    ? "مساهمة من الساهم: " + _context.ShareholderContributions
                        .Where(c => c.TransactionId == ct.Id)
                        .Select(c => c.Shareholder.Name)
                        .FirstOrDefault()
                    : ct.Expense != null
                        ? "سداد مصروف: " + (ct.Expense.MaterialName ?? ct.Expense.Description)
                        : ct.Description,
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
        CashTransactionType.OwnerDeposit => "إيداع مالك الشركة",
        CashTransactionType.OtherIncome => "إيراد آخر",
        CashTransactionType.OtherExpense => "مصروف آخر",
        CashTransactionType.ShareholderContribution => "مساهمة مساهم",
        _ => type.ToString()
    };
}
