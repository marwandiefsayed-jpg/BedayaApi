using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Storages.DTOs;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Storages.Queries;

public record GetStoragesQuery(int? ProjectId = null) : IRequest<ApiResponse<List<StorageDto>>>;

public class GetStoragesQueryHandler : IRequestHandler<GetStoragesQuery, ApiResponse<List<StorageDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStoragesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<StorageDto>>> Handle(GetStoragesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Storages.Include(s => s.Project).AsNoTracking().AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(s => s.ProjectId == request.ProjectId.Value || s.Type == StorageType.Company);
        }

        var storages = await query
            .Select(s => new StorageDto(
                s.Id,
                s.Name,
                s.Type,
                s.ProjectId,
                s.Project != null ? s.Project.Name : null,
                s.Location,
                s.Description,
                s.IsActive,
                s.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<StorageDto>>.SuccessResult(storages);
    }
}

public record GetStorageTransactionsQuery(
    int StorageId,
    int PageIndex = 1,
    int PageSize = 10,
    string? MaterialName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<ApiResponse<PaginatedList<StorageTransactionDto>>>;

public class GetStorageTransactionsQueryHandler : IRequestHandler<GetStorageTransactionsQuery, ApiResponse<PaginatedList<StorageTransactionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStorageTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<StorageTransactionDto>>> Handle(GetStorageTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StorageTransactions
            .Include(st => st.Storage)
            .Include(st => st.Project)
            .Include(st => st.CreatedByUser)
            .Where(st => st.StorageId == request.StorageId)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.MaterialName)) query = query.Where(st => st.MaterialName.Contains(request.MaterialName));
        if (request.FromDate.HasValue) query = query.Where(st => st.TransactionDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(st => st.TransactionDate <= request.ToDate.Value);

        var projectedQuery = query.OrderByDescending(st => st.TransactionDate)
            .Select(st => new StorageTransactionDto(
                st.Id,
                st.StorageId,
                st.Storage.Name,
                st.ProjectId,
                st.Project != null ? st.Project.Name : null,
                st.TransactionDate,
                st.MaterialName,
                st.Unit,
                st.Quantity,
                st.Type,
                GetArabicStorageTransactionTypeName(st.Type),
                st.ReferenceNumber,
                st.Description,
                st.CreatedByUserId,
                st.CreatedByUser.FullName,
                st.CreatedAt
            ));

        var result = await PaginatedList<StorageTransactionDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<StorageTransactionDto>>.SuccessResult(result);
    }

    private static string GetArabicStorageTransactionTypeName(StorageTransactionType type) => type switch
    {
        StorageTransactionType.Purchase => "شراء",
        StorageTransactionType.TransferIn => "تحويل وارد",
        StorageTransactionType.TransferOut => "تحويل صادر",
        StorageTransactionType.IssueToProject => "صرف للمشروع",
        StorageTransactionType.Return => "مرتجع",
        StorageTransactionType.Adjustment => "تسوية",
        _ => type.ToString()
    };
}

public record GetStorageBalancesQuery(int StorageId) : IRequest<ApiResponse<List<StorageMaterialBalanceDto>>>;

public class GetStorageBalancesQueryHandler : IRequestHandler<GetStorageBalancesQuery, ApiResponse<List<StorageMaterialBalanceDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStorageBalancesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<StorageMaterialBalanceDto>>> Handle(GetStorageBalancesQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _context.StorageTransactions
            .Where(st => st.StorageId == request.StorageId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var groups = transactions.GroupBy(t => new { t.MaterialName, t.Unit });
        var result = new List<StorageMaterialBalanceDto>();

        foreach (var group in groups)
        {
            var totalIn = group
                .Where(t => t.Type == StorageTransactionType.Purchase || t.Type == StorageTransactionType.TransferIn || t.Type == StorageTransactionType.Return || t.Type == StorageTransactionType.Adjustment)
                .Sum(t => t.Quantity);

            var totalOut = group
                .Where(t => t.Type == StorageTransactionType.TransferOut || t.Type == StorageTransactionType.IssueToProject)
                .Sum(t => t.Quantity);

            var balance = totalIn - totalOut;

            result.Add(new StorageMaterialBalanceDto(group.Key.MaterialName, group.Key.Unit, totalIn, totalOut, balance));
        }

        return ApiResponse<List<StorageMaterialBalanceDto>>.SuccessResult(result);
    }
}
