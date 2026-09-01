using BedayaGroup.Domain.Entities;

namespace BedayaGroup.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
