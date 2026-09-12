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
            if (data.Receipts.Count == 0)
            {
                col.Item().Padding(40).AlignCenter().Text("لا توجد مقبوضات مطابقة للفلتر المحدد")
                    .FontSize(12)
                    .FontColor("#64748B");
                return;
            }

            if (data.IsSingleShareholderReport)
            {
                col.Item().Element(singleShareholderContent => ComposeSingleShareholderContent(singleShareholderContent, data));
                return;
            }

            // Receipts Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(24);  // م
                    columns.ConstantColumn(65);  // التاريخ
                    columns.RelativeColumn(2);   // المساهم
                    columns.ConstantColumn(75);  // الهاتف
                    columns.RelativeColumn(1.5f);// المشروع
                    columns.ConstantColumn(45);  // الأسهم
                    columns.ConstantColumn(75);  // المقبوض
                    columns.RelativeColumn(2.5f);// التوزيع / الوصف
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
                    header.Cell().Element(HeaderCellStyle).Text("بيان التوزيع / الوصف");
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

                    // Allocation column inside table
                    table.Cell().Element(c => CellStyle(c, bg)).Column(allocCol =>
                    {
                        if (r.Allocations.Count > 0)
                        {
                            foreach (var a in r.Allocations)
                            {
                                allocCol.Item().Text(t =>
                                {
                                    t.Span($"• {a.InstallmentName}: ").FontSize(8).FontColor("#475569");
                                    t.Span($"{a.AmountAllocated:N0} ج.م").FontSize(8).Bold().FontColor("#1E293B");
                                });
                            }
                            if (r.UnallocatedAmount > 0)
                            {
                                allocCol.Item().Text($"• غير موزع: {r.UnallocatedAmount:N0} ج.م")
                                    .FontSize(8).Bold().FontColor("#DC2626");
                            }
                        }
                        else
                        {
                            allocCol.Item().Text(string.IsNullOrWhiteSpace(r.Description) ? "غير موزع" : r.Description)
                                .FontSize(8).FontColor("#64748B");
                        }
                    });

                    index++;
                }
            });

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
                        c.Item().Text("إجمالي المقبوضات").FontSize(8).FontColor("#475569");
                        c.Item().Text($"{data.TotalReceived:N0} جنيه مصري").FontSize(12).Bold().FontColor("#0D9488");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("إجمالي المبالغ الموزعة").FontSize(8).FontColor("#475569");
                        c.Item().Text($"{data.TotalAllocated:N0} جنيه مصري").FontSize(12).Bold().FontColor("#2563EB");
                    });

                    if (data.TotalUnallocated > 0)
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("المبالغ غير الموزعة").FontSize(8).FontColor("#475569");
                            c.Item().Text($"{data.TotalUnallocated:N0} جنيه مصري").FontSize(12).Bold().FontColor("#DC2626");
                        });
                    }

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("عدد عمليات التحصيل").FontSize(8).FontColor("#475569");
                        c.Item().Text($"{data.ReceiptCount} عملية").FontSize(12).Bold().FontColor("#0F172A");
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
                    columns.ConstantColumn(24);
                    columns.ConstantColumn(68);
                    columns.RelativeColumn(1.5f);
                    columns.ConstantColumn(48);
                    columns.ConstantColumn(82);
                    columns.RelativeColumn(2.8f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("م");
                    header.Cell().Element(HeaderCellStyle).Text("التاريخ");
                    header.Cell().Element(HeaderCellStyle).Text("المشروع");
                    header.Cell().Element(HeaderCellStyle).Text("الأسهم");
                    header.Cell().Element(HeaderCellStyle).Text("المبلغ المستلم");
                    header.Cell().Element(HeaderCellStyle).Text("توزيع الدفعة / البيان");
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
                    table.Cell().Element(c => CellStyle(c, background)).Column(allocations =>
                    {
                        if (receipt.Allocations.Count > 0)
                        {
                            foreach (var allocation in receipt.Allocations)
                            {
                                allocations.Item().Text(text =>
                                {
                                    text.Span($"• {allocation.InstallmentName}: ").FontSize(8).FontColor("#475569");
                                    text.Span($"{allocation.AmountAllocated:N0} ج.م").FontSize(8).Bold().FontColor("#1E293B");
                                });
                            }

                            if (receipt.UnallocatedAmount > 0)
                            {
                                allocations.Item().Text($"• غير موزع: {receipt.UnallocatedAmount:N0} ج.م")
                                    .FontSize(8).Bold().FontColor("#DC2626");
                            }

                            if (!string.IsNullOrWhiteSpace(receipt.Description))
                            {
                                allocations.Item().PaddingTop(2).Text(text =>
                                {
                                    text.Span("ملاحظات: ").FontSize(8).FontColor("#64748B");
                                    text.Span(receipt.Description).FontSize(8).FontColor("#475569");
                                });
                            }
                        }
                        else
                        {
                            allocations.Item().Text(string.IsNullOrWhiteSpace(receipt.Description) ? "غير موزع" : receipt.Description)
                                .FontSize(8).FontColor("#64748B");
                        }
                    });

                    index++;
                }
            });

            col.Item().Height(16);
            col.Item().Background("#F1F5F9").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(summary =>
                {
                    summary.Item().Text("إجمالي المقبوضات").FontSize(8).FontColor("#475569");
                    summary.Item().Text($"{data.TotalReceived:N0} جنيه مصري").FontSize(12).Bold().FontColor("#0D9488");
                });
                row.RelativeItem().Column(summary =>
                {
                    summary.Item().Text("إجمالي المبالغ الموزعة").FontSize(8).FontColor("#475569");
                    summary.Item().Text($"{data.TotalAllocated:N0} جنيه مصري").FontSize(12).Bold().FontColor("#2563EB");
                });
                row.RelativeItem().Column(summary =>
                {
                    summary.Item().Text("عدد عمليات التحصيل").FontSize(8).FontColor("#475569");
                    summary.Item().Text($"{data.ReceiptCount} عملية").FontSize(12).Bold().FontColor("#0F172A");
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor("#E2E8F0");
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("نظام بدايات لإدارة المشاريع العقارية - سجل المقبوضات وتوزيع المبالغ")
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
