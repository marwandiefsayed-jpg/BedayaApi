using BedayaGroup.Application.Reports.DTOs;
using BedayaGroup.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BedayaGroup.UnitTests;

public class ReceiptsDistributionPdfTests
{
    [Fact]
    public void ReceiptsDistributionReportDto_Totals_ShouldCalculateCorrectly()
    {
        // Arrange
        var receipt1 = new ReceiptItemDto(
            Id: 1,
            Date: new DateTime(2026, 9, 12),
            ShareholderName: "أحمد محمد",
            ShareholderPhone: "01012345678",
            ProjectName: "مشروع النرجس",
            NumberOfShares: 2.5m,
            AmountReceived: 800m,
            Description: "سداد دفعة أولى وثانية",
            Allocations: new List<AllocationItemDto>
            {
                new AllocationItemDto(1, "الدفعة الأولى", 600m),
                new AllocationItemDto(2, "الدفعة الثانية", 200m)
            }
        );

        var receipt2 = new ReceiptItemDto(
            Id: 2,
            Date: new DateTime(2026, 9, 13),
            ShareholderName: "محمود حسن",
            ShareholderPhone: "01198765432",
            ProjectName: "مشروع النرجس",
            NumberOfShares: 1.0m,
            AmountReceived: 500m,
            Description: "سداد جزئي",
            Allocations: new List<AllocationItemDto>
            {
                new AllocationItemDto(1, "الدفعة الأولى", 400m)
            }
        );

        var reportDto = new ReceiptsDistributionReportDto(
            ProjectName: "مشروع النرجس",
            ShareName: null,
            FromDate: new DateTime(2026, 9, 1),
            ToDate: new DateTime(2026, 9, 30),
            GeneratedAt: DateTime.UtcNow,
            Receipts: new List<ReceiptItemDto> { receipt1, receipt2 }
        );

        // Act & Assert
        receipt1.TotalAllocated.Should().Be(800m);
        receipt1.UnallocatedAmount.Should().Be(0m);

        receipt2.TotalAllocated.Should().Be(400m);
        receipt2.UnallocatedAmount.Should().Be(100m);

        reportDto.TotalReceived.Should().Be(1300m);
        reportDto.TotalAllocated.Should().Be(1200m);
        reportDto.TotalUnallocated.Should().Be(100m);
        reportDto.ReceiptCount.Should().Be(2);
    }

    [Fact]
    public void ReceiptsPdfGenerator_ShouldGenerateNonEmptyPdfBytes()
    {
        // Arrange
        var generator = new ReceiptsPdfGenerator();
        var reportDto = new ReceiptsDistributionReportDto(
            ProjectName: "مشروع الردسي",
            ShareName: "سهم تجاري",
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 12, 31),
            GeneratedAt: DateTime.UtcNow,
            Receipts: new List<ReceiptItemDto>
            {
                new ReceiptItemDto(
                    Id: 1,
                    Date: new DateTime(2026, 9, 12),
                    ShareholderName: "سارة إبراهيم",
                    ShareholderPhone: "01234567890",
                    ProjectName: "مشروع الردسي",
                    NumberOfShares: 1.5m,
                    AmountReceived: 150000m,
                    Description: "دفعة مقدمة",
                    Allocations: new List<AllocationItemDto>
                    {
                        new AllocationItemDto(10, "الدفعة الأولى", 100000m),
                        new AllocationItemDto(11, "الدفعة الثانية", 50000m)
                    }
                )
            }
        );

        // Act
        var bytes = generator.GenerateReceiptsDistributionPdf(reportDto);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(100);
        // Header bytes for PDF start with %PDF
        System.Text.Encoding.ASCII.GetString(bytes.Take(4).ToArray()).Should().Be("%PDF");
    }

    [Fact]
    public void ReceiptsPdfGenerator_ShouldGenerateSingleShareholderLayoutPdf()
    {
        var receipt = new ReceiptItemDto(
            Id: 1,
            Date: new DateTime(2026, 9, 12),
            ShareholderName: "Ahmed Mohamed",
            ShareholderPhone: "01234567890",
            ProjectName: "New Cairo Project",
            NumberOfShares: 2m,
            AmountReceived: 100000m,
            Description: null,
            Allocations: new List<AllocationItemDto>
            {
                new AllocationItemDto(1, "First Installment", 100000m)
            });

        var report = new ReceiptsDistributionReportDto(
            ProjectName: "New Cairo Project",
            ShareName: null,
            FromDate: null,
            ToDate: null,
            GeneratedAt: DateTime.UtcNow,
            Receipts: new List<ReceiptItemDto> { receipt },
            SelectedShareholderName: "Ahmed Mohamed");

        report.IsSingleShareholderReport.Should().BeTrue();

        var bytes = new ReceiptsPdfGenerator().GenerateReceiptsDistributionPdf(report);

        bytes.Should().NotBeNull();
        System.Text.Encoding.ASCII.GetString(bytes.Take(4).ToArray()).Should().Be("%PDF");
    }
}
