using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Suppliers.DTOs;

public record CreateSupplierRequest(
    string Code,
    string Name,
    SupplierType Type,
    string? Phone,
    string? Address,
    decimal OpeningBalance,
    string? Notes
);

public record UpdateSupplierRequest(
    string Name,
    SupplierType Type,
    string? Phone,
    string? Address,
    decimal OpeningBalance,
    string? Notes,
    bool IsActive
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
    decimal CurrentBalance
);

public record SupplierStatementItemDto(
    DateTime Date,
    string Type,
    string DocumentNumber,
    string Description,
    decimal DebtAmount,   // قيمة المصروف (له)
    decimal CreditAmount, // سداد نقدي (عليه)
    decimal RunningBalance
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
    List<SupplierStatementItemDto> Items
);
