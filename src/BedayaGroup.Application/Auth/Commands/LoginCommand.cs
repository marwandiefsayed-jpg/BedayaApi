using BedayaGroup.Application.Auth.DTOs;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Auth.Commands;

public record LoginCommand(string Username, string Password) : IRequest<ApiResponse<AuthResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("اسم المستخدم مطلوب");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditService _auditService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IAuditService auditService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponse>.FailureResult("اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        if (!user.IsActive)
        {
            return ApiResponse<AuthResponse>.FailureResult("هذا الحساب معطل، يرجى التواصل مع إدارة النظام");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        await _auditService.LogAsync("Login", "User", user.Id.ToString(), null, new { user.Username, LoginTime = DateTime.UtcNow }, cancellationToken);

        var authResponse = new AuthResponse(
            user.Id,
            user.FullName,
            user.Username,
            user.Role,
            GetRoleName(user.Role),
            token,
            expiresAt
        );

        return ApiResponse<AuthResponse>.SuccessResult(authResponse, "تم تسجيل الدخول بنجاح");
    }

    private static string GetRoleName(BedayaGroup.Domain.Enums.UserRole role) => role switch
    {
        BedayaGroup.Domain.Enums.UserRole.CompanyOwner => "مالك الشركة",
        BedayaGroup.Domain.Enums.UserRole.ShareholdersOfficer => "مسؤول المساهمين",
        BedayaGroup.Domain.Enums.UserRole.ExpensesOfficer => "مسؤول المصروفات والموردين",
        _ => "مستخدم"
    };
}
