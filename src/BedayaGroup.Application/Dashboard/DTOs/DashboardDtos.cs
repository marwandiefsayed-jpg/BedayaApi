namespace BedayaGroup.Application.Dashboard.DTOs;

public record DashboardSummaryDto(
    int TotalProjects,
    int ActiveProjects,
    decimal TotalExpenses,
    decimal TotalCashBalance,
    decimal TotalShareholderOutstanding,
    List<MonthlyCashFlowDto> MonthlyCashFlow
);

public record MonthlyCashFlowDto(
    string MonthName,
    int Year,
    int Month,
    decimal CashIn,
    decimal CashOut
);
