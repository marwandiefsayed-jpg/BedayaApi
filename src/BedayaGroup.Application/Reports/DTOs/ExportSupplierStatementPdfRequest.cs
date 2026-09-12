namespace BedayaGroup.Application.Reports.DTOs;

public class ExportSupplierStatementPdfRequest
{
    public int SupplierId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
