namespace BedayaGroup.Application.Dashboard.DTOs;

public record DashboardSummaryDto(
    int TotalProjects,
    int ActiveProjects,
    decimal TotalExpenses,
    decimal TotalSupplierOutstanding,
    decimal TotalCashBalance,
    decimal TotalShareholderOutstanding,
    decimal TotalOutstandingAdvances,
    List<MonthlyCashFlowDto> MonthlyCashFlow
);

public record MonthlyCashFlowDto(
    string MonthName,
    int Year,
    int Month,
    decimal CashIn,
    decimal CashOut
);
