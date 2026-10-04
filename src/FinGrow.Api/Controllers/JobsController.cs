namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Api.Jobs;
using FinGrow.Application.Features.Jobs;
using FinGrow.Application.Features.Jobs.ListJobRuns;
using FinGrow.Application.Features.Jobs.ListJobs;
using FinGrow.Application.Features.Jobs.RunJob;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/jobs")]
[AllowAnonymous]
[ValidateJobsKey]
public sealed class JobsController : ControllerBase
{
    private readonly ISender _sender;

    public JobsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<JobResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await _sender.Send(new ListJobsQuery(), cancellationToken)).ToActionResult();

    [HttpPost("{name}/run")]
    [ProducesResponseType<JobRunResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Run(string name, CancellationToken cancellationToken) =>
        (await _sender.Send(new RunJobCommand(name), cancellationToken)).ToActionResult();

    [HttpGet("{name}/runs")]
    [ProducesResponseType<IReadOnlyList<JobRunResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListRuns(
        string name,
        [FromQuery] int take = ListJobRunsQuery.DefaultTake,
        CancellationToken cancellationToken = default) =>
        (await _sender.Send(new ListJobRunsQuery(name, take), cancellationToken)).ToActionResult();
}
