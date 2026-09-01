using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Floors.DTOs;
using BedayaGroup.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Floors.Commands;

public record CreateFloorCommand(CreateFloorRequest Request) : IRequest<ApiResponse<FloorDto>>;

public class CreateFloorCommandValidator : AbstractValidator<CreateFloorCommand>
{
    public CreateFloorCommandValidator()
    {
        RuleFor(x => x.Request.ProjectId).GreaterThan(0).WithMessage("معرف المشروع غير صحيح");
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("اسم الدور مطلوب");
    }
}

public class CreateFloorCommandHandler : IRequestHandler<CreateFloorCommand, ApiResponse<FloorDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateFloorCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<FloorDto>> Handle(CreateFloorCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (!await _context.Projects.AnyAsync(p => p.Id == req.ProjectId, cancellationToken))
        {
            return ApiResponse<FloorDto>.FailureResult("المشروع غير موجود");
        }

        var floor = new Floor
        {
            ProjectId = req.ProjectId,
            FloorNumber = req.FloorNumber,
            Name = req.Name,
            Description = req.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Floors.Add(floor);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Floor", floor.Id.ToString(), null, new { floor.ProjectId, floor.FloorNumber, floor.Name }, cancellationToken);

        var dto = new FloorDto(floor.Id, floor.ProjectId, floor.FloorNumber, floor.Name, floor.Description, floor.CreatedAt);
        return ApiResponse<FloorDto>.SuccessResult(dto, "تم إضافة الدور بنجاح");
    }
}

public record UpdateFloorCommand(int Id, UpdateFloorRequest Request) : IRequest<ApiResponse<FloorDto>>;

public class UpdateFloorCommandHandler : IRequestHandler<UpdateFloorCommand, ApiResponse<FloorDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateFloorCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<FloorDto>> Handle(UpdateFloorCommand request, CancellationToken cancellationToken)
    {
        var floor = await _context.Floors.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (floor == null)
        {
            throw new NotFoundException("الدور غير موجود");
        }

        var req = request.Request;
        var oldValues = new { floor.FloorNumber, floor.Name };

        floor.FloorNumber = req.FloorNumber;
        floor.Name = req.Name;
        floor.Description = req.Description;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Floor", floor.Id.ToString(), oldValues, new { floor.FloorNumber, floor.Name }, cancellationToken);

        var dto = new FloorDto(floor.Id, floor.ProjectId, floor.FloorNumber, floor.Name, floor.Description, floor.CreatedAt);
        return ApiResponse<FloorDto>.SuccessResult(dto, "تم تحديث الدور بنجاح");
    }
}
