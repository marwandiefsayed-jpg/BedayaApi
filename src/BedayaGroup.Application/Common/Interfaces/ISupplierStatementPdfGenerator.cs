using BedayaGroup.Application.Suppliers.DTOs;

namespace BedayaGroup.Application.Common.Interfaces;

public interface ISupplierStatementPdfGenerator
{
    byte[] GenerateSupplierStatementPdf(SupplierStatementDto statement, DateTime? fromDate, DateTime? toDate, DateTime generatedAt);
}
