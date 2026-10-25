namespace FinGrow.Application.Features.Metrics.GetCompanyMonthlyReport;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics.MonthlyReport;
using MediatR;

public sealed record GetCompanyMonthlyReportQuery : IRequest<Result<CompanyMonthlyReportResponse>>;
