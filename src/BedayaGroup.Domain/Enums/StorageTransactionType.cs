namespace BedayaGroup.Domain.Enums;

public enum StorageTransactionType
{
    Purchase = 1,       // شراء
    TransferIn = 2,     // تحويل وارد
    TransferOut = 3,    // تحويل صادر
    IssueToProject = 4, // صرف للمشروع
    Return = 5,         // مرتجع
    Adjustment = 6      // تسوية
}
