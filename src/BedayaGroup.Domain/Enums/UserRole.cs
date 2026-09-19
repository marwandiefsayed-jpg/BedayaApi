namespace BedayaGroup.Domain.Enums;

public enum UserRole
{
    CompanyOwner = 1,          // مالك الشركة (Access to everything)
    ShareholdersOfficer = 2,   // مسؤول المساهمين (Shareholders only)
    ExpensesOfficer = 3        // مسؤول المصروفات والموردين (Expenses & Suppliers only)
}
