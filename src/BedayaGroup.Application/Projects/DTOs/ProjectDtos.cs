using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Projects.DTOs;

public record CreateProjectRequest(
    string Name,
    DateTime? StartDate
);

public record UpdateProjectRequest(
    string Name,
    DateTime? StartDate,
    bool IsActive
);

public record ProjectDto(
    int Id,
    string Name,
    DateTime? StartDate,
    bool IsActive,
    DateTime CreatedAt,
    int? CashStorageId = null,
    decimal CashBalance = 0m
);

public record ProjectFinancialSummaryDto(
    int ProjectId,
    string ProjectName,
    decimal TotalExpenses,
    decimal TotalPaidExpenses,
    decimal TotalOutstandingExpenses,
    decimal CashIn,
    decimal CashOut
);
