using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Reports.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BedayaGroup.Infrastructure.Services;

public class ReceiptsPdfGenerator : IReceiptsPdfGenerator
{
    private static readonly string FontFamilyName = Fonts.Arial;
    private static readonly Lazy<byte[]> LogoBytes = new(() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "bedaya-logo.png")));

    public ReceiptsPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateReceiptsDistributionPdf(ReceiptsDistributionReportDto data)
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
                page.Header().Element(header => ComposeHeader(header, data));

                // Main Content
                page.Content().Element(content => ComposeContent(content, data));

                // Footer
                page.Footer().Element(footer => ComposeFooter(footer));
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateStorageActivityPdf(StorageActivityPdfReportDto data)
    {
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(24); page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontFamily(FontFamilyName).FontSize(9).FontColor("#1E293B")); page.ContentFromRightToLeft();
            page.Header().Column(header =>
            {
                header.Item().Row(row =>
                {
                    row.ConstantItem(76).Height(76).Image(LogoBytes.Value).FitArea();
                    row.RelativeItem().Column(c => { c.Item().Text($"تقرير حركة {data.StorageName}").FontSize(18).Bold().FontColor("#0F172A"); c.Item().Text("شركة بداية للاستثمار والتطوير العقاري").FontSize(10).FontColor("#64748B"); });
                    row.ConstantItem(120).AlignLeft().Column(c => { c.Item().Text("تاريخ التقرير:").FontSize(8).FontColor("#64748B"); c.Item().Text($"{data.GeneratedAt:yyyy/MM/dd HH:mm}").Bold(); });
                });
                header.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#E2E8F0");
                header.Item().Background("#F8FAFC").Padding(10).Text($"الخزينة: {data.StorageName}{(string.IsNullOrWhiteSpace(data.ProjectName) ? string.Empty : $"   |   المشروع: {data.ProjectName}")}").Bold().FontColor("#0F172A");
                header.Item().Height(10);
            });
            page.Content().Column(col =>
            {
                var inflow = data.Transactions.Where(t => t.TypeName is "CashIn" or "OwnerDeposit" or "OtherIncome" or "ShareholderContribution").Sum(t => t.Amount);
                var outflow = data.Transactions.Where(t => t.TypeName is "CashOut" or "ExpensePayment" or "OtherExpense").Sum(t => t.Amount);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c => { c.Item().Text("عدد العمليات").FontSize(8).FontColor("#64748B"); c.Item().Text($"{data.Transactions.Count} عملية").FontSize(13).Bold(); });
                    row.RelativeItem().Padding(4).Background("#ECFDF5").Border(1).BorderColor("#A7F3D0").Padding(10).Column(c => { c.Item().Text("إجمالي الوارد").FontSize(8).FontColor("#047857"); c.Item().Text($"{inflow:N2} ج.م").FontSize(13).Bold().FontColor("#059669"); });
                    row.RelativeItem().Padding(4).Background("#FEF2F2").Border(1).BorderColor("#FECACA").Padding(10).Column(c => { c.Item().Text("إجمالي المنصرف").FontSize(8).FontColor("#991B1B"); c.Item().Text($"{outflow:N2} ج.م").FontSize(13).Bold().FontColor("#DC2626"); });
                });
                col.Item().Height(16);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.ConstantColumn(32); c.ConstantColumn(90); c.RelativeColumn(3.5f); c.ConstantColumn(110); });
                    table.Header(h => { foreach (var title in new[] { "م", "التاريخ", "البيان", "المبلغ" }) h.Cell().Element(HeaderCellStyle).Text(title); });
                    var index = 1;
                    foreach (var item in data.Transactions) { var bg = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF"; table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(index.ToString()); table.Cell().Element(c => CellStyle(c, bg)).Text(item.Date.ToString("yyyy/MM/dd")); table.Cell().Element(c => CellStyle(c, bg)).Text(item.Description); table.Cell().Element(c => CellStyle(c, bg)).Text($"{item.Amount:N2} ج.م").Bold(); index++; }
                });
            });
            page.Footer().Element(ComposeFooter);
        })).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ReceiptsDistributionReportDto data)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(76).Height(76).Image(LogoBytes.Value).FitArea();

                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text("سجل المقبوضات وتوزيع المبالغ")
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
                    dateCol.Item().Text($"{data.GeneratedAt:yyyy/MM/dd HH:mm}")
                        .FontSize(9)
                        .Bold()
                        .FontColor("#1E293B");
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#E2E8F0");

            if (data.IsSingleShareholderReport)
            {
                col.Item().Background("#ECFDF5").Border(1).BorderColor("#A7F3D0").Padding(10).Row(row =>
                {
                    row.ConstantItem(84).Text("المساهم")
                        .FontSize(9)
                        .Bold()
                        .FontColor("#047857");
                    row.RelativeItem().Text(data.SelectedShareholderName!)
                        .FontSize(12)
                        .Bold()
                        .FontColor("#064E3B");
                });

                col.Item().Height(8);
            }

            // Filter Badges Header
            col.Item().Background("#F8FAFC").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("المشروع: ").Bold().FontColor("#475569");
                        t.Span(string.IsNullOrWhiteSpace(data.ProjectName) ? "جميع المشاريع" : data.ProjectName).FontColor("#0F172A");
                    });
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("السهم: ").Bold().FontColor("#475569");
                        t.Span(string.IsNullOrWhiteSpace(data.ShareName) ? "جميع الأسهم" : data.ShareName).FontColor("#0F172A");
                    });
                });

                row.RelativeItem().Column(c =>
                {
                    var fromStr = data.FromDate.HasValue ? data.FromDate.Value.ToString("yyyy/MM/dd") : "الكل";
                    var toStr = data.ToDate.HasValue ? data.ToDate.Value.ToString("yyyy/MM/dd") : "الكل";

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

    private static void ComposeContent(IContainer container, ReceiptsDistributionReportDto data)
    {
        container.Column(col =>
        {
            if (data.IsSingleShareholderReport)
            {
                col.Item().Element(singleShareholderContent => ComposeSingleShareholderContent(singleShareholderContent, data));
                return;
            }

            // Metrics Summary Block at Top
            col.Item().Row(row =>
            {
                // Card 1: Total Expected
                row.RelativeItem().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                {
                    c.Item().Text("إجمالي المستحق").FontSize(8).FontColor("#64748B");
                    c.Item().Text($"{data.TotalExpected:N0} ج.م").FontSize(13).Bold().FontColor("#0F172A");
                });

                // Card 2: Total Received
                row.RelativeItem().Padding(4).Background("#ECFDF5").Border(1).BorderColor("#A7F3D0").Padding(10).Column(c =>
                {
                    c.Item().Text("إجمالي المقبوضات").FontSize(8).FontColor("#047857");
                    c.Item().Text($"{data.TotalReceived:N0} ج.م").FontSize(13).Bold().FontColor("#059669");
                });

                // Card 3: Total Remaining
                var remBg = data.TotalRemaining > 0 ? "#FEF2F2" : "#ECFDF5";
                var remBorder = data.TotalRemaining > 0 ? "#FECACA" : "#A7F3D0";
                var remText = data.TotalRemaining > 0 ? "#DC2626" : "#059669";
                var remLabel = data.TotalRemaining > 0 ? "#991B1B" : "#047857";

                row.RelativeItem().Padding(4).Background(remBg).Border(1).BorderColor(remBorder).Padding(10).Column(c =>
                {
                    c.Item().Text("المتبقي للتحصيل").FontSize(8).FontColor(remLabel);
                    c.Item().Text($"{data.TotalRemaining:N0} ج.م").FontSize(13).Bold().FontColor(remText);
                });
            });

            col.Item().Height(14);

            // Check if we have shareholder summaries list
            if (data.ShareholderSummaries != null && data.ShareholderSummaries.Count > 0)
            {
                var showProjectColumn = string.IsNullOrWhiteSpace(data.ProjectName);

                col.Item().Text("ملخص رصيد المساهمين").FontSize(11).Bold().FontColor("#0F172A");
                col.Item().Height(6);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(24);   // م
                        columns.RelativeColumn(2);    // اسم المساهم
                        columns.ConstantColumn(85);   // الهاتف
                        columns.ConstantColumn(45);   // الأسهم
                        if (showProjectColumn)
                        {
                            columns.RelativeColumn(1.8f); // المشروع
                        }
                        columns.ConstantColumn(90);   // إجمالي المستحق
                        columns.ConstantColumn(90);   // إجمالي المقبوضات
                        columns.ConstantColumn(95);   // المتبقي للتحصيل
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("م");
                        header.Cell().Element(HeaderCellStyle).Text("اسم المساهم");
                        header.Cell().Element(HeaderCellStyle).Text("الهاتف");
                        header.Cell().Element(HeaderCellStyle).Text("الأسهم");
                        if (showProjectColumn)
                        {
                            header.Cell().Element(HeaderCellStyle).Text("المشروع");
                        }
                        header.Cell().Element(HeaderCellStyle).Text("إجمالي المستحق");
                        header.Cell().Element(HeaderCellStyle).Text("إجمالي المقبوضات");
                        header.Cell().Element(HeaderCellStyle).Text("المتبقي للتحصيل");
                    });

                    int idx = 1;
                    foreach (var s in data.ShareholderSummaries)
                    {
                        var bg = idx % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                        table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(idx.ToString());
                        table.Cell().Element(c => CellStyle(c, bg)).Text(s.Name).Bold();
                        table.Cell().Element(c => CellStyle(c, bg)).Text(s.Phone ?? "-");
                        table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(s.NumberOfShares.ToString("0.##"));
                        if (showProjectColumn)
                        {
                            table.Cell().Element(c => CellStyle(c, bg)).Text(s.ProjectName ?? "-");
                        }
                        table.Cell().Element(c => CellStyle(c, bg)).Text($"{s.TotalExpected:N0} ج.م").FontColor("#0F172A");
                        table.Cell().Element(c => CellStyle(c, bg)).Text($"{s.TotalPaid:N0} ج.م").Bold().FontColor("#059669");
                        table.Cell().Element(c => CellStyle(c, bg)).Text($"{s.TotalRemaining:N0} ج.م").Bold().FontColor(s.TotalRemaining > 0 ? "#DC2626" : "#059669");

                        idx++;
                    }
                });
            }
            else
            {
                // Receipts Table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(24);  // م
                        columns.ConstantColumn(75);  // التاريخ
                        columns.RelativeColumn(2);   // المساهم
                        columns.ConstantColumn(90);  // الهاتف
                        columns.RelativeColumn(1.8f);// المشروع
                        columns.ConstantColumn(55);  // الأسهم
                        columns.ConstantColumn(95);  // المقبوض
                    });

                    // Header Row
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("م");
                        header.Cell().Element(HeaderCellStyle).Text("التاريخ");
                        header.Cell().Element(HeaderCellStyle).Text("اسم المساهم");
                        header.Cell().Element(HeaderCellStyle).Text("رقم الهاتف");
                        header.Cell().Element(HeaderCellStyle).Text("المشروع");
                        header.Cell().Element(HeaderCellStyle).Text("الأسهم");
                        header.Cell().Element(HeaderCellStyle).Text("المبلغ المقبوض");
                    });

                    int index = 1;
                    foreach (var r in data.Receipts)
                    {
                        var isEven = index % 2 == 0;
                        var bg = isEven ? "#F8FAFC" : "#FFFFFF";

                        table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(index.ToString());
                        table.Cell().Element(c => CellStyle(c, bg)).Text(r.Date.ToString("yyyy/MM/dd"));
                        table.Cell().Element(c => CellStyle(c, bg)).Text(r.ShareholderName).Bold();
                        table.Cell().Element(c => CellStyle(c, bg)).Text(r.ShareholderPhone ?? "-");
                        table.Cell().Element(c => CellStyle(c, bg)).Text(r.ProjectName ?? "-");
                        table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(r.NumberOfShares.ToString("0.##"));
                        table.Cell().Element(c => CellStyle(c, bg)).Text($"{r.AmountReceived:N0} ج.م").Bold().FontColor("#0D9488");

                        index++;
                    }
                });
            }

            col.Item().Height(16);

            // Report Summary Block
            col.Item().Background("#F1F5F9").Padding(12).Column(summaryCol =>
            {
                summaryCol.Item().Text("ملخص التقرير المالي").FontSize(11).Bold().FontColor("#0F172A");
                summaryCol.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor("#CBD5E1");

                summaryCol.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("إجمالي المستحق").FontSize(8).FontColor("#475569");
                        c.Item().Text($"{data.TotalExpected:N0} جنيه مصري").FontSize(12).Bold().FontColor("#0F172A");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("إجمالي المقبوضات").FontSize(8).FontColor("#475569");
                        c.Item().Text($"{data.TotalReceived:N0} جنيه مصري").FontSize(12).Bold().FontColor("#0D9488");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("المتبقي للتحصيل").FontSize(8).FontColor("#475569");
                        c.Item().Text($"{data.TotalRemaining:N0} جنيه مصري").FontSize(12).Bold().FontColor(data.TotalRemaining > 0 ? "#DC2626" : "#0D9488");
                    });

                    var totalShares = (data.ShareholderSummaries ?? new List<ShareholderSummaryItemDto>()).Sum(s => s.NumberOfShares);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("إجمالي الأسهم").FontSize(8).FontColor("#475569");
                        c.Item().Text($"{totalShares:0.##} سهم").FontSize(12).Bold().FontColor("#0F172A");
                    });
                });
            });
        });
    }

    private static void ComposeSingleShareholderContent(IContainer container, ReceiptsDistributionReportDto data)
    {
        container.Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(85);
                    columns.RelativeColumn(2.5f);
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(110);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("م");
                    header.Cell().Element(HeaderCellStyle).Text("التاريخ");
                    header.Cell().Element(HeaderCellStyle).Text("المشروع");
                    header.Cell().Element(HeaderCellStyle).Text("الأسهم");
                    header.Cell().Element(HeaderCellStyle).Text("المبلغ المستلم");
                });

                var index = 1;
                foreach (var receipt in data.Receipts)
                {
                    var background = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                    table.Cell().Element(c => CellStyle(c, background)).AlignCenter().Text(index.ToString());
                    table.Cell().Element(c => CellStyle(c, background)).Text(receipt.Date.ToString("yyyy/MM/dd"));
                    table.Cell().Element(c => CellStyle(c, background)).Text(receipt.ProjectName ?? "-");
                    table.Cell().Element(c => CellStyle(c, background)).AlignCenter().Text(receipt.NumberOfShares.ToString("0.##"));
                    table.Cell().Element(c => CellStyle(c, background)).Text($"{receipt.AmountReceived:N0} ج.م").Bold().FontColor("#0D9488");

                    index++;
                }
            });

            col.Item().Height(16);

            // Financial Summary Row matching website layout
            col.Item().Row(row =>
            {
                // Card 1: Total Expected
                row.RelativeItem().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                {
                    c.Item().Text("إجمالي المستحق").FontSize(8).FontColor("#64748B");
                    c.Item().Text($"{data.TotalExpected:N2} جنيه").FontSize(13).Bold().FontColor("#0F172A");
                });

                // Card 2: Total Received
                row.RelativeItem().Padding(4).Background("#ECFDF5").Border(1).BorderColor("#A7F3D0").Padding(10).Column(c =>
                {
                    c.Item().Text("إجمالي المقبوضات").FontSize(8).FontColor("#047857");
                    c.Item().Text($"{data.TotalReceived:N2} جنيه").FontSize(13).Bold().FontColor("#059669");
                });

                // Card 3: Total Remaining
                var remBg = data.TotalRemaining > 0 ? "#FEF2F2" : "#ECFDF5";
                var remBorder = data.TotalRemaining > 0 ? "#FECACA" : "#A7F3D0";
                var remText = data.TotalRemaining > 0 ? "#DC2626" : "#059669";
                var remLabel = data.TotalRemaining > 0 ? "#991B1B" : "#047857";

                row.RelativeItem().Padding(4).Background(remBg).Border(1).BorderColor(remBorder).Padding(10).Column(c =>
                {
                    c.Item().Text("المتبقي للتحصيل").FontSize(8).FontColor(remLabel);
                    c.Item().Text($"{data.TotalRemaining:N2} جنيه").FontSize(13).Bold().FontColor(remText);
                });

                // Optional Card 4: Excess Credit
                if (data.ExcessCredit > 0)
                {
                    row.RelativeItem().Padding(4).Background("#F0FDF4").Border(1).BorderColor("#86EFAC").Padding(10).Column(c =>
                    {
                        c.Item().Text("الرصيد الدائن (فائض)").FontSize(8).FontColor("#166534");
                        c.Item().Text($"{data.ExcessCredit:N2} جنيه").FontSize(13).Bold().FontColor("#16A34A");
                    });
                }
            });
        });
    }

    public byte[] GenerateProjectExpensesPdf(ProjectExpensesPdfReportDto data)
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
                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(76).Height(76).Image(LogoBytes.Value).FitArea();

                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("تقرير مصاريف المشروع")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor("#0F172A");

                                var projText = !string.IsNullOrWhiteSpace(data.ProjectName) ? $"مشروع: {data.ProjectName}" : "مشروع: كافة المشاريع";
                                if (!string.IsNullOrWhiteSpace(data.MaterialFilterName))
                                {
                                    projText += $" (تصفية المادة: {data.MaterialFilterName})";
                                }

                                titleCol.Item().Text(projText)
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor("#0284C7");

                                titleCol.Item().Text("شركة بداية للاستثمار والتطوير العقاري")
                                    .FontSize(9)
                                    .FontColor("#64748B");
                            });

                            row.ConstantItem(120).AlignLeft().Column(dateCol =>
                            {
                                dateCol.Item().Text("تاريخ التقرير:")
                                    .FontSize(8)
                                    .FontColor("#64748B");
                                dateCol.Item().Text($"{data.GeneratedAt:yyyy/MM/dd HH:mm}")
                                    .FontSize(9)
                                    .Bold()
                                    .FontColor("#1E293B");
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#E2E8F0");
                    });
                });

                // Content
                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        if (data.Expenses.Count == 0)
                        {
                            col.Item().Padding(40).AlignCenter().Text("لا توجد مصاريف مسجلة لهذا المشروع")
                                .FontSize(12)
                                .FontColor("#64748B");
                            return;
                        }

                        // Summary Cards
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("عدد المصاريف").FontSize(8).FontColor("#64748B");
                                c.Item().Text($"{data.Expenses.Count} مصروف").FontSize(13).Bold().FontColor("#0F172A");
                            });

                            row.RelativeItem().Padding(4).Background("#FEF2F2").Border(1).BorderColor("#FECACA").Padding(10).Column(c =>
                            {
                                c.Item().Text("إجمالي المصاريف").FontSize(8).FontColor("#991B1B");
                                c.Item().Text($"{data.TotalAmount:N2} ج.م").FontSize(13).Bold().FontColor("#DC2626");
                            });
                        });

                        col.Item().Height(14);

                        // Expenses Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(24);   // م
                                columns.ConstantColumn(75);   // التاريخ
                                columns.RelativeColumn(2.2f); // البيان / المادة
                                columns.ConstantColumn(90);   // الكمية والسعر
                                columns.ConstantColumn(95);   // المبلغ الإجمالي
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("م");
                                header.Cell().Element(HeaderCellStyle).Text("التاريخ");
                                header.Cell().Element(HeaderCellStyle).Text("البيان / المادة");
                                header.Cell().Element(HeaderCellStyle).Text("الكمية والسعر");
                                header.Cell().Element(HeaderCellStyle).Text("المبلغ الإجمالي");
                            });

                            int idx = 1;
                            foreach (var exp in data.Expenses)
                            {
                                var bg = idx % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                                var label = !string.IsNullOrWhiteSpace(exp.MaterialName) ? exp.MaterialName : exp.Description;

                                table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(idx.ToString());
                                table.Cell().Element(c => CellStyle(c, bg)).Text(exp.ExpenseDate.ToString("yyyy/MM/dd"));
                                table.Cell().Element(c => CellStyle(c, bg)).Column(c =>
                                {
                                    c.Item().Text(label).Bold();
                                    if (!string.IsNullOrWhiteSpace(exp.Description) && exp.Description != label)
                                    {
                                        c.Item().Text(exp.Description).FontSize(7.5f).FontColor("#64748B");
                                    }
                                });

                                table.Cell().Element(c => CellStyle(c, bg)).Text(exp.Quantity > 0 ? $"{exp.Quantity} {exp.Unit} × {exp.UnitPrice:N0}" : "—");
                                table.Cell().Element(c => CellStyle(c, bg)).Text($"{exp.TotalAmount:N2} ج.م").Bold().FontColor("#0F172A");

                                idx++;
                            }
                        });
                    });
                });

                // Footer
                page.Footer().Element(footer => ComposeFooter(footer));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor("#E2E8F0");
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("نظام بدايات لإدارة المشاريع العقارية - تقرير مصاريف المشروع")
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
}
