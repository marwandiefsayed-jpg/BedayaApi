using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Expenses.DTOs;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Expenses.Queries;

public record GetExpensesQuery(
    int PageIndex = 1,
    int PageSize = 10,
    int? ProjectId = null,
    int? StorageId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    ExpenseStatus? Status = null,
    string? Search = null
) : IRequest<ApiResponse<PaginatedList<ExpenseDto>>>;

public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, ApiResponse<PaginatedList<ExpenseDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetExpensesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<ExpenseDto>>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Expenses
            .Include(e => e.Project)
            .Include(e => e.Storage)
            .Include(e => e.CreatedByUser)
            .Include(e => e.CashTransactions)
            .AsNoTracking()
            .AsQueryable();

        if (request.ProjectId.HasValue) query = query.Where(e => e.ProjectId == request.ProjectId.Value);
        if (request.StorageId.HasValue) query = query.Where(e => e.StorageId == request.StorageId.Value);
        if (request.FromDate.HasValue) query = query.Where(e => e.ExpenseDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(e => e.ExpenseDate <= request.ToDate.Value);
        if (request.Status.HasValue) query = query.Where(e => e.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(e =>
                e.Description.ToLower().Contains(search) ||
                (e.MaterialName != null && e.MaterialName.ToLower().Contains(search)) ||
                e.Project.Name.ToLower().Contains(search) ||
                (e.Storage != null && e.Storage.Name.ToLower().Contains(search)));
        }

        var projectedQuery = query.OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseDto(
                e.Id,
                e.ExpenseNumber,
                e.ProjectId,
                e.Project.Name,
                e.StorageId,
                e.Storage != null ? e.Storage.Name : null,
                e.ExpenseDate,
                e.Description,
                e.TotalAmount,
                e.CashTransactions.Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount),
                e.TotalAmount - e.CashTransactions.Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount),
                e.Status,
                e.CreatedByUserId,
                e.CreatedByUser.FullName,
                e.CreatedAt,
                e.Notes,
                e.MaterialName,
                e.Unit,
                e.Quantity,
                e.UnitPrice
            ));

        var result = await PaginatedList<ExpenseDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<ExpenseDto>>.SuccessResult(result);
    }
}

public record GetExpenseByIdQuery(int Id) : IRequest<ApiResponse<ExpenseDto>>;

public class GetExpenseByIdQueryHandler : IRequestHandler<GetExpenseByIdQuery, ApiResponse<ExpenseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetExpenseByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        var e = await _context.Expenses
            .Include(e => e.Project)
            .Include(e => e.Storage)
            .Include(e => e.CreatedByUser)
            .Include(e => e.CashTransactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (e == null)
        {
            throw new NotFoundException("المصروف غير موجود");
        }

        var paid = e.CashTransactions.Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount);
        var remaining = e.TotalAmount - paid;

        var dto = new ExpenseDto(
            e.Id,
            e.ExpenseNumber,
            e.ProjectId,
            e.Project.Name,
            e.StorageId,
            e.Storage?.Name,
            e.ExpenseDate,
            e.Description,
            e.TotalAmount,
            paid,
            remaining,
            e.Status,
            e.CreatedByUserId,
            e.CreatedByUser.FullName,
            e.CreatedAt,
            e.Notes,
            e.MaterialName,
            e.Unit,
            e.Quantity,
            e.UnitPrice
        );

        return ApiResponse<ExpenseDto>.SuccessResult(dto);
    }
}

public record GetDailyExpensesByProjectQuery(int ProjectId, DateTime? FromDate = null, DateTime? ToDate = null) : IRequest<ApiResponse<List<DailyExpenseGroupDto>>>;

public class GetDailyExpensesByProjectQueryHandler : IRequestHandler<GetDailyExpensesByProjectQuery, ApiResponse<List<DailyExpenseGroupDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDailyExpensesByProjectQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<DailyExpenseGroupDto>>> Handle(GetDailyExpensesByProjectQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Expenses
            .Include(e => e.Project)
            .Include(e => e.Storage)
            .Include(e => e.CreatedByUser)
            .Include(e => e.CashTransactions)
            .Where(e => e.ProjectId == request.ProjectId)
            .AsNoTracking();

        if (request.FromDate.HasValue) query = query.Where(e => e.ExpenseDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(e => e.ExpenseDate <= request.ToDate.Value);

        var expensesList = await query.ToListAsync(cancellationToken);

        var dailyGroups = expensesList
            .GroupBy(e => e.ExpenseDate.Date)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var items = g.Select(e =>
                {
                    var paid = e.CashTransactions.Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount);
                    var remaining = e.TotalAmount - paid;
                    return new ExpenseDto(
                        e.Id,
                        e.ExpenseNumber,
                        e.ProjectId,
                        e.Project.Name,
                        e.StorageId,
                        e.Storage?.Name,
                        e.ExpenseDate,
                        e.Description,
                        e.TotalAmount,
                        paid,
                        remaining,
                        e.Status,
                        e.CreatedByUserId,
                        e.CreatedByUser?.FullName ?? "",
                        e.CreatedAt,
                        e.Notes,
                        e.MaterialName,
                        e.Unit,
                        e.Quantity,
                        e.UnitPrice
                    );
                }).ToList();

                var totalAmt = items.Sum(i => i.TotalAmount);
                var totalPaid = items.Sum(i => i.PaidAmount);
                var totalRemaining = items.Sum(i => i.RemainingAmount);

                return new DailyExpenseGroupDto(
                    g.Key,
                    totalAmt,
                    totalPaid,
                    totalRemaining,
                    items.Count,
                    items
                );
            })
            .ToList();

        return ApiResponse<List<DailyExpenseGroupDto>>.SuccessResult(dailyGroups);
    }
}
