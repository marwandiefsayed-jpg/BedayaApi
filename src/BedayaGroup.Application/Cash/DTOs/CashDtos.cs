using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Cash.DTOs;

public record CreateCashStorageRequest(
    string Name,
    CashStorageType Type,
    decimal OpeningBalance,
    string? Location,
    int? ProjectId
);

public record RecordCashTransactionRequest(
    string TransactionNumber,
    DateTime TransactionDate,
    CashTransactionType Type,
    decimal Amount,
    int CashStorageId,
    int? ProjectId,
    int? ExpenseId,
    int? AdvanceId,
    string Description,
    string? ReferenceNumber,
    string? Notes
);

public record RecordExpensePaymentRequest(
    int ExpenseId,
    int CashStorageId,
    decimal Amount,
    DateTime PaymentDate,
    string? ReferenceNumber,
    string? Notes
);

public record CashStorageDto(
    int Id,
    string Name,
    CashStorageType Type,
    decimal OpeningBalance,
    string? Location,
    int? ProjectId,
    string? ProjectName,
    bool IsActive,
    DateTime CreatedAt,
    decimal CurrentBalance
);

public record CashTransactionDto(
    int Id,
    string TransactionNumber,
    DateTime TransactionDate,
    CashTransactionType Type,
    string TypeArabicName,
    decimal Amount,
    int CashStorageId,
    string CashStorageName,
    int? ProjectId,
    string? ProjectName,
    int? ExpenseId,
    string? ExpenseNumber,
    int? AdvanceId,
    string? AdvanceNumber,
    string Description,
    string? ReferenceNumber,
    int CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt,
    string? Notes
);
