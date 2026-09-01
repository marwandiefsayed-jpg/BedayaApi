using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Projects.DTOs;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Projects.Queries;

public record GetProjectsQuery(int PageIndex = 1, int PageSize = 10, string? Search = null, ProjectStatus? Status = null) : IRequest<ApiResponse<PaginatedList<ProjectDto>>>;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, ApiResponse<PaginatedList<ProjectDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<ProjectDto>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Projects.Include(p => p.ProjectOwner).Include(p => p.Floors).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p => p.Name.Contains(request.Search) || p.Code.Contains(request.Search) || (p.Location != null && p.Location.Contains(request.Search)));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        var projectedQuery = query.OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectDto(
                p.Id,
                p.Code,
                p.Name,
                p.Location,
                p.Description,
                p.Budget,
                p.StartDate,
                p.ExpectedEndDate,
                p.ActualEndDate,
                p.Status,
                p.ProjectOwnerId,
                p.ProjectOwner != null ? p.ProjectOwner.FullName : null,
                p.IsActive,
                p.CreatedAt,
                p.Floors.Count
            ));

        var result = await PaginatedList<ProjectDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<ProjectDto>>.SuccessResult(result);
    }
}

public record GetProjectByIdQuery(int Id) : IRequest<ApiResponse<ProjectDto>>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ApiResponse<ProjectDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ProjectDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectOwner)
            .Include(p => p.Floors)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project == null)
        {
            throw new NotFoundException("المشروع غير موجود");
        }

        var dto = new ProjectDto(
            project.Id,
            project.Code,
            project.Name,
            project.Location,
            project.Description,
            project.Budget,
            project.StartDate,
            project.ExpectedEndDate,
            project.ActualEndDate,
            project.Status,
            project.ProjectOwnerId,
            project.ProjectOwner?.FullName,
            project.IsActive,
            project.CreatedAt,
            project.Floors.Count
        );

        return ApiResponse<ProjectDto>.SuccessResult(dto);
    }
}

public record GetProjectFinancialSummaryQuery(int ProjectId) : IRequest<ApiResponse<ProjectFinancialSummaryDto>>;

public class GetProjectFinancialSummaryQueryHandler : IRequestHandler<GetProjectFinancialSummaryQuery, ApiResponse<ProjectFinancialSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectFinancialSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ProjectFinancialSummaryDto>> Handle(GetProjectFinancialSummaryQuery request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);
        if (project == null)
        {
            throw new NotFoundException("المشروع غير موجود");
        }

        // Aggregate total expenses for this project
        var totalExpenses = await _context.Expenses
            .Where(e => e.ProjectId == request.ProjectId)
            .SumAsync(e => (decimal?)e.TotalAmount, cancellationToken) ?? 0m;

        // Aggregate valid expense payments for this project
        var totalPaidExpenses = await _context.CashTransactions
            .Where(ct => ct.ProjectId == request.ProjectId && ct.ExpenseId != null && ct.Type == CashTransactionType.ExpensePayment)
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        var totalOutstandingExpenses = totalExpenses - totalPaidExpenses;

        // Aggregate cash in vs cash out for this project
        var cashIn = await _context.CashTransactions
            .Where(ct => ct.ProjectId == request.ProjectId && (ct.Type == CashTransactionType.CashIn || ct.Type == CashTransactionType.ShareholderContribution || ct.Type == CashTransactionType.OwnerDeposit || ct.Type == CashTransactionType.OtherIncome))
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        var cashOut = await _context.CashTransactions
            .Where(ct => ct.ProjectId == request.ProjectId && (ct.Type == CashTransactionType.CashOut || ct.Type == CashTransactionType.ExpensePayment || ct.Type == CashTransactionType.AdvanceGiven || ct.Type == CashTransactionType.OtherExpense))
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        var remainingBudget = project.Budget - totalExpenses;

        var summary = new ProjectFinancialSummaryDto(
            project.Id,
            project.Code,
            project.Name,
            project.Budget,
            totalExpenses,
            totalPaidExpenses,
            totalOutstandingExpenses,
            cashIn,
            cashOut,
            remainingBudget
        );

        return ApiResponse<ProjectFinancialSummaryDto>.SuccessResult(summary);
    }
}
