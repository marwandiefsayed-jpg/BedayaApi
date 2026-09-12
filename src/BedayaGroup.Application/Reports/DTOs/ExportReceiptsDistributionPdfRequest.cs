namespace BedayaGroup.Application.Reports.DTOs;

public class ExportReceiptsDistributionPdfRequest
{
    public int? ProjectId { get; set; }
    public int? ShareholderId { get; set; }
    public int? ShareId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
