using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Suppliers.DTOs;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Suppliers.Queries;

public record GetSuppliersQuery(int PageIndex = 1, int PageSize = 10, string? Search = null, SupplierType? Type = null, int? ProjectId = null) : IRequest<ApiResponse<PaginatedList<SupplierDto>>>;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, ApiResponse<PaginatedList<SupplierDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetSuppliersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<SupplierDto>>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Suppliers.Include(s => s.Expenses).ThenInclude(e => e.CashTransactions).Include(s => s.Project).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(s => s.Name.Contains(request.Search) || s.Code.Contains(request.Search) || (s.Phone != null && s.Phone.Contains(request.Search)));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(s => s.Type == request.Type.Value);
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(s => s.ProjectId == request.ProjectId.Value);
        }

        var projectedQuery = query.OrderByDescending(s => s.CreatedAt)
            .Select(s => new SupplierDto(
                s.Id,
                s.Code,
                s.Name,
                s.Type,
                s.Phone,
                s.Address,
                s.OpeningBalance,
                s.Notes,
                s.IsActive,
                s.CreatedAt,
                s.Expenses.Sum(e => e.TotalAmount),
                s.Expenses.SelectMany(e => e.CashTransactions).Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount),
                s.OpeningBalance + s.Expenses.Sum(e => e.TotalAmount) - s.Expenses.SelectMany(e => e.CashTransactions).Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount),
                s.ProjectId,
                s.Project != null ? s.Project.Name : null
            ));

        var result = await PaginatedList<SupplierDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<SupplierDto>>.SuccessResult(result);
    }
}

public record GetSupplierByIdQuery(int Id) : IRequest<ApiResponse<SupplierDto>>;

public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, ApiResponse<SupplierDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSupplierByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<SupplierDto>> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Expenses)
            .ThenInclude(e => e.CashTransactions)
            .Include(s => s.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("المورد غير موجود");
        }

        var totalExpenses = supplier.Expenses.Sum(e => e.TotalAmount);
        var totalPaid = supplier.Expenses.SelectMany(e => e.CashTransactions).Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount);
        var currentBalance = supplier.OpeningBalance + totalExpenses - totalPaid;

        var dto = new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.Type, supplier.Phone, supplier.Address, supplier.OpeningBalance, supplier.Notes, supplier.IsActive, supplier.CreatedAt, totalExpenses, totalPaid, currentBalance, supplier.ProjectId, supplier.Project?.Name);
        return ApiResponse<SupplierDto>.SuccessResult(dto);
    }
}

public record GetSupplierStatementQuery(int SupplierId, DateTime? FromDate = null, DateTime? ToDate = null) : IRequest<ApiResponse<SupplierStatementDto>>;

public class GetSupplierStatementQueryHandler : IRequestHandler<GetSupplierStatementQuery, ApiResponse<SupplierStatementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSupplierStatementQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<SupplierStatementDto>> Handle(GetSupplierStatementQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (supplier == null)
        {
            throw new NotFoundException("المورد غير موجود");
        }

        var expenses = await _context.Expenses
            .Where(e => e.SupplierId == request.SupplierId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var expenseIds = expenses.Select(e => e.Id).ToList();

        var payments = await _context.CashTransactions
            .Where(ct => ct.ExpenseId.HasValue && expenseIds.Contains(ct.ExpenseId.Value) && ct.Type == CashTransactionType.ExpensePayment)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var statementItems = new List<SupplierStatementItemDto>();

        foreach (var exp in expenses)
        {
            statementItems.Add(new SupplierStatementItemDto(
                exp.ExpenseDate,
                "مصروف / فاتورة",
                exp.ExpenseNumber,
                exp.Description,
                exp.TotalAmount,
                0m,
                0m
            ));
        }

        foreach (var pay in payments)
        {
            statementItems.Add(new SupplierStatementItemDto(
                pay.TransactionDate,
                "سداد نقدي",
                pay.TransactionNumber,
                pay.Description,
                0m,
                pay.Amount,
                0m
            ));
        }

        statementItems = statementItems.OrderBy(i => i.Date).ToList();

        decimal running = supplier.OpeningBalance;
        var calculatedItems = new List<SupplierStatementItemDto>();

        foreach (var item in statementItems)
        {
            running += item.DebtAmount - item.CreditAmount;
            calculatedItems.Add(item with { RunningBalance = running });
        }

        var totalInvoiced = expenses.Sum(e => e.TotalAmount);
        var totalPaid = payments.Sum(p => p.Amount);
        var currentBalance = supplier.OpeningBalance + totalInvoiced - totalPaid;

        var statement = new SupplierStatementDto(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.Type,
            supplier.OpeningBalance,
            totalInvoiced,
            totalPaid,
            currentBalance,
            supplier.ProjectId,
            supplier.Project?.Name,
            calculatedItems
        );

        return ApiResponse<SupplierStatementDto>.SuccessResult(statement);
    }
}
