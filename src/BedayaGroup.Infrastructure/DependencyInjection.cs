using System.Text;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Infrastructure.Identity;
using BedayaGroup.Infrastructure.Persistence;
using BedayaGroup.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BedayaGroup.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=BedayaGroupDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReceiptsPdfGenerator, ReceiptsPdfGenerator>();
        services.AddHttpContextAccessor();


        // JWT Authentication Setup
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Required configuration value 'JwtSettings:Secret' is missing.");
        }

        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException("Configuration value 'JwtSettings:Secret' must be at least 32 bytes for HS256.");
        }

        var issuer = jwtSettings["Issuer"] ?? "BedayaGroupApi";
        var audience = jwtSettings["Audience"] ?? "BedayaGroupApp";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
        });

        // Role-Based Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CompanyOwnerOnly", policy => policy.RequireRole(
                Domain.Enums.UserRole.CompanyOwner.ToString(),
                ((int)Domain.Enums.UserRole.CompanyOwner).ToString(),
                "1"
            ));
            options.AddPolicy("ShareholdersAccess", policy => policy.RequireRole(
                Domain.Enums.UserRole.CompanyOwner.ToString(),
                Domain.Enums.UserRole.ShareholdersOfficer.ToString(),
                ((int)Domain.Enums.UserRole.CompanyOwner).ToString(),
                ((int)Domain.Enums.UserRole.ShareholdersOfficer).ToString(),
                "1", "2"
            ));
            options.AddPolicy("ExpensesAccess", policy => policy.RequireRole(
                Domain.Enums.UserRole.CompanyOwner.ToString(),
                Domain.Enums.UserRole.ExpensesOfficer.ToString(),
                ((int)Domain.Enums.UserRole.CompanyOwner).ToString(),
                ((int)Domain.Enums.UserRole.ExpensesOfficer).ToString(),
                "1", "3"
            ));
            options.AddPolicy("FinancialWriteAccess", policy => policy.RequireRole(
                Domain.Enums.UserRole.CompanyOwner.ToString(),
                Domain.Enums.UserRole.ShareholdersOfficer.ToString(),
                Domain.Enums.UserRole.ExpensesOfficer.ToString(),
                ((int)Domain.Enums.UserRole.CompanyOwner).ToString(),
                ((int)Domain.Enums.UserRole.ShareholdersOfficer).ToString(),
                ((int)Domain.Enums.UserRole.ExpensesOfficer).ToString(),
                "1", "2", "3"
            ));
            options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
