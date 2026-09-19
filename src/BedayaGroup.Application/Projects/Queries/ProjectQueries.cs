using BedayaGroup.Application.Cash;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Projects.DTOs;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Projects.Queries;

public record GetProjectsQuery(int PageIndex = 1, int PageSize = 10, string? Search = null) : IRequest<ApiResponse<PaginatedList<ProjectDto>>>;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, ApiResponse<PaginatedList<ProjectDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<ProjectDto>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Projects.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p => p.Name.Contains(request.Search));
        }

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var projectIds = projects.Select(p => p.Id).ToList();
        var storages = await _context.CashStorages
            .AsNoTracking()
            .Include(cs => cs.CashTransactions)
            .Where(cs => cs.Type == CashStorageType.Project
                      && cs.ProjectId != null
                      && projectIds.Contains(cs.ProjectId.Value))
            .ToListAsync(cancellationToken);

        var storageByProject = storages
            .Where(cs => cs.ProjectId.HasValue)
            .GroupBy(cs => cs.ProjectId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var items = projects.Select(p =>
        {
            storageByProject.TryGetValue(p.Id, out var storage);
            return new ProjectDto(
                p.Id,
                p.Name,
                p.StartDate,
                p.IsActive,
                p.CreatedAt,
                storage?.Id,
                storage == null ? 0m : ProjectCashStorage.ComputeBalance(storage));
        }).ToList();

        var result = new PaginatedList<ProjectDto>(items, totalCount, pageIndex, pageSize);
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
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project == null)
        {
            throw new NotFoundException("المشروع غير موجود");
        }

        var storage = await ProjectCashStorage.FindAsync(_context, project.Id, cancellationToken);

        var dto = new ProjectDto(
            project.Id,
            project.Name,
            project.StartDate,
            project.IsActive,
            project.CreatedAt,
            storage?.Id,
            storage == null ? 0m : ProjectCashStorage.ComputeBalance(storage)
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
            .Where(ct => ct.ProjectId == request.ProjectId && (ct.Type == CashTransactionType.CashOut || ct.Type == CashTransactionType.ExpensePayment || ct.Type == CashTransactionType.OtherExpense))
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        var summary = new ProjectFinancialSummaryDto(
            project.Id,
            project.Name,
            totalExpenses,
            totalPaidExpenses,
            totalOutstandingExpenses,
            cashIn,
            cashOut
        );

        return ApiResponse<ProjectFinancialSummaryDto>.SuccessResult(summary);
    }
}
