using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Engineers.DTOs;
using BedayaGroup.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Engineers.Commands;

public record CreateEngineerCommand(CreateEngineerRequest Request) : IRequest<ApiResponse<EngineerDto>>;

public class CreateEngineerCommandValidator : AbstractValidator<CreateEngineerCommand>
{
    public CreateEngineerCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().WithMessage("كود المهندس مطلوب");
        RuleFor(x => x.Request.FullName).NotEmpty().WithMessage("اسم المهندس مطلوب");
    }
}

public class CreateEngineerCommandHandler : IRequestHandler<CreateEngineerCommand, ApiResponse<EngineerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateEngineerCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<EngineerDto>> Handle(CreateEngineerCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _context.Engineers.AnyAsync(e => e.Code == req.Code, cancellationToken))
        {
            return ApiResponse<EngineerDto>.FailureResult("كود المهندس مستخدم بالفعل");
        }

        var engineer = new Engineer
        {
            Code = req.Code,
            FullName = req.FullName,
            Phone = req.Phone,
            Email = req.Email,
            Specialization = req.Specialization,
            Notes = req.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Engineers.Add(engineer);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Engineer", engineer.Id.ToString(), null, new { engineer.Code, engineer.FullName }, cancellationToken);

        var dto = new EngineerDto(engineer.Id, engineer.Code, engineer.FullName, engineer.Phone, engineer.Email, engineer.Specialization, engineer.Notes, engineer.IsActive, engineer.CreatedAt, 0);
        return ApiResponse<EngineerDto>.SuccessResult(dto, "تم إضافة المهندس بنجاح");
    }
}

public record UpdateEngineerCommand(int Id, UpdateEngineerRequest Request) : IRequest<ApiResponse<EngineerDto>>;

public class UpdateEngineerCommandHandler : IRequestHandler<UpdateEngineerCommand, ApiResponse<EngineerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateEngineerCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<EngineerDto>> Handle(UpdateEngineerCommand request, CancellationToken cancellationToken)
    {
        var engineer = await _context.Engineers
            .Include(e => e.ProjectEngineers)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (engineer == null)
        {
            throw new NotFoundException("المهندس غير موجود");
        }

        var req = request.Request;
        var oldValues = new { engineer.FullName, engineer.Phone, engineer.IsActive };

        engineer.FullName = req.FullName;
        engineer.Phone = req.Phone;
        engineer.Email = req.Email;
        engineer.Specialization = req.Specialization;
        engineer.Notes = req.Notes;
        engineer.IsActive = req.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Engineer", engineer.Id.ToString(), oldValues, new { engineer.FullName, engineer.IsActive }, cancellationToken);

        var dto = new EngineerDto(engineer.Id, engineer.Code, engineer.FullName, engineer.Phone, engineer.Email, engineer.Specialization, engineer.Notes, engineer.IsActive, engineer.CreatedAt, engineer.ProjectEngineers.Count);
        return ApiResponse<EngineerDto>.SuccessResult(dto, "تم تحديث بيانات المهندس بنجاح");
    }
}

public record AssignEngineerToProjectCommand(AssignEngineerToProjectRequest Request) : IRequest<ApiResponse<ProjectEngineerDto>>;

public class AssignEngineerToProjectCommandHandler : IRequestHandler<AssignEngineerToProjectCommand, ApiResponse<ProjectEngineerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AssignEngineerToProjectCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ProjectEngineerDto>> Handle(AssignEngineerToProjectCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, cancellationToken);
        if (project == null) return ApiResponse<ProjectEngineerDto>.FailureResult("المشروع المحدد غير موجود");

        var engineer = await _context.Engineers.FirstOrDefaultAsync(e => e.Id == req.EngineerId, cancellationToken);
        if (engineer == null) return ApiResponse<ProjectEngineerDto>.FailureResult("المهندس المحدد غير موجود");

        if (await _context.ProjectEngineers.AnyAsync(pe => pe.ProjectId == req.ProjectId && pe.EngineerId == req.EngineerId && pe.EndDate == null, cancellationToken))
        {
            return ApiResponse<ProjectEngineerDto>.FailureResult("المهندس معين بالفعل على هذا المشروع حالياً");
        }

        var pe = new ProjectEngineer
        {
            ProjectId = req.ProjectId,
            EngineerId = req.EngineerId,
            Role = req.Role,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Notes = req.Notes
        };

        _context.ProjectEngineers.Add(pe);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("AssignEngineer", "ProjectEngineer", pe.Id.ToString(), null, new { pe.ProjectId, pe.EngineerId, pe.Role }, cancellationToken);

        var dto = new ProjectEngineerDto(pe.Id, project.Id, project.Name, engineer.Id, engineer.FullName, engineer.Code, pe.Role, pe.StartDate, pe.EndDate, pe.Notes);
        return ApiResponse<ProjectEngineerDto>.SuccessResult(dto, "تم تعيين المهندس على المشروع بنجاح");
    }
}
