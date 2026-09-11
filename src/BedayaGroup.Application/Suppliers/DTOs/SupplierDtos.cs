using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Suppliers.DTOs;

public record CreateSupplierRequest(
    string Code,
    string Name,
    SupplierType Type,
    string? Phone,
    string? Address,
    decimal OpeningBalance,
    string? Notes,
    int? ProjectId
);

public record UpdateSupplierRequest(
    string Name,
    SupplierType Type,
    string? Phone,
    string? Address,
    decimal OpeningBalance,
    string? Notes,
    bool IsActive,
    int? ProjectId
);

public record SupplierDto(
    int Id,
    string Code,
    string Name,
    SupplierType Type,
    string? Phone,
    string? Address,
    decimal OpeningBalance,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    decimal TotalExpenses,
    decimal TotalPaid,
    decimal CurrentBalance,
    int? ProjectId,
    string? ProjectName
);

public record SupplierStatementItemDto(
    DateTime Date,
    string Type,
    string DocumentNumber,
    string Description,
    decimal DebtAmount,   // قيمة المصروف (له)
    decimal CreditAmount, // سداد نقدي (عليه)
    decimal RunningBalance,
    int? ProjectId,
    string? ProjectName
);

public record SupplierStatementDto(
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    SupplierType SupplierType,
    decimal OpeningBalance,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal CurrentBalance,
    int? ProjectId,
    string? ProjectName,
    List<SupplierStatementItemDto> Items
);
