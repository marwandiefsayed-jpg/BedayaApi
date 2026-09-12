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
        services.AddScoped<ISupplierStatementPdfGenerator, SupplierStatementPdfGenerator>();
        services.AddHttpContextAccessor();

        // JWT Authentication Setup
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? "SuperSecretKeyForBedayaGroupApi_MustBeAtLeast32BytesLong!";
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
                ClockSkew = TimeSpan.Zero
            };
        });

        // Authorization Policies (Roles removed - all authenticated users have access)
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CompanyOwnerOnly", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("CompanyOrProjectOwner", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("FinancialWriteAccess", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
