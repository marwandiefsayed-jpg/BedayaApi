using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Suppliers.DTOs;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Suppliers.Commands;

public record CreateSupplierCommand(CreateSupplierRequest Request) : IRequest<ApiResponse<SupplierDto>>;

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().WithMessage("كود المورد مطلوب");
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("اسم المورد مطلوب");
    }
}

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, ApiResponse<SupplierDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateSupplierCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<SupplierDto>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var supplierCode = string.IsNullOrWhiteSpace(req.Code)
            ? $"SUP-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
            : req.Code.Trim();

        if (await _context.Suppliers.AnyAsync(s => s.Code == supplierCode, cancellationToken))
        {
            return ApiResponse<SupplierDto>.FailureResult("كود المورد مستخدم بالفعل");
        }

        if (!req.ProjectId.HasValue)
        {
            return ApiResponse<SupplierDto>.FailureResult("يجب ربط المورد بمشروع");
        }

        // Validate project relationship
        string? projectName = null;
        if (req.ProjectId.HasValue)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, cancellationToken);
            if (project == null) return ApiResponse<SupplierDto>.FailureResult("المشروع المحدد غير موجود");
            projectName = project.Name;
        }

        var supplier = new Supplier
        {
            Code = supplierCode,
            Name = req.Name.Trim(),
            Type = req.Type,
            Phone = req.Phone,
            Address = req.Address,
            OpeningBalance = 0m,
            Notes = req.Notes,
            ProjectId = req.ProjectId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Supplier", supplier.Id.ToString(), null, new { supplier.Code, supplier.Name, supplier.Type, supplier.ProjectId }, cancellationToken);

        var dto = new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.Type, supplier.Phone, supplier.Address, supplier.OpeningBalance, supplier.Notes, supplier.IsActive, supplier.CreatedAt, 0, 0, supplier.OpeningBalance, supplier.ProjectId, projectName);
        return ApiResponse<SupplierDto>.SuccessResult(dto, "تم إضافة المورد بنجاح");
    }
}

public record UpdateSupplierCommand(int Id, UpdateSupplierRequest Request) : IRequest<ApiResponse<SupplierDto>>;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, ApiResponse<SupplierDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateSupplierCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<SupplierDto>> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Project)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (supplier == null)
        {
            throw new NotFoundException("المورد غير موجود");
        }

        var req = request.Request;

        if (!req.ProjectId.HasValue)
        {
            return ApiResponse<SupplierDto>.FailureResult("يجب ربط المورد بمشروع");
        }
        var oldValues = new { supplier.Name, supplier.Type, supplier.OpeningBalance, supplier.IsActive, supplier.ProjectId };

        // Validate new ProjectId if provided
        string? projectName = supplier.Project?.Name;
        if (req.ProjectId != supplier.ProjectId)
        {
            if (req.ProjectId.HasValue)
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, cancellationToken);
                if (project == null) return ApiResponse<SupplierDto>.FailureResult("المشروع المحدد غير موجود");
                projectName = project.Name;
            }
            else
            {
                projectName = null;
            }
        }

        supplier.Name = req.Name;
        supplier.Type = req.Type;
        supplier.Phone = req.Phone;
        supplier.Address = req.Address;
        supplier.OpeningBalance = req.OpeningBalance;
        supplier.Notes = req.Notes;
        supplier.IsActive = req.IsActive;
        supplier.ProjectId = req.ProjectId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Supplier", supplier.Id.ToString(), oldValues, new { supplier.Name, supplier.Type, supplier.OpeningBalance, supplier.IsActive, supplier.ProjectId }, cancellationToken);

        var dto = new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.Type, supplier.Phone, supplier.Address, supplier.OpeningBalance, supplier.Notes, supplier.IsActive, supplier.CreatedAt, 0, 0, supplier.OpeningBalance, supplier.ProjectId, projectName);
        return ApiResponse<SupplierDto>.SuccessResult(dto, "تم تحديث بيانات المورد بنجاح");
    }
}
