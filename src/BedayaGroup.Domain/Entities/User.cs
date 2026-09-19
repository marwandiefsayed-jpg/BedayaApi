using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.CompanyOwner;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}
