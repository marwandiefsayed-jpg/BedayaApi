namespace BedayaGroup.Domain.Enums;

public enum CashTransactionType
{
    ExpensePayment = 1,          // سداد مصروف (Outflow)
    CashIn = 2,                  // إيراد نقدي / إيداع يدوي (Inflow)
    CashOut = 3,                 // مصروف نقدي / سحب يدوي (Outflow)
    OwnerDeposit = 6,            // إيداع مالك الشركة (Inflow)
    OtherIncome = 7,             // إيراد آخر (Inflow)
    OtherExpense = 8,            // مصروف آخر (Outflow)
    ShareholderContribution = 9  // مساهمة مساهم (Inflow)
}
