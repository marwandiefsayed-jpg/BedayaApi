using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Expenses.DTOs;

public record CreateExpenseRequest(
    string ExpenseNumber,
    int ProjectId,
    int? FloorId,
    int SupplierId,
    DateTime ExpenseDate,
    string Description,
    decimal TotalAmount,
    string? Notes
);

public record UpdateExpenseRequest(
    int? FloorId,
    int SupplierId,
    DateTime ExpenseDate,
    string Description,
    decimal TotalAmount,
    string? Notes
);

public record ExpenseDto(
    int Id,
    string ExpenseNumber,
    int ProjectId,
    string ProjectName,
    int? FloorId,
    string? FloorName,
    int SupplierId,
    string SupplierName,
    DateTime ExpenseDate,
    string Description,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    ExpenseStatus Status,
    int CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt,
    string? Notes
);
