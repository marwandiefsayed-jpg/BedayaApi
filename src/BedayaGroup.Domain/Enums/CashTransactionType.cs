namespace BedayaGroup.Domain.Enums;

public enum CashTransactionType
{
    ExpensePayment = 1,          // سداد مصروف
    CashIn = 2,                  // إيراد نقدي
    CashOut = 3,                 // مصروف نقدي
    AdvanceGiven = 4,            // صرف عهدة
    AdvanceReturned = 5,         // رد عهدة
    OwnerDeposit = 6,            // إيداع مالك الشركة
    OtherIncome = 7,             // إيراد آخر
    OtherExpense = 8,            // مصروف آخر
    ShareholderContribution = 9  // مساهمة مساهم
}
