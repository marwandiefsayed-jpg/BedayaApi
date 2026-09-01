using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BedayaGroup.Infrastructure.Identity;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? "SuperSecretKeyForBedayaGroupApi_MustBeAtLeast32BytesLong!";
        var issuer = jwtSettings["Issuer"] ?? "BedayaGroupApi";
        var audience = jwtSettings["Audience"] ?? "BedayaGroupApp";
        var expiryInMinutes = double.TryParse(jwtSettings["ExpiryMinutes"], out var minutes) ? minutes : 480;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim("RoleId", user.RoleId.ToString()),
            new Claim("RoleType", ((int)user.Role.RoleType).ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
