using BedayaGroup.Application.Reports.DTOs;

namespace BedayaGroup.Application.Common.Interfaces;

public interface IReceiptsPdfGenerator
{
    byte[] GenerateReceiptsDistributionPdf(ReceiptsDistributionReportDto data);
    byte[] GenerateProjectExpensesPdf(ProjectExpensesPdfReportDto data);
    byte[] GenerateStorageActivityPdf(StorageActivityPdfReportDto data);
}

public record ProjectExpensePdfItemDto(
    int Id,
    string ExpenseNumber,
    string Description,
    string MaterialName,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    DateTime ExpenseDate
);

public record ProjectExpensesPdfReportDto(
    string? ProjectName,
    string? MaterialFilterName,
    DateTime GeneratedAt,
    List<ProjectExpensePdfItemDto> Expenses,
    decimal TotalAmount
);
