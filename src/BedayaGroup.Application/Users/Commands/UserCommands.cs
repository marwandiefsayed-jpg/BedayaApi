using BedayaGroup.Application.Auth.DTOs;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Users.DTOs;
using BedayaGroup.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Users.Commands;

public record CreateUserCommand(CreateUserRequest Request) : IRequest<ApiResponse<UserDto>>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Request.FullName).NotEmpty().WithMessage("الاسم بالكامل مطلوب");
        RuleFor(x => x.Request.Username).NotEmpty().WithMessage("اسم المستخدم مطلوب");
        RuleFor(x => x.Request.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة").MinimumLength(6).WithMessage("كلمة المرور يجب أن لا تقل عن 6 أحرف");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public CreateUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IAuditService auditService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<ApiResponse<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _context.Users.AnyAsync(u => u.Username == req.Username, cancellationToken))
        {
            return ApiResponse<UserDto>.FailureResult("اسم المستخدم مستخدم بالفعل");
        }


        var user = new User
        {
            FullName = req.FullName,
            Username = req.Username,
            PasswordHash = _passwordHasher.HashPassword(req.Password),
            Phone = req.Phone,
            Role = req.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "User", user.Id.ToString(), null, new { user.Username, user.FullName, user.Role }, cancellationToken);

        var dto = new UserDto(user.Id, user.FullName, user.Username, user.Role, GetRoleName(user.Role), user.Phone, user.IsActive, user.CreatedAt, user.LastLoginAt);
        return ApiResponse<UserDto>.SuccessResult(dto, "تم إنشاء المستخدم بنجاح");
    }

    private static string GetRoleName(BedayaGroup.Domain.Enums.UserRole role) => role switch
    {
        BedayaGroup.Domain.Enums.UserRole.CompanyOwner => "مالك الشركة",
        BedayaGroup.Domain.Enums.UserRole.ShareholdersOfficer => "مسؤول المساهمين",
        BedayaGroup.Domain.Enums.UserRole.ExpensesOfficer => "مسؤول المصروفات والموردين",
        _ => "مستخدم"
    };
}

public record UpdateUserCommand(int Id, UpdateUserRequest Request) : IRequest<ApiResponse<UserDto>>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ApiResponse<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateUserCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("المستخدم غير موجود");
        }

        var oldValues = new { user.FullName, user.Phone, user.Role, user.IsActive };

        user.FullName = request.Request.FullName;
        user.Phone = request.Request.Phone;
        user.Role = request.Request.Role;
        user.IsActive = request.Request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "User", user.Id.ToString(), oldValues, new { user.FullName, user.Phone, user.Role, user.IsActive }, cancellationToken);

        var dto = new UserDto(user.Id, user.FullName, user.Username, user.Role, GetRoleName(user.Role), user.Phone, user.IsActive, user.CreatedAt, user.LastLoginAt);
        return ApiResponse<UserDto>.SuccessResult(dto, "تم تحديث بيانات المستخدم بنجاح");
    }

    private static string GetRoleName(BedayaGroup.Domain.Enums.UserRole role) => role switch
    {
        BedayaGroup.Domain.Enums.UserRole.CompanyOwner => "مالك الشركة",
        BedayaGroup.Domain.Enums.UserRole.ShareholdersOfficer => "مسؤول المساهمين",
        BedayaGroup.Domain.Enums.UserRole.ExpensesOfficer => "مسؤول المصروفات والموردين",
        _ => "مستخدم"
    };
}

public record DeleteUserCommand(int Id) : IRequest<ApiResponse<bool>>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public DeleteUserCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null) throw new NotFoundException("المستخدم غير موجود");
        if (_currentUserService.UserId == user.Id)
            return ApiResponse<bool>.FailureResult("لا يمكن حذف الحساب المستخدم حالياً");
        if (user.Role == BedayaGroup.Domain.Enums.UserRole.CompanyOwner)
            return ApiResponse<bool>.FailureResult("لا يمكن حذف حساب مالك الشركة");

        var oldValues = new { user.Username, user.FullName, user.Role };
        var replacementUserId = _currentUserService.UserId;
        if (!replacementUserId.HasValue)
            return ApiResponse<bool>.FailureResult("تعذر تحديد مالك الشركة المنفذ للحذف");

        // Preserve every financial/history record. Creator references are reassigned to the
        // deleting owner before the account is removed, avoiding foreign-key violations.
        var contributions = await _context.ShareholderContributions.Where(x => x.CreatedByUserId == user.Id).ToListAsync(cancellationToken);
        var expenses = await _context.Expenses.Where(x => x.CreatedByUserId == user.Id).ToListAsync(cancellationToken);
        var cashTransactions = await _context.CashTransactions.Where(x => x.CreatedByUserId == user.Id).ToListAsync(cancellationToken);
        var storageTransactions = await _context.StorageTransactions.Where(x => x.CreatedByUserId == user.Id).ToListAsync(cancellationToken);
        var penalties = await _context.ShareholderInstallmentPenalties.Where(x => x.CreatedByUserId == user.Id).ToListAsync(cancellationToken);
        foreach (var item in contributions) item.CreatedByUserId = replacementUserId.Value;
        foreach (var item in expenses) item.CreatedByUserId = replacementUserId.Value;
        foreach (var item in cashTransactions) item.CreatedByUserId = replacementUserId.Value;
        foreach (var item in storageTransactions) item.CreatedByUserId = replacementUserId.Value;
        foreach (var item in penalties) item.CreatedByUserId = replacementUserId.Value;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("Delete", "User", request.Id.ToString(), oldValues, new { ReassignedToUserId = replacementUserId.Value }, cancellationToken);
        return ApiResponse<bool>.SuccessResult(true, "تم حذف المستخدم بنجاح");
    }
}
