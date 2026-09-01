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
        RuleFor(x => x.Request.Code).NotEmpty().WithMessage("كود المشروع مطلوب");
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("اسم المشروع مطلوب");
        RuleFor(x => x.Request.Budget).GreaterThan(0).WithMessage("ميزانية المشروع يجب أن تكون أكبر من صفر");
        RuleFor(x => x.Request.StartDate).NotEmpty().WithMessage("تاريخ بداية المشروع مطلوب");
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

        if (await _context.Projects.AnyAsync(p => p.Code == req.Code, cancellationToken))
        {
            return ApiResponse<ProjectDto>.FailureResult("كود المشروع مستخدم بالفعل");
        }

        if (req.ProjectOwnerId.HasValue && !await _context.Users.AnyAsync(u => u.Id == req.ProjectOwnerId.Value, cancellationToken))
        {
            return ApiResponse<ProjectDto>.FailureResult("مالك المشروع المحدد غير موجود");
        }

        var project = new Project
        {
            Code = req.Code,
            Name = req.Name,
            Location = req.Location,
            Description = req.Description,
            Budget = req.Budget,
            StartDate = req.StartDate,
            ExpectedEndDate = req.ExpectedEndDate,
            Status = ProjectStatus.Planning,
            ProjectOwnerId = req.ProjectOwnerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Project", project.Id.ToString(), null, new { project.Code, project.Name, project.Budget }, cancellationToken);

        string? ownerName = null;
        if (project.ProjectOwnerId.HasValue)
        {
            var owner = await _context.Users.FindAsync(new object[] { project.ProjectOwnerId.Value }, cancellationToken);
            ownerName = owner?.FullName;
        }

        var dto = new ProjectDto(project.Id, project.Code, project.Name, project.Location, project.Description, project.Budget, project.StartDate, project.ExpectedEndDate, project.ActualEndDate, project.Status, project.ProjectOwnerId, ownerName, project.IsActive, project.CreatedAt, 0);

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
            .Include(p => p.ProjectOwner)
            .Include(p => p.Floors)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project == null)
        {
            throw new NotFoundException("المشروع غير موجود");
        }

        var req = request.Request;
        var oldValues = new { project.Name, project.Budget, project.Status, project.ProjectOwnerId };

        project.Name = req.Name;
        project.Location = req.Location;
        project.Description = req.Description;
        project.Budget = req.Budget;
        project.StartDate = req.StartDate;
        project.ExpectedEndDate = req.ExpectedEndDate;
        project.ActualEndDate = req.ActualEndDate;
        project.Status = req.Status;
        project.ProjectOwnerId = req.ProjectOwnerId;
        project.IsActive = req.IsActive;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Project", project.Id.ToString(), oldValues, new { project.Name, project.Budget, project.Status }, cancellationToken);

        var dto = new ProjectDto(project.Id, project.Code, project.Name, project.Location, project.Description, project.Budget, project.StartDate, project.ExpectedEndDate, project.ActualEndDate, project.Status, project.ProjectOwnerId, project.ProjectOwner?.FullName, project.IsActive, project.CreatedAt, project.Floors.Count);

        return ApiResponse<ProjectDto>.SuccessResult(dto, "تم تحديث بيانات المشروع بنجاح");
    }
}
