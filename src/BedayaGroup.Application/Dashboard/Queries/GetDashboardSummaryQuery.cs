using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Dashboard.DTOs;
using BedayaGroup.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Dashboard.Queries;

public record GetDashboardSummaryQuery : IRequest<ApiResponse<DashboardSummaryDto>>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, ApiResponse<DashboardSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var totalProjects = await _context.Projects.CountAsync(cancellationToken);
        var activeProjects = await _context.Projects.CountAsync(p => p.IsActive, cancellationToken);

        var totalExpenses = await _context.Expenses.SumAsync(e => (decimal?)e.TotalAmount, cancellationToken) ?? 0m;

        var totalPaidExpenses = await _context.CashTransactions
            .Where(ct => ct.ExpenseId != null && ct.Type == CashTransactionType.ExpensePayment)
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        // Total supplier balances (OpeningBalance + InvoicedExpenses - Payments)
        var suppliersOpening = await _context.Suppliers.SumAsync(s => (decimal?)s.OpeningBalance, cancellationToken) ?? 0m;
        var totalSupplierOutstanding = suppliersOpening + totalExpenses - totalPaidExpenses;

        // Total Cash Balances across all cash storages
        var cashStoragesOpening = await _context.CashStorages.SumAsync(cs => (decimal?)cs.OpeningBalance, cancellationToken) ?? 0m;
        var totalCashIn = await _context.CashTransactions
            .Where(ct => ct.Type == CashTransactionType.CashIn || ct.Type == CashTransactionType.AdvanceReturned || ct.Type == CashTransactionType.OwnerDeposit || ct.Type == CashTransactionType.OtherIncome || ct.Type == CashTransactionType.ShareholderContribution)
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        var totalCashOut = await _context.CashTransactions
            .Where(ct => ct.Type == CashTransactionType.CashOut || ct.Type == CashTransactionType.ExpensePayment || ct.Type == CashTransactionType.AdvanceGiven || ct.Type == CashTransactionType.OtherExpense)
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        var totalCashBalance = cashStoragesOpening + totalCashIn - totalCashOut;

        // Shareholder Outstanding - expected = sum of (installment.AmountPerShare * shareholder.NumberOfShares) for all installments
        var reqShareholderContributions = await _context.ProjectInstallments
            .Join(_context.Shareholders,
                i => i.ProjectId,
                s => s.ProjectId,
                (i, s) => i.AmountPerShare * s.NumberOfShares)
            .SumAsync(cancellationToken);
        var actualShareholderContributions = await _context.ShareholderContributions.SumAsync(sc => (decimal?)sc.Amount, cancellationToken) ?? 0m;
        var totalShareholderOutstanding = reqShareholderContributions - actualShareholderContributions;

        // Outstanding Advances
        var totalIssuedAdvances = await _context.Advances.SumAsync(a => (decimal?)a.IssuedAmount, cancellationToken) ?? 0m;
        var totalReturnedAdvances = await _context.CashTransactions
            .Where(ct => ct.AdvanceId != null && ct.Type == CashTransactionType.AdvanceReturned)
            .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

        var totalOutstandingAdvances = totalIssuedAdvances - totalReturnedAdvances;

        // Monthly cashflow data for last 6 months
        var monthlyCashFlow = new List<MonthlyCashFlowDto>();
        var now = DateTime.UtcNow;

        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var year = targetMonth.Year;
            var month = targetMonth.Month;

            var monthlyIn = await _context.CashTransactions
                .Where(ct => ct.TransactionDate.Year == year && ct.TransactionDate.Month == month &&
                    (ct.Type == CashTransactionType.CashIn || ct.Type == CashTransactionType.AdvanceReturned || ct.Type == CashTransactionType.OwnerDeposit || ct.Type == CashTransactionType.OtherIncome || ct.Type == CashTransactionType.ShareholderContribution))
                .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

            var monthlyOut = await _context.CashTransactions
                .Where(ct => ct.TransactionDate.Year == year && ct.TransactionDate.Month == month &&
                    (ct.Type == CashTransactionType.CashOut || ct.Type == CashTransactionType.ExpensePayment || ct.Type == CashTransactionType.AdvanceGiven || ct.Type == CashTransactionType.OtherExpense))
                .SumAsync(ct => (decimal?)ct.Amount, cancellationToken) ?? 0m;

            monthlyCashFlow.Add(new MonthlyCashFlowDto(
                targetMonth.ToString("MMMM yyyy"),
                year,
                month,
                monthlyIn,
                monthlyOut
            ));
        }

        var summary = new DashboardSummaryDto(
            totalProjects,
            activeProjects,
            totalExpenses,
            totalSupplierOutstanding,
            totalCashBalance,
            totalShareholderOutstanding,
            totalOutstandingAdvances,
            monthlyCashFlow
        );

        return ApiResponse<DashboardSummaryDto>.SuccessResult(summary);
    }
}
