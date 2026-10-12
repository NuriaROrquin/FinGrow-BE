namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics.GetCompanyMetrics;
using FinGrow.Application.Features.Metrics.GetDepartmentMetrics;
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
}

