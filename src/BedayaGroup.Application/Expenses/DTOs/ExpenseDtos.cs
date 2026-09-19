using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Expenses.DTOs;

public record CreateExpenseRequest(
    string ExpenseNumber,
    int? ProjectId,
    int? StorageId,
    DateTime ExpenseDate,
    string Description,
    decimal TotalAmount,
    string? Notes,
    string MaterialName = "",
    string Unit = "",
    decimal Quantity = 0m,
    decimal UnitPrice = 0m,
    bool TargetAllProjects = false,
    List<int>? ProjectIds = null
);

public record UpdateExpenseRequest(
    int? StorageId,
    DateTime ExpenseDate,
    string Description,
    decimal TotalAmount,
    string? Notes,
    string MaterialName = "",
    string Unit = "",
    decimal Quantity = 0m,
    decimal UnitPrice = 0m
);

public record ExpenseDto(
    int Id,
    string ExpenseNumber,
    int ProjectId,
    string ProjectName,
    int? StorageId,
    string? StorageName,
    DateTime ExpenseDate,
    string Description,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    ExpenseStatus Status,
    int CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt,
    string? Notes,
    string? MaterialName = null,
    string? Unit = null,
    decimal Quantity = 0m,
    decimal UnitPrice = 0m
);

public record DailyExpenseGroupDto(
    DateTime Date,
    decimal TotalAmount,
    decimal TotalPaid,
    decimal TotalRemaining,
    int ExpenseCount,
    List<ExpenseDto> Expenses
);
