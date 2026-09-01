using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Advances.DTOs;

public record CreateAdvanceRequest(
    string AdvanceNumber,
    int EngineerId,
    int ProjectId,
    int CashStorageId,
    decimal IssuedAmount,
    DateTime IssueDate,
    string? Notes
);

public record SettleAdvanceRequest(
    int AdvanceId,
    int CashStorageId,
    decimal SettledAmount,
    DateTime SettlementDate,
    string Description,
    string? ReferenceNumber,
    string? Notes
);

public record AdvanceDto(
    int Id,
    string AdvanceNumber,
    int EngineerId,
    string EngineerName,
    string EngineerCode,
    int ProjectId,
    string ProjectName,
    decimal IssuedAmount,
    decimal SettledAmount,
    decimal RemainingAmount,
    DateTime IssueDate,
    DateTime? SettlementDate,
    AdvanceStatus Status,
    int CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt,
    string? Notes
);

public record AdvanceStatementDto(
    int AdvanceId,
    string AdvanceNumber,
    string EngineerName,
    string ProjectName,
    decimal IssuedAmount,
    decimal TotalSettled,
    decimal RemainingAmount,
    AdvanceStatus Status,
    List<AdvanceSettlementItemDto> Settlements
);

public record AdvanceSettlementItemDto(
    DateTime Date,
    string TransactionNumber,
    string Description,
    decimal Amount,
    string CreatedByUserName
);
