using BedayaGroup.Application.Advances.DTOs;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Advances.Queries;

public record GetAdvancesQuery(
    int PageIndex = 1,
    int PageSize = 10,
    int? EngineerId = null,
    int? ProjectId = null,
    AdvanceStatus? Status = null
) : IRequest<ApiResponse<PaginatedList<AdvanceDto>>>;

public class GetAdvancesQueryHandler : IRequestHandler<GetAdvancesQuery, ApiResponse<PaginatedList<AdvanceDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAdvancesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<AdvanceDto>>> Handle(GetAdvancesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Advances
            .Include(a => a.Engineer)
            .Include(a => a.Project)
            .Include(a => a.CreatedByUser)
            .Include(a => a.CashTransactions)
            .AsNoTracking()
            .AsQueryable();

        if (request.EngineerId.HasValue) query = query.Where(a => a.EngineerId == request.EngineerId.Value);
        if (request.ProjectId.HasValue) query = query.Where(a => a.ProjectId == request.ProjectId.Value);
        if (request.Status.HasValue) query = query.Where(a => a.Status == request.Status.Value);

        var projectedQuery = query.OrderByDescending(a => a.IssueDate)
            .Select(a => new AdvanceDto(
                a.Id,
                a.AdvanceNumber,
                a.EngineerId,
                a.Engineer.FullName,
                a.Engineer.Code,
                a.ProjectId,
                a.Project.Name,
                a.IssuedAmount,
                a.CashTransactions.Where(ct => ct.Type == CashTransactionType.AdvanceReturned).Sum(ct => ct.Amount),
                a.IssuedAmount - a.CashTransactions.Where(ct => ct.Type == CashTransactionType.AdvanceReturned).Sum(ct => ct.Amount),
                a.IssueDate,
                a.SettlementDate,
                a.Status,
                a.CreatedByUserId,
                a.CreatedByUser.FullName,
                a.CreatedAt,
                a.Notes
            ));

        var result = await PaginatedList<AdvanceDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<AdvanceDto>>.SuccessResult(result);
    }
}

public record GetAdvanceByIdQuery(int Id) : IRequest<ApiResponse<AdvanceDto>>;

public class GetAdvanceByIdQueryHandler : IRequestHandler<GetAdvanceByIdQuery, ApiResponse<AdvanceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdvanceByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<AdvanceDto>> Handle(GetAdvanceByIdQuery request, CancellationToken cancellationToken)
    {
        var a = await _context.Advances
            .Include(a => a.Engineer)
            .Include(a => a.Project)
            .Include(a => a.CreatedByUser)
            .Include(a => a.CashTransactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (a == null)
        {
            throw new NotFoundException("العهدة غير موجودة");
        }

        var settled = a.CashTransactions.Where(ct => ct.Type == CashTransactionType.AdvanceReturned).Sum(ct => ct.Amount);
        var remaining = a.IssuedAmount - settled;

        var dto = new AdvanceDto(
            a.Id,
            a.AdvanceNumber,
            a.EngineerId,
            a.Engineer.FullName,
            a.Engineer.Code,
            a.ProjectId,
            a.Project.Name,
            a.IssuedAmount,
            settled,
            remaining,
            a.IssueDate,
            a.SettlementDate,
            a.Status,
            a.CreatedByUserId,
            a.CreatedByUser.FullName,
            a.CreatedAt,
            a.Notes
        );

        return ApiResponse<AdvanceDto>.SuccessResult(dto);
    }
}

public record GetAdvanceStatementQuery(int AdvanceId) : IRequest<ApiResponse<AdvanceStatementDto>>;

public class GetAdvanceStatementQueryHandler : IRequestHandler<GetAdvanceStatementQuery, ApiResponse<AdvanceStatementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdvanceStatementQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<AdvanceStatementDto>> Handle(GetAdvanceStatementQuery request, CancellationToken cancellationToken)
    {
        var advance = await _context.Advances
            .Include(a => a.Engineer)
            .Include(a => a.Project)
            .Include(a => a.CashTransactions)
            .ThenInclude(ct => ct.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AdvanceId, cancellationToken);

        if (advance == null)
        {
            throw new NotFoundException("العهدة غير موجودة");
        }

        var settlements = advance.CashTransactions
            .Where(ct => ct.Type == CashTransactionType.AdvanceReturned)
            .OrderByDescending(ct => ct.TransactionDate)
            .Select(ct => new AdvanceSettlementItemDto(
                ct.TransactionDate,
                ct.TransactionNumber,
                ct.Description,
                ct.Amount,
                ct.CreatedByUser.FullName
            ))
            .ToList();

        var totalSettled = settlements.Sum(s => s.Amount);
        var remaining = advance.IssuedAmount - totalSettled;

        var statement = new AdvanceStatementDto(
            advance.Id,
            advance.AdvanceNumber,
            advance.Engineer.FullName,
            advance.Project.Name,
            advance.IssuedAmount,
            totalSettled,
            remaining,
            advance.Status,
            settlements
        );

        return ApiResponse<AdvanceStatementDto>.SuccessResult(statement);
    }
}
