namespace FinGrow.Application.Features.Metrics.GetCompanyMonthlyReportPdf;

using FinGrow.Application.Common;
using MediatR;

/// <summary>El PDF que descarga "Exportar reporte" del panel (HU-65): el mismo que adjunta el mail mensual (HU-82).</summary>
public sealed record GetCompanyMonthlyReportPdfQuery : IRequest<Result<CompanyReportPdfFile>>;

public sealed record CompanyReportPdfFile(string FileName, ReadOnlyMemory<byte> Content);
