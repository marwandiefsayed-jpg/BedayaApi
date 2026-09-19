namespace BedayaGroup.Application.Reports.DTOs;

public record AllocationItemDto(
    int ProjectInstallmentId,
    string InstallmentName,
    decimal AmountAllocated
);

public record ReceiptItemDto(
    int Id,
    DateTime Date,
    string ShareholderName,
    string? ShareholderPhone,
    string? ProjectName,
    decimal NumberOfShares,
    decimal AmountReceived,
    string? Description,
    List<AllocationItemDto> Allocations
)
{
    public decimal TotalAllocated => Allocations.Sum(a => a.AmountAllocated);
    public decimal UnallocatedAmount => Math.Max(0, AmountReceived - TotalAllocated);
}

public record ShareholderSummaryItemDto(
    int Id,
    string Name,
    string? Phone,
    decimal NumberOfShares,
    string? ProjectName,
    decimal TotalExpected,
    decimal TotalPaid,
    decimal TotalRemaining
);

public record ReceiptsDistributionReportDto(
    string? ProjectName,
    string? ShareName,
    DateTime? FromDate,
    DateTime? ToDate,
    DateTime GeneratedAt,
    List<ReceiptItemDto> Receipts,
    string? SelectedShareholderName = null,
    decimal TotalExpected = 0m,
    decimal TotalRemaining = 0m,
    decimal ExcessCredit = 0m,
    List<ShareholderSummaryItemDto>? ShareholderSummaries = null
)
{
    public bool IsSingleShareholderReport => !string.IsNullOrWhiteSpace(SelectedShareholderName);
    public decimal TotalReceived => Receipts.Sum(r => r.AmountReceived);
    public decimal TotalAllocated => Receipts.Sum(r => r.TotalAllocated);
    public decimal TotalUnallocated => Receipts.Sum(r => r.UnallocatedAmount);
    public int ReceiptCount => Receipts.Count;
}

public record ExportPdfResultDto(
    byte[] FileBytes,
    string FileName,
    string ContentType
);

public record StorageActivityPdfItemDto(DateTime Date, string TypeName, string? ProjectName, decimal Amount, string Description, string? ReferenceNumber);
public record StorageActivityPdfReportDto(string StorageName, string? ProjectName, DateTime GeneratedAt, List<StorageActivityPdfItemDto> Transactions);
