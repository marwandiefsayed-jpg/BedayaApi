using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shares.Commands;

// =============================================
// Share Commands
// =============================================

public record CreateShareCommand(CreateShareRequest Request) : IRequest<ApiResponse<ShareDto>>;

public class CreateShareCommandHandler : IRequestHandler<CreateShareCommand, ApiResponse<ShareDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateShareCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ShareDto>> Handle(CreateShareCommand request, CancellationToken cancellationToken)
    {
        var share = new Share
        {
            Name = request.Request.Name,
            StartDate = request.Request.StartDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Shares.Add(share);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<ShareDto>.SuccessResult(new ShareDto(share.Id, share.Name, share.StartDate, share.IsActive, share.CreatedAt), "تم إنشاء السهم بنجاح");
    }
}

public record UpdateShareCommand(int Id, UpdateShareRequest Request) : IRequest<ApiResponse<ShareDto>>;

public class UpdateShareCommandHandler : IRequestHandler<UpdateShareCommand, ApiResponse<ShareDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateShareCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ShareDto>> Handle(UpdateShareCommand request, CancellationToken cancellationToken)
    {
        var share = await _context.Shares.FindAsync(new object[] { request.Id }, cancellationToken);
        if (share == null) throw new NotFoundException("السهم غير موجود");

        share.Name = request.Request.Name;
        share.StartDate = request.Request.StartDate;
        share.IsActive = request.Request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<ShareDto>.SuccessResult(new ShareDto(share.Id, share.Name, share.StartDate, share.IsActive, share.CreatedAt), "تم تحديث السهم بنجاح");
    }
}

// =============================================
// ProjectInstallment Commands
// =============================================

public record CreateProjectInstallmentCommand(CreateProjectInstallmentRequest Request) : IRequest<ApiResponse<ProjectInstallmentDto>>;

public class CreateProjectInstallmentCommandHandler : IRequestHandler<CreateProjectInstallmentCommand, ApiResponse<ProjectInstallmentDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateProjectInstallmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ProjectInstallmentDto>> Handle(CreateProjectInstallmentCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var project = await _context.Projects.FindAsync(new object[] { req.ProjectId }, cancellationToken);
        if (project == null)
        {
            return ApiResponse<ProjectInstallmentDto>.FailureResult("المشروع المحدد غير موجود");
        }

        var installment = new ProjectInstallment
        {
            ProjectId = req.ProjectId,
            Name = req.Name,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            AmountPerShare = req.AmountPerShare,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectInstallments.Add(installment);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ProjectInstallmentDto(installment.Id, installment.ProjectId, project.Name, installment.Name, installment.StartDate, installment.EndDate, installment.AmountPerShare, installment.IsActive, installment.CreatedAt);
        return ApiResponse<ProjectInstallmentDto>.SuccessResult(dto, "تم إنشاء الدفعة/القسط بنجاح");
    }
}

public record UpdateProjectInstallmentCommand(int Id, UpdateProjectInstallmentRequest Request) : IRequest<ApiResponse<ProjectInstallmentDto>>;

public class UpdateProjectInstallmentCommandHandler : IRequestHandler<UpdateProjectInstallmentCommand, ApiResponse<ProjectInstallmentDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateProjectInstallmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ProjectInstallmentDto>> Handle(UpdateProjectInstallmentCommand request, CancellationToken cancellationToken)
    {
        var installment = await _context.ProjectInstallments
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (installment == null) throw new NotFoundException("القسط غير موجود");

        var req = request.Request;
        installment.Name = req.Name;
        installment.StartDate = req.StartDate;
        installment.EndDate = req.EndDate;
        installment.AmountPerShare = req.AmountPerShare;
        installment.IsActive = req.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ProjectInstallmentDto(installment.Id, installment.ProjectId, installment.Project.Name, installment.Name, installment.StartDate, installment.EndDate, installment.AmountPerShare, installment.IsActive, installment.CreatedAt);
        return ApiResponse<ProjectInstallmentDto>.SuccessResult(dto, "تم تحديث القسط بنجاح");
    }
}
