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

        if (await _context.Suppliers.AnyAsync(s => s.Code == req.Code, cancellationToken))
        {
            return ApiResponse<SupplierDto>.FailureResult("كود المورد مستخدم بالفعل");
        }

        var supplier = new Supplier
        {
            Code = req.Code,
            Name = req.Name,
            Type = req.Type,
            Phone = req.Phone,
            Address = req.Address,
            OpeningBalance = req.OpeningBalance,
            Notes = req.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Supplier", supplier.Id.ToString(), null, new { supplier.Code, supplier.Name, supplier.Type }, cancellationToken);

        var dto = new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.Type, supplier.Phone, supplier.Address, supplier.OpeningBalance, supplier.Notes, supplier.IsActive, supplier.CreatedAt, 0, 0, supplier.OpeningBalance);
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
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (supplier == null)
        {
            throw new NotFoundException("المورد غير موجود");
        }

        var req = request.Request;
        var oldValues = new { supplier.Name, supplier.Type, supplier.OpeningBalance, supplier.IsActive };

        supplier.Name = req.Name;
        supplier.Type = req.Type;
        supplier.Phone = req.Phone;
        supplier.Address = req.Address;
        supplier.OpeningBalance = req.OpeningBalance;
        supplier.Notes = req.Notes;
        supplier.IsActive = req.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Supplier", supplier.Id.ToString(), oldValues, new { supplier.Name, supplier.Type, supplier.OpeningBalance, supplier.IsActive }, cancellationToken);

        var dto = new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.Type, supplier.Phone, supplier.Address, supplier.OpeningBalance, supplier.Notes, supplier.IsActive, supplier.CreatedAt, 0, 0, supplier.OpeningBalance);
        return ApiResponse<SupplierDto>.SuccessResult(dto, "تم تحديث بيانات المورد بنجاح");
    }
}
