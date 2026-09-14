using System.Text.RegularExpressions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Suppliers.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BedayaGroup.Infrastructure.Services;

public class SupplierStatementPdfGenerator : ISupplierStatementPdfGenerator
{
    private static readonly string FontFamilyName = Fonts.Arial;
    private static readonly Lazy<byte[]> LogoBytes = new(() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "bedaya-logo.png")));

    public SupplierStatementPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private class PdfExpenseGroup
    {
        public int? ExpenseId { get; set; }
        public SupplierStatementItemDto MainItem { get; set; } = null!;
        public List<SupplierStatementItemDto> Payments { get; set; } = new();
        public decimal TotalPaid { get; set; }
        public decimal Remaining => Math.Max(0, MainItem.DebtAmount - TotalPaid);
    }

    public byte[] GenerateSupplierStatementPdf(
        SupplierStatementDto statement,
        DateTime? fromDate,
        DateTime? toDate,
        DateTime generatedAt)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(FontFamilyName).FontSize(9).FontColor("#1E293B"));
                page.ContentFromRightToLeft();

                // Header
                page.Header().Element(header => ComposeHeader(header, statement, fromDate, toDate, generatedAt));

                // Main Content
                page.Content().Element(content => ComposeContent(content, statement));

                // Footer
                page.Footer().Element(footer => ComposeFooter(footer));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(
        IContainer container,
        SupplierStatementDto statement,
        DateTime? fromDate,
        DateTime? toDate,
        DateTime generatedAt)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(76).Height(76).Image(LogoBytes.Value).FitArea();

                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text("كشف حساب مورد")
                        .FontSize(18)
                        .Bold()
                        .FontColor("#0F172A");

                    titleCol.Item().Text("شركة بداية للاستثمار والتطوير العقاري")
                        .FontSize(10)
                        .FontColor("#64748B");
                });

                row.ConstantItem(120).AlignLeft().Column(dateCol =>
                {
                    dateCol.Item().Text("تاريخ التقرير:")
                        .FontSize(8)
                        .FontColor("#64748B");
                    dateCol.Item().Text($"{generatedAt:yyyy/MM/dd HH:mm}")
                        .FontSize(9)
                        .Bold()
                        .FontColor("#1E293B");
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#E2E8F0");

            col.Item().Background("#ECFDF5").Border(1).BorderColor("#A7F3D0").Padding(10).Row(row =>
            {
                row.ConstantItem(84).Text("اسم المورد:")
                    .FontSize(9)
                    .Bold()
                    .FontColor("#047857");
                row.RelativeItem().Text(statement.SupplierName)
                    .FontSize(12)
                    .Bold()
                    .FontColor("#064E3B");
            });

            col.Item().Height(8);

            // Filter Badges Header
            col.Item().Background("#F8FAFC").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("المشروع: ").Bold().FontColor("#475569");
                        t.Span(string.IsNullOrWhiteSpace(statement.ProjectName) ? "جميع المشاريع" : statement.ProjectName).FontColor("#0F172A");
                    });
                });

                row.RelativeItem().Column(c =>
                {
                    var fromStr = fromDate.HasValue ? fromDate.Value.ToString("yyyy/MM/dd") : "الكل";
                    var toStr = toDate.HasValue ? toDate.Value.ToString("yyyy/MM/dd") : "الكل";

                    c.Item().Text(t =>
                    {
                        t.Span("الفترة: ").Bold().FontColor("#475569");
                        t.Span($"من {fromStr} إلى {toStr}").FontColor("#0F172A");
                    });
                });
            });

            col.Item().Height(10);
        });
    }

    private static void ComposeContent(IContainer container, SupplierStatementDto statement)
    {
        container.Column(col =>
        {
            // Metrics Summary Block at Top Only
            col.Item().Row(row =>
            {
                Metric(row.RelativeItem(), "الرصيد الافتتاحي", statement.OpeningBalance, "#475569");
                Metric(row.RelativeItem(), "إجمالي المشتريات / المستحق", statement.TotalInvoiced - statement.OpeningBalance, "#DC2626");
                Metric(row.RelativeItem(), "إجمالي المدفوعات", statement.TotalPaid, "#15803D");
                Metric(row.RelativeItem(), "الرصيد المستحق (المتبقي)", statement.CurrentBalance, statement.CurrentBalance > 0 ? "#B45309" : "#15803D");
            });

            col.Item().Height(14);

            if (statement.Items.Count == 0)
            {
                col.Item().Padding(40).AlignCenter().Text("لا توجد حركات في الفترة المحددة")
                    .FontSize(12)
                    .FontColor("#64748B");
                return;
            }

            // Group statement items by ExpenseId
            var expenseGroups = new List<PdfExpenseGroup>();
            var expenseMap = new Dictionary<int, PdfExpenseGroup>();
            var standalonePayments = new List<PdfExpenseGroup>();

            foreach (var item in statement.Items)
            {
                if (item.DebtAmount > 0 || item.Type.Contains("مصروف"))
                {
                    var group = new PdfExpenseGroup
                    {
                        ExpenseId = item.ExpenseId,
                        MainItem = item,
                    };
                    expenseGroups.Add(group);
                    if (item.ExpenseId.HasValue)
                    {
                        expenseMap[item.ExpenseId.Value] = group;
                    }
                }
            }

            foreach (var item in statement.Items)
            {
                if (item.CreditAmount > 0 || item.Type.Contains("سداد"))
                {
                    if (item.ExpenseId.HasValue && expenseMap.TryGetValue(item.ExpenseId.Value, out var parentGroup))
                    {
                        parentGroup.Payments.Add(item);
                        parentGroup.TotalPaid += item.CreditAmount;
                    }
                    else
                    {
                        standalonePayments.Add(new PdfExpenseGroup
                        {
                            ExpenseId = item.ExpenseId,
                            MainItem = item,
                            TotalPaid = item.CreditAmount,
                        });
                    }
                }
            }

            var allGroups = expenseGroups.Concat(standalonePayments).ToList();

            // Statement Items Table with Grouped Operations
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(26);   // م
                    columns.ConstantColumn(68);   // التاريخ
                    columns.RelativeColumn(3.2f); // المادة / البيان
                    columns.ConstantColumn(75);   // المشروع
                    columns.ConstantColumn(80);   // المستحق
                    columns.ConstantColumn(80);   // المدفوع حتى الآن
                    columns.ConstantColumn(85);   // الرصيد المتبقي
                });

                // Header Row
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("م");
                    header.Cell().Element(HeaderCellStyle).Text("التاريخ");
                    header.Cell().Element(HeaderCellStyle).Text("المادة / البيان");
                    header.Cell().Element(HeaderCellStyle).Text("المشروع");
                    header.Cell().Element(HeaderCellStyle).Text("المستحق");
                    header.Cell().Element(HeaderCellStyle).Text("المدفوع حتى الآن");
                    header.Cell().Element(HeaderCellStyle).Text("الرصيد المتبقي");
                });

                int index = 1;
                foreach (var group in allGroups)
                {
                    var item = group.MainItem;
                    var isExpenseRow = item.DebtAmount > 0 || item.Type.Contains("مصروف");
                    var bg = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                    var materialText = !string.IsNullOrWhiteSpace(item.MaterialName)
                        ? $"{item.MaterialName} ({item.Quantity:0.###} {item.Unit ?? ""} × {item.UnitPrice:N2} ج.م)"
                        : CleanDescription(item.Description);

                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(index.ToString());
                    table.Cell().Element(c => CellStyle(c, bg)).Text(item.Date.ToString("yyyy/MM/dd"));
                    table.Cell().Element(c => CellStyle(c, bg)).Column(descCol =>
                    {
                        descCol.Item().Text(materialText).Bold();
                        if (isExpenseRow && !string.IsNullOrWhiteSpace(item.MaterialName) && !string.IsNullOrWhiteSpace(item.Description) && item.Description != item.MaterialName && item.Description != "اي حاجة")
                        {
                            descCol.Item().Text(CleanDescription(item.Description)).FontSize(8).FontColor("#64748B");
                        }
                    });
                    table.Cell().Element(c => CellStyle(c, bg)).Text(item.ProjectName ?? "-");
                    table.Cell().Element(c => CellStyle(c, bg)).Text(item.DebtAmount > 0 ? $"{item.DebtAmount:N0} ج.م" : "-")
                        .Bold().FontColor(item.DebtAmount > 0 ? "#DC2626" : "#64748B");
                    table.Cell().Element(c => CellStyle(c, bg)).Text(group.TotalPaid > 0 ? $"{group.TotalPaid:N0} ج.م" : (item.CreditAmount > 0 ? $"{item.CreditAmount:N0} ج.م" : "-"))
                        .Bold().FontColor(group.TotalPaid > 0 || item.CreditAmount > 0 ? "#15803D" : "#64748B");
                    table.Cell().Element(c => CellStyle(c, bg)).Text(isExpenseRow ? $"{group.Remaining:N0} ج.م" : $"{item.RunningBalance:N0} ج.م")
                        .Bold().FontColor("#1E293B");

                    // Render Sub-Rows for Child Payments
                    foreach (var pay in group.Payments)
                    {
                        var childBg = "#F1F5F9";
                        var payDesc = CleanChildDescription(pay.Description);

                        table.Cell().Element(c => SubCellStyle(c, childBg)).AlignCenter().Text("");
                        table.Cell().Element(c => SubCellStyle(c, childBg)).Text(pay.Date.ToString("yyyy/MM/dd")).FontSize(8).FontColor("#64748B");
                        table.Cell().Element(c => SubCellStyle(c, childBg)).Text($"  ↳ {payDesc}").FontSize(8).Bold().FontColor("#047857");
                        table.Cell().Element(c => SubCellStyle(c, childBg)).Text(pay.ProjectName ?? "-").FontSize(8).FontColor("#64748B");
                        table.Cell().Element(c => SubCellStyle(c, childBg)).Text("-").FontSize(8).FontColor("#64748B");
                        table.Cell().Element(c => SubCellStyle(c, childBg)).Text($"{pay.CreditAmount:N0} ج.م").FontSize(8).Bold().FontColor("#15803D");
                        table.Cell().Element(c => SubCellStyle(c, childBg)).Text("-").FontSize(8).FontColor("#64748B");
                    }

                    index++;
                }
            });
        });
    }

    private static string CleanDescription(string desc)
    {
        if (string.IsNullOrWhiteSpace(desc)) return "سداد نقدي";
        var cleaned = Regex.Replace(desc, @"سداد لمصروف رقم:\s*EXP-[A-Z0-9-]+\s*-\s*", "");
        cleaned = Regex.Replace(cleaned, @"سداد لمصروف رقم:\s*EXP-[A-Z0-9-]+", "");
        cleaned = Regex.Replace(cleaned, @"سداد دفعة للمادة:\s*", "");
        cleaned = cleaned.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "سداد نقدي" : cleaned;
    }

    private static string CleanChildDescription(string desc)
    {
        if (string.IsNullOrWhiteSpace(desc)) return "دفعة مسددة";
        var cleaned = CleanDescription(desc);
        if (cleaned == "سداد نقدي" || cleaned == "سداد دفعة" || cleaned == "اي حاجة")
        {
            return "دفعة مسددة";
        }
        return $"دفعة مسددة ({cleaned})";
    }

    private static void Metric(IContainer container, string label, decimal value, string color) =>
        container.PaddingHorizontal(3).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Column(column =>
        {
            column.Item().Text(label).FontSize(8).FontColor("#64748B");
            column.Item().Text($"{value:N0} ج.م").FontSize(11).Bold().FontColor(color);
        });

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor("#E2E8F0");
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("نظام بدايات لإدارة المشاريع العقارية - كشف حساب المورد")
                    .FontSize(8)
                    .FontColor("#94A3B8");

                row.RelativeItem().AlignLeft().Text(x =>
                {
                    x.Span("صفحة ").FontSize(8).FontColor("#94A3B8");
                    x.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                    x.Span(" من ").FontSize(8).FontColor("#94A3B8");
                    x.TotalPages().FontSize(8).FontColor("#94A3B8");
                });
            });
        });
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Background("#1E293B")
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .AlignMiddle()
            .DefaultTextStyle(x => x.FontFamily(FontFamilyName).FontSize(8.5f).Bold().FontColor("#FFFFFF"));
    }

    private static IContainer CellStyle(IContainer container, string backgroundColor)
    {
        return container
            .Background(backgroundColor)
            .BorderBottom(0.5f)
            .BorderColor("#E2E8F0")
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .AlignMiddle();
    }

    private static IContainer SubCellStyle(IContainer container, string backgroundColor)
    {
        return container
            .Background(backgroundColor)
            .BorderBottom(0.5f)
            .BorderColor("#CBD5E1")
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .AlignMiddle();
    }
}
