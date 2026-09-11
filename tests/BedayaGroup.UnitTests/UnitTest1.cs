using BedayaGroup.Application.Expenses.Commands;
using BedayaGroup.Application.Expenses.DTOs;
using FluentAssertions;
using Xunit;

namespace BedayaGroup.UnitTests;

public class ValidationTests
{
    [Fact]
    public void CreateExpenseValidator_ShouldHaveError_WhenTotalAmountIsZero()
    {
        // Arrange
        var validator = new CreateExpenseCommandValidator();
        var request = new CreateExpenseRequest(
            ExpenseNumber: "EXP-001",
            ProjectId: 1,
            SupplierId: 1,
            ExpenseDate: DateTime.UtcNow,
            Description: "اختبار المصروف",
            TotalAmount: 0m, // Invalid total amount
            Notes: null
        );

        // Act
        var result = validator.Validate(new CreateExpenseCommand(request));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("TotalAmount"));
    }
}
