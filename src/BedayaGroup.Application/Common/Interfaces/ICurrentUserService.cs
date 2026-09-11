namespace BedayaGroup.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    string? IpAddress { get; }
}
