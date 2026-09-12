using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Suppliers.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BedayaGroup.Infrastructure.Services;

public class SupplierStatementPdfGenerator : ISupplierStatementPdfGenerator
{
    private static readonly Lazy<byte[]> LogoBytes = new(() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "bedaya-logo.png")));

    public SupplierStatementPdfGenerator() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] GenerateSupplierStatementPdf(SupplierStatementDto statement, DateTime? fromDate, DateTime? toDate, DateTime generatedAt)
    {
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(9).FontColor("#1E293B"));
            page.ContentFromRightToLeft();
            page.Header().Element(c => Header(c, statement, fromDate, toDate, generatedAt));
            page.Content().Element(c => Content(c, statement));
            page.Footer().AlignCenter().Text("مجموعة بداية • كشف حساب مورد").FontSize(8).FontColor("#64748B");
        })).GeneratePdf();
    }

    private static void Header(IContainer container, SupplierStatementDto statement, DateTime? fromDate, DateTime? toDate, DateTime generatedAt)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(64).Height(64).Image(LogoBytes.Value).FitArea();
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("كشف حساب مورد").FontSize(18).Bold().FontColor("#0F172A");
                    c.Item().Text(statement.SupplierName).FontSize(13).Bold().FontColor("#0D9488");
                    c.Item().Text($"{statement.SupplierCode}  |  {statement.ProjectName ?? "غير محدد المشروع"}").FontColor("#64748B");
                });
                row.ConstantItem(110).AlignLeft().Text($"تاريخ الإصدار\n{generatedAt:yyyy/MM/dd HH:mm}").FontSize(8).FontColor("#64748B");
            });
            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#E2E8F0");
            col.Item().Background("#F8FAFC").Padding(8).Text($"الفترة: {(fromDate.HasValue ? fromDate.Value.ToString("yyyy/MM/dd") : "من بداية الحساب")} - {(toDate.HasValue ? toDate.Value.ToString("yyyy/MM/dd") : "حتى اليوم")}");
            col.Item().Height(10);
        });
    }

    private static void Content(IContainer container, SupplierStatementDto statement)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                Summary(row.RelativeItem(), "الرصيد الافتتاحي", statement.OpeningBalance, "#475569");
                Summary(row.RelativeItem(), "إجمالي المشتريات", statement.TotalInvoiced - statement.OpeningBalance, "#DC2626");
                Summary(row.RelativeItem(), "إجمالي المدفوع", statement.TotalPaid, "#15803D");
                Summary(row.RelativeItem(), "الرصيد المستحق", statement.CurrentBalance, "#B45309");
            });
            col.Item().Height(14);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(54); columns.RelativeColumn(1.6f); columns.RelativeColumn(1.1f); columns.ConstantColumn(48);
                    columns.ConstantColumn(66); columns.ConstantColumn(66); columns.ConstantColumn(68);
                });
                table.Header(h =>
                {
                    foreach (var text in new[] { "التاريخ", "المادة / البيان", "رقم المستند", "الكمية", "مدين", "دائن", "الرصيد" })
                        h.Cell().Element(HeaderCell).Text(text);
                });
                foreach (var item in statement.Items)
                {
                    var detail = item.MaterialName is null ? item.Description : $"{item.MaterialName} ({item.Quantity:0.###} {item.Unit} × {item.UnitPrice:N2})";
                    table.Cell().Element(Cell).Text(item.Date.ToString("yyyy/MM/dd"));
                    table.Cell().Element(Cell).Text(detail);
                    table.Cell().Element(Cell).Text(item.DocumentNumber);
                    table.Cell().Element(Cell).AlignCenter().Text(item.Quantity > 0 ? item.Quantity.ToString("0.###") : "-");
                    table.Cell().Element(Cell).Text(item.DebtAmount > 0 ? item.DebtAmount.ToString("N2") : "-");
                    table.Cell().Element(Cell).Text(item.CreditAmount > 0 ? item.CreditAmount.ToString("N2") : "-");
                    table.Cell().Element(Cell).Text(item.RunningBalance.ToString("N2")).Bold();
                }
            });
        });
    }

    private static void Summary(IContainer container, string label, decimal value, string color) => container.PaddingHorizontal(3).Background("#F8FAFC").Padding(8).Column(c =>
    {
        c.Item().Text(label).FontSize(8).FontColor("#64748B");
        c.Item().Text(value.ToString("N2")).FontSize(11).Bold().FontColor(color);
    });

    private static IContainer HeaderCell(IContainer container) => container.Background("#0F766E").Padding(5).AlignCenter().DefaultTextStyle(x => x.FontColor(Colors.White).Bold()).Border(0.5f).BorderColor("#CCFBF1");
    private static IContainer Cell(IContainer container) => container.BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5);
}
