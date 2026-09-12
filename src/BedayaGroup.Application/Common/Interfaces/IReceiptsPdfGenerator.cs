using BedayaGroup.Application.Reports.DTOs;

namespace BedayaGroup.Application.Common.Interfaces;

public interface IReceiptsPdfGenerator
{
    byte[] GenerateReceiptsDistributionPdf(ReceiptsDistributionReportDto data);
}
