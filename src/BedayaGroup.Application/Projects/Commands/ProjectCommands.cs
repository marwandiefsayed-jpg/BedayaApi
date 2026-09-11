using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Projects.DTOs;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Projects.Commands;

public record CreateProjectCommand(CreateProjectRequest Request) : IRequest<ApiResponse<ProjectDto>>;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("اسم المشروع مطلوب");
    }
}

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ApiResponse<ProjectDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateProjectCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var project = new Project
        {
            Name = req.Name,
            StartDate = req.StartDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Project", project.Id.ToString(), null, new { project.Name, project.StartDate }, cancellationToken);

        var dto = new ProjectDto(project.Id, project.Name, project.StartDate, project.IsActive, project.CreatedAt);

        return ApiResponse<ProjectDto>.SuccessResult(dto, "تم إنشاء المشروع بنجاح");
    }
}

public record UpdateProjectCommand(int Id, UpdateProjectRequest Request) : IRequest<ApiResponse<ProjectDto>>;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ApiResponse<ProjectDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateProjectCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project == null)
        {
            throw new NotFoundException("المشروع غير موجود");
        }

        var req = request.Request;
        var oldValues = new { project.Name, project.StartDate, project.IsActive };

        project.Name = req.Name;
        project.StartDate = req.StartDate;
        project.IsActive = req.IsActive;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Project", project.Id.ToString(), oldValues, new { project.Name, project.StartDate, project.IsActive }, cancellationToken);

        var dto = new ProjectDto(project.Id, project.Name, project.StartDate, project.IsActive, project.CreatedAt);

        return ApiResponse<ProjectDto>.SuccessResult(dto, "تم تحديث بيانات المشروع بنجاح");
    }
}

public record DeleteProjectCommand(int Id) : IRequest<ApiResponse<bool>>;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DeleteProjectCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project == null)
        {
            throw new NotFoundException("المشروع غير موجود");
        }

        int projectId = request.Id;

        // ── 1. Clean up ShareholderPaymentAllocations first (FK to ProjectInstallments) ──
        var installments = await _context.ProjectInstallments
            .Where(i => i.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        var installmentIds = installments.Select(i => i.Id).ToList();
        if (installmentIds.Any())
        {
            var allocations = await _context.ShareholderPaymentAllocations
                .Where(a => installmentIds.Contains(a.ProjectInstallmentId))
                .ToListAsync(cancellationToken);
            _context.ShareholderPaymentAllocations.RemoveRange(allocations);
        }

        // ── 2. Delete ProjectInstallments ──
        _context.ProjectInstallments.RemoveRange(installments);

        // ── 3. Delete ProjectEngineers (NOT NULL FK) ──
        var engineers = await _context.ProjectEngineers
            .Where(pe => pe.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        _context.ProjectEngineers.RemoveRange(engineers);

        // ── 4. Delete Expenses (NOT NULL FK) ──
        var expenses = await _context.Expenses
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        _context.Expenses.RemoveRange(expenses);

        // ── 5. Delete Advances (NOT NULL FK) ──
        var advances = await _context.Advances
            .Where(a => a.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        _context.Advances.RemoveRange(advances);

        // ── 6. Nullify nullable FK references ──
        var shareholders = await _context.Shareholders
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var sh in shareholders) sh.ProjectId = null;

        var shareholderContribs = await _context.ShareholderContributions
            .Where(sc => sc.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var sc in shareholderContribs) sc.ProjectId = null;

        var cashTransactions = await _context.CashTransactions
            .Where(ct => ct.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var ct in cashTransactions) ct.ProjectId = null;

        var cashStorages = await _context.CashStorages
            .Where(cs => cs.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var cs in cashStorages) cs.ProjectId = null;

        var suppliers = await _context.Suppliers
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var s in suppliers) s.ProjectId = null;

        var storages = await _context.Storages
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var s in storages) s.ProjectId = null;

        var storageTransactions = await _context.StorageTransactions
            .Where(st => st.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var st in storageTransactions) st.ProjectId = null;

        // ── 7. Finally delete the project ──
        _context.Projects.Remove(project);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("Delete", "Project", project.Id.ToString(), new { project.Name }, null, cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "تم حذف المشروع بنجاح");
    }
}
