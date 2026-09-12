using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Suppliers.DTOs;

public class CreateSupplierRequest
{
    public string Code { get; set; } = $"SUP-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    public string Name { get; set; } = string.Empty;
    public SupplierType Type { get; set; } = SupplierType.Supplier;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? Notes { get; set; }
    public int? ProjectId { get; set; }
}

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
    string? ProjectName,
    string? MaterialName = null,
    string? Unit = null,
    decimal Quantity = 0m,
    decimal UnitPrice = 0m
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
