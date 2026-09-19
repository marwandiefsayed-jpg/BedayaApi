using BedayaGroup.Application.Cash;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Projects.DTOs;
using BedayaGroup.Application.Storages;
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

        var storage = await ProjectCashStorage.GetOrCreateAsync(_context, project, cancellationToken);
        await ProjectMaterialStorage.GetOrCreateAsync(_context, project, cancellationToken);

        await _auditService.LogAsync("Create", "Project", project.Id.ToString(), null, new { project.Name, project.StartDate }, cancellationToken);

        var dto = new ProjectDto(project.Id, project.Name, project.StartDate, project.IsActive, project.CreatedAt, storage.Id, ProjectCashStorage.ComputeBalance(storage));

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

        var storage = await ProjectCashStorage.GetOrCreateAsync(_context, project, cancellationToken);
        var expectedName = ProjectCashStorage.BuildName(project.Name);
        if (storage.Name != expectedName)
        {
            storage.Name = expectedName;
            storage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var materialStorage = await ProjectMaterialStorage.FindAsync(_context, project.Id, cancellationToken);
        if (materialStorage != null)
        {
            var expectedMaterialName = ProjectMaterialStorage.BuildName(project.Name);
            if (materialStorage.Name != expectedMaterialName)
            {
                materialStorage.Name = expectedMaterialName;
                materialStorage.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        await _auditService.LogAsync("Update", "Project", project.Id.ToString(), oldValues, new { project.Name, project.StartDate, project.IsActive }, cancellationToken);

        var dto = new ProjectDto(project.Id, project.Name, project.StartDate, project.IsActive, project.CreatedAt, storage.Id, ProjectCashStorage.ComputeBalance(storage));

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

            // Penalties also have a restricted FK to installments.
            var penalties = await _context.ShareholderInstallmentPenalties
                .Where(p => installmentIds.Contains(p.ProjectInstallmentId))
                .ToListAsync(cancellationToken);
            _context.ShareholderInstallmentPenalties.RemoveRange(penalties);
        }

        // ── 2. Delete ProjectInstallments ──
        _context.ProjectInstallments.RemoveRange(installments);

        // ── 3. Delete Expenses (NOT NULL FK) ──
        var expenses = await _context.Expenses
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        _context.Expenses.RemoveRange(expenses);

        // ── 4. Nullify nullable FK references ──
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

        // ── Project dedicated cash storage: remove it together with the project ──
        var projectStorages = await _context.CashStorages
            .Where(cs => cs.Type == CashStorageType.Project && cs.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        var projectStorageIds = projectStorages.Select(cs => cs.Id).ToList();
        var expenseIds = expenses.Select(e => e.Id).ToList();

        var projectStorageTransactions = await _context.CashTransactions
            .Where(ct => projectStorageIds.Contains(ct.CashStorageId))
            .ToListAsync(cancellationToken);

        // Clear the nullable contribution reference explicitly before deleting its transaction.
        // This keeps EF's tracked graph consistent with the database's SET NULL rule.
        var projectStorageTransactionIds = projectStorageTransactions.Select(ct => ct.Id).ToList();
        if (projectStorageTransactionIds.Any())
        {
            var linkedContributions = await _context.ShareholderContributions
                .Where(sc => sc.TransactionId.HasValue && projectStorageTransactionIds.Contains(sc.TransactionId.Value))
                .ToListAsync(cancellationToken);
            foreach (var contribution in linkedContributions) contribution.TransactionId = null;
        }
        _context.CashTransactions.RemoveRange(projectStorageTransactions);

        // Detach any remaining expense link so the expenses can be deleted (FK is restricted).
        var expenseLinkedTransactions = await _context.CashTransactions
            .Where(ct => ct.ExpenseId != null && expenseIds.Contains(ct.ExpenseId.Value))
            .ToListAsync(cancellationToken);
        foreach (var ct in expenseLinkedTransactions)
        {
            ct.ExpenseId = null;
            ct.ProjectId = null;
        }

        _context.CashStorages.RemoveRange(projectStorages);

        var cashStorages = await _context.CashStorages
            .Where(cs => cs.ProjectId == projectId && cs.Type != CashStorageType.Project)
            .ToListAsync(cancellationToken);
        foreach (var cs in cashStorages) cs.ProjectId = null;

        // ── Project dedicated material warehouse: remove it with its own movements ──
        var projectMaterialStorages = await _context.Storages
            .Where(s => s.Type == StorageType.Project && s.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        var projectMaterialStorageIds = projectMaterialStorages.Select(s => s.Id).ToList();
        var projectMaterialTransactions = await _context.StorageTransactions
            .Where(st => projectMaterialStorageIds.Contains(st.StorageId))
            .ToListAsync(cancellationToken);
        _context.StorageTransactions.RemoveRange(projectMaterialTransactions);
        _context.Storages.RemoveRange(projectMaterialStorages);

        // Detach any remaining (company) storages that were linked to the project.
        var storages = await _context.Storages
            .Where(s => s.ProjectId == projectId && s.Type != StorageType.Project)
            .ToListAsync(cancellationToken);
        foreach (var s in storages) s.ProjectId = null;

        var storageTransactions = await _context.StorageTransactions
            .Where(st => st.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var st in storageTransactions) st.ProjectId = null;

        // ── 5. Finally delete the project ──
        _context.Projects.Remove(project);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("Delete", "Project", project.Id.ToString(), new { project.Name }, null, cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "تم حذف المشروع بنجاح");
    }
}
