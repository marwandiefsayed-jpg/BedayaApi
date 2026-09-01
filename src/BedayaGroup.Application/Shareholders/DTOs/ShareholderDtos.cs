namespace BedayaGroup.Application.Shareholders.DTOs;

public record CreateShareholderRequest(
    string Code,
    string Name,
    string? Phone,
    decimal OwnershipPercentage,
    decimal RequiredContribution,
    string? Notes
);

public record UpdateShareholderRequest(
    string Name,
    string? Phone,
    decimal OwnershipPercentage,
    decimal RequiredContribution,
    string? Notes,
    bool IsActive
);

public record RecordShareholderContributionRequest(
    int ShareholderId,
    int? ProjectId,
    int CashStorageId,
    decimal Amount,
    DateTime ContributionDate,
    string Description,
    string? ReferenceNumber,
    string? Notes
);

public record ShareholderDto(
    int Id,
    string Code,
    string Name,
    string? Phone,
    decimal OwnershipPercentage,
    decimal RequiredContribution,
    decimal ContributedAmount,
    decimal RemainingAmount,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt
);

public record ShareholderContributionDto(
    int Id,
    int ShareholderId,
    string ShareholderName,
    int? ProjectId,
    string? ProjectName,
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
    decimal OwnershipPercentage,
    decimal RequiredContribution,
    decimal TotalContributed,
    decimal RemainingContribution,
    List<ShareholderContributionDto> Contributions
);
