namespace BedayaGroup.Application.Engineers.DTOs;

public record CreateEngineerRequest(
    string Code,
    string FullName,
    string? Phone,
    string? Email,
    string? Specialization,
    string? Notes
);

public record UpdateEngineerRequest(
    string FullName,
    string? Phone,
    string? Email,
    string? Specialization,
    string? Notes,
    bool IsActive
);

public record AssignEngineerToProjectRequest(
    int ProjectId,
    int EngineerId,
    string? Role,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes
);

public record EngineerDto(
    int Id,
    string Code,
    string FullName,
    string? Phone,
    string? Email,
    string? Specialization,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    int AssignedProjectsCount
);

public record ProjectEngineerDto(
    int Id,
    int ProjectId,
    string ProjectName,
    int EngineerId,
    string EngineerName,
    string EngineerCode,
    string? Role,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes
);
