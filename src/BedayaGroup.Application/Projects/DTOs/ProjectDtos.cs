using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Projects.DTOs;

public record CreateProjectRequest(
    string Code,
    string Name,
    string? Location,
    string? Description,
    decimal Budget,
    DateTime StartDate,
    DateTime? ExpectedEndDate,
    int? ProjectOwnerId
);

public record UpdateProjectRequest(
    string Name,
    string? Location,
    string? Description,
    decimal Budget,
    DateTime StartDate,
    DateTime? ExpectedEndDate,
    DateTime? ActualEndDate,
    ProjectStatus Status,
    int? ProjectOwnerId,
    bool IsActive
);

public record ProjectDto(
    int Id,
    string Code,
    string Name,
    string? Location,
    string? Description,
    decimal Budget,
    DateTime StartDate,
    DateTime? ExpectedEndDate,
    DateTime? ActualEndDate,
    ProjectStatus Status,
    int? ProjectOwnerId,
    string? ProjectOwnerName,
    bool IsActive,
    DateTime CreatedAt,
    int FloorsCount
);

public record ProjectFinancialSummaryDto(
    int ProjectId,
    string ProjectCode,
    string ProjectName,
    decimal ProjectBudget,
    decimal TotalExpenses,
    decimal TotalPaidExpenses,
    decimal TotalOutstandingExpenses,
    decimal CashIn,
    decimal CashOut,
    decimal RemainingBudget
);
