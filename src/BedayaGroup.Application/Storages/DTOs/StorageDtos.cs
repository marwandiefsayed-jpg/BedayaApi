using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Storages.DTOs;

public record CreateStorageRequest(
    string Name,
    StorageType Type,
    int? ProjectId,
    string? Location,
    string? Description
);

public record RecordStorageTransactionRequest(
    int StorageId,
    int? ProjectId,
    DateTime TransactionDate,
    string MaterialName,
    string Unit,
    decimal Quantity,
    StorageTransactionType Type,
    string? ReferenceNumber,
    string? Description
);

public record StorageDto(
    int Id,
    string Name,
    StorageType Type,
    int? ProjectId,
    string? ProjectName,
    string? Location,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);

public record StorageTransactionDto(
    int Id,
    int StorageId,
    string StorageName,
    int? ProjectId,
    string? ProjectName,
    DateTime TransactionDate,
    string MaterialName,
    string Unit,
    decimal Quantity,
    StorageTransactionType Type,
    string TypeArabicName,
    string? ReferenceNumber,
    string? Description,
    int CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt
);

public record StorageMaterialBalanceDto(
    string MaterialName,
    string Unit,
    decimal TotalIn,
    decimal TotalOut,
    decimal CurrentQuantity
);
