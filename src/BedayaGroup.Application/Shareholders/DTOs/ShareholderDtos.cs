namespace BedayaGroup.Application.Shareholders.DTOs;

// ============== Share (الأسهم) DTOs ==============

public record CreateShareRequest(
    string Name,
    DateTime StartDate
);

public record UpdateShareRequest(
    string Name,
    DateTime StartDate,
    bool IsActive
);

public record ShareDto(
    int Id,
    string Name,
    DateTime StartDate,
    bool IsActive,
    DateTime CreatedAt
);

// ============== ProjectInstallment DTOs ==============

public record CreateProjectInstallmentRequest(
    int ProjectId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal AmountPerShare
);

public record UpdateProjectInstallmentRequest(
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal AmountPerShare,
    bool IsActive
);

public record ProjectInstallmentDto(
    int Id,
    int ProjectId,
    string ProjectName,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal AmountPerShare,
    bool IsActive,
    DateTime CreatedAt
);

// ============== Shareholder DTOs ==============

public record CreateShareholderRequest(
    string Code,
    string Name,
    string? Phone,
    decimal NumberOfShares,
    int? ProjectId,
    string? Notes
);

public record UpdateShareholderRequest(
    string Name,
    string? Phone,
    decimal NumberOfShares,
    int? ProjectId,
    string? Notes,
    bool IsActive
);

public record RecordShareholderContributionRequest(
    int ShareholderId,
    int ProjectId,
    int ProjectInstallmentId,
    int? CashStorageId,
    decimal Amount,
    DateTime ContributionDate,
    string? Description,
    string? ReferenceNumber,
    string? Notes
);

public record UpdateShareholderContributionRequest(
    decimal Amount,
    DateTime ContributionDate,
    string? Description,
    string? ReferenceNumber,
    string? Notes
);

public record ShareholderDto(
    int Id,
    string Code,
    string Name,
    string? Phone,
    decimal NumberOfShares,
    int? ProjectId,
    string? ProjectName,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt
);

// ============== Installment Status DTOs ==============

public enum InstallmentPaymentStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    FullyPaid = 2
}

public record InstallmentSummaryDto(
    int InstallmentId,
    string InstallmentName,
    DateTime StartDate,
    DateTime EndDate,
    decimal AmountPerShare,
    decimal PenaltyAmount,    // غرامة تأخر مضافة من الإدارة
    decimal RequiredAmount,   // (NumberOfShares * AmountPerShare) + PenaltyAmount
    decimal PaidAmount,       // sum of allocations toward this installment
    decimal RemainingAmount,  // RequiredAmount - PaidAmount
    InstallmentPaymentStatus Status
);

// ============== Penalty DTOs (غرامة تأخر) ==============

public record AddShareholderPenaltyRequest(
    int ShareholderId,
    int ProjectInstallmentId,
    decimal PenaltyAmount,
    string? Notes
);

public record ShareholderInstallmentPenaltyDto(
    int Id,
    int ShareholderId,
    string ShareholderName,
    int ProjectInstallmentId,
    string InstallmentName,
    decimal PenaltyAmount,
    string? Notes,
    int CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt
);

public record ShareholderContributionDto(
    int Id,
    int ShareholderId,
    string ShareholderName,
    int ProjectId,
    string? ProjectName,
    int InstallmentId,
    string InstallmentName,
    int? TransactionId,
    string? TransactionNumber,
    decimal Amount,
    DateTime ContributionDate,
    string Description,
    int CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt
);

public record ShareholderStatementDto(
    int ShareholderId,
    string ShareholderCode,
    string ShareholderName,
    decimal NumberOfShares,
    int? ProjectId,
    string? ProjectName,
    decimal TotalExpected,
    decimal TotalPaid,
    decimal TotalRemaining,
    List<InstallmentSummaryDto> Installments,
    List<ShareholderContributionDto> Contributions
);
