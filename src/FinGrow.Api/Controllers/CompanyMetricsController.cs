namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Metrics.GetCompanyMetrics;
using FinGrow.Application.Features.Metrics.GetCompanyMetricsComparison;
using FinGrow.Application.Features.Metrics.GetCompanyEducationMetrics;
using FinGrow.Application.Features.Metrics.GetCompanyInsights;
using FinGrow.Application.Features.Metrics.GetCompanyMonthlyReport;
using FinGrow.Application.Features.Metrics.GetCompanyTrends;
using FinGrow.Application.Features.Metrics.GetDepartmentMetricsOverview;
using FinGrow.Application.Features.Metrics.GetDepartmentMetrics;
using FinGrow.Application.Features.Metrics.GetParticipation;
using FinGrow.Application.Features.Metrics.MonthlyReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/company")]
[Authorize(Roles = Rol.Empresa)]
public sealed class CompanyMetricsController(ISender sender) : ControllerBase
{
    [HttpGet("metrics")]
    [ProducesResponseType<IReadOnlyList<CompanyMetricsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ListCompanyMetrics(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken) =>
        (await sender.Send(new GetCompanyMetricsQuery(fromDate, toDate), cancellationToken)).ToActionResult();

    [HttpGet("metrics/comparison")]
    [ProducesResponseType<CompanyMetricsComparisonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CompareMetrics(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new GetCompanyMetricsComparisonQuery(fromDate, toDate),
            cancellationToken)).ToActionResult();

    [HttpGet("metrics/education")]
    [ProducesResponseType<CompanyEducationMetricsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> EducationMetrics(
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new GetCompanyEducationMetricsQuery(),
            cancellationToken)).ToActionResult();

    [HttpGet("metrics/insights")]
    [ProducesResponseType<CompanyInsightsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Insights(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCompanyInsightsQuery(), cancellationToken)).ToActionResult();

    [HttpGet("metrics/trends")]
    [ProducesResponseType<CompanyTrendsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Trends(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCompanyTrendsQuery(), cancellationToken)).ToActionResult();

    [HttpGet("metrics/report")]
    [ProducesResponseType<CompanyMonthlyReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Report(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCompanyMonthlyReportQuery(), cancellationToken)).ToActionResult();

    [HttpGet("metrics/participation")]
    [ProducesResponseType<CompanyParticipationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Participation(CancellationToken cancellationToken) =>
        (await sender.Send(new GetParticipationQuery(), cancellationToken)).ToActionResult();

    [HttpGet("departments/{departmentId:guid}/metrics")]
    [ProducesResponseType<IReadOnlyList<DepartmentMetricsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ListDepartmentMetrics(
        Guid departmentId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new GetDepartmentMetricsQuery(departmentId, fromDate, toDate),
            cancellationToken)).ToActionResult();

    [HttpGet("metrics/departments")]
    [ProducesResponseType<IReadOnlyList<DepartmentMetricsOverviewResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListDepartmentMetricsSummary(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new GetDepartmentMetricsOverviewQuery(fromDate, toDate),
            cancellationToken)).ToActionResult();
}
