using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

/// <summary>
/// غرامة تأخر يعيّنها المدير على مساهم لدفعة معينة
/// Penalty fee assigned by an admin to a shareholder for a specific installment.
/// </summary>
public class ShareholderInstallmentPenalty : BaseEntity
{
    /// <summary>معرف المساهم</summary>
    public int ShareholderId { get; set; }

    /// <summary>معرف الدفعة</summary>
    public int ProjectInstallmentId { get; set; }

    /// <summary>مبلغ الغرامة</summary>
    public decimal PenaltyAmount { get; set; }

    /// <summary>سبب الغرامة / ملاحظات</summary>
    public string? Notes { get; set; }

    /// <summary>المستخدم الذي أضاف الغرامة</summary>
    public int CreatedByUserId { get; set; }

    // ── Navigation ──────────────────────────────────────────────────
    public Shareholder Shareholder { get; set; } = null!;
    public ProjectInstallment ProjectInstallment { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
