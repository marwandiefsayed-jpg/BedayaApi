using BedayaGroup.Application.AuditLogs.DTOs;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.AuditLogs.Queries;

public record GetAuditLogsQuery(
    int PageIndex = 1,
    int PageSize = 20,
    int? UserId = null,
    string? EntityName = null,
    string? Action = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<ApiResponse<PaginatedList<AuditLogDto>>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, ApiResponse<PaginatedList<AuditLogDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (request.UserId.HasValue) query = query.Where(a => a.UserId == request.UserId.Value);
        if (!string.IsNullOrWhiteSpace(request.EntityName)) query = query.Where(a => a.EntityName == request.EntityName);
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(a => a.Action == request.Action);
        if (request.FromDate.HasValue) query = query.Where(a => a.CreatedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(a => a.CreatedAt <= request.ToDate.Value);

        var projectedQuery = query.OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto(
                a.Id,
                a.UserId,
                a.User != null ? a.User.FullName : null,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.CreatedAt,
                a.IpAddress
            ));

        var result = await PaginatedList<AuditLogDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<AuditLogDto>>.SuccessResult(result);
    }
}
