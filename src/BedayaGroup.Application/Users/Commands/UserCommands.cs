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
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "User", user.Id.ToString(), null, new { user.Username, user.FullName }, cancellationToken);

        var dto = new UserDto(user.Id, user.FullName, user.Username, user.Phone, user.IsActive, user.CreatedAt, user.LastLoginAt);
        return ApiResponse<UserDto>.SuccessResult(dto, "تم إنشاء المستخدم بنجاح");
    }
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

        var oldValues = new { user.FullName, user.Phone, user.IsActive };

        user.FullName = request.Request.FullName;
        user.Phone = request.Request.Phone;
        user.IsActive = request.Request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "User", user.Id.ToString(), oldValues, new { user.FullName, user.Phone, user.IsActive }, cancellationToken);

        var dto = new UserDto(user.Id, user.FullName, user.Username, user.Phone, user.IsActive, user.CreatedAt, user.LastLoginAt);
        return ApiResponse<UserDto>.SuccessResult(dto, "تم تحديث بيانات المستخدم بنجاح");
    }
}
