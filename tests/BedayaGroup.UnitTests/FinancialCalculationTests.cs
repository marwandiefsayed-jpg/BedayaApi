using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BedayaGroup.UnitTests;

public class FinancialCalculationTests
{
    [Fact]
    public void ExpenseRemaining_ShouldCalculateCorrectly_WhenPartialPaymentRecorded()
    {
        // Arrange
        var expense = new Expense
        {
            TotalAmount = 100000m,
            ExpenseNumber = "EXP-100",
            Description = "مواد بناء"
        };

        var cashPayments = new List<CashTransaction>
        {
            new CashTransaction { Amount = 60000m, Type = CashTransactionType.ExpensePayment }
        };

        // Act
        var totalPaid = cashPayments.Where(ct => ct.Type == CashTransactionType.ExpensePayment).Sum(ct => ct.Amount);
        var remaining = expense.TotalAmount - totalPaid;

        // Assert
        totalPaid.Should().Be(60000m);
        remaining.Should().Be(40000m);
    }

    [Fact]
    public void ShareholderRemaining_ShouldCalculateCorrectly()
    {
        // Arrange - new model: required = NumberOfShares * installment.AmountPerShare
        var shareholder = new Shareholder
        {
            NumberOfShares = 2.5m,
            ShareId = 1,
            Code = "SH-01",
            Name = "أحمد علي"
        };

        decimal amountPerShare = 200000m; // e.g., installment amount per share
        decimal requiredContribution = shareholder.NumberOfShares * amountPerShare; // = 500000

        var contributions = new List<ShareholderContribution>
        {
            new ShareholderContribution { Amount = 300000m }
        };

        // Act
        var totalContributed = contributions.Sum(c => c.Amount);
        var remaining = requiredContribution - totalContributed;

        // Assert
        totalContributed.Should().Be(300000m);
        remaining.Should().Be(200000m);
    }

    [Fact]
    public void AdvanceRemaining_ShouldCalculateCorrectly()
    {
        // Arrange
        var advance = new Advance
        {
            IssuedAmount = 50000m,
            AdvanceNumber = "ADV-01"
        };

        var settlements = new List<CashTransaction>
        {
            new CashTransaction { Amount = 15000m, Type = CashTransactionType.AdvanceReturned }
        };

        // Act
        var totalSettled = settlements.Where(ct => ct.Type == CashTransactionType.AdvanceReturned).Sum(ct => ct.Amount);
        var remaining = advance.IssuedAmount - totalSettled;

        // Assert
        totalSettled.Should().Be(15000m);
        remaining.Should().Be(35000m);
    }
}
