namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.Goals.AddContribution;
using Application.Features.Goals.CreateGoal;
using Application.Features.Goals.ListContributions;
using Application.Features.Goals.ListGoals;
using Application.Features.Goals.RemoveContribution;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/goals")]
[Authorize]
public sealed class GoalsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new ListGoalsQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateGoalCommand command, CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpGet("{goalId:guid}/contributions")]
    public async Task<IActionResult> ListContributions(Guid goalId, CancellationToken cancellationToken) =>
        (await sender.Send(
            new ListGoalContributionsQuery(currentUser.UserId!.Value, goalId),
            cancellationToken)).ToActionResult();

    [HttpPost("{goalId:guid}/contributions")]
    public async Task<IActionResult> AddContribution(
        Guid goalId,
        AddGoalContributionCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value, GoalId = goalId };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpDelete("{goalId:guid}/contributions/{contributionId:guid}")]
    public async Task<IActionResult> RemoveContribution(
        Guid goalId,
        Guid contributionId,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new RemoveGoalContributionCommand(currentUser.UserId!.Value, goalId, contributionId),
            cancellationToken)).ToActionResult();
}
