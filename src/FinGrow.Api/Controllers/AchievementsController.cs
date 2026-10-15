namespace FinGrow.Api.Controllers;

using Contracts;
using Extensions;
using Application.Common;
using Application.Features.Achievements.CelebrateAchievements;
using Application.Features.Achievements.ListAchievements;
using Application.Features.Achievements.ListUncelebratedAchievements;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/achievements")]
[Authorize(Roles = Rol.Empleado)]
public sealed class AchievementsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new ListAchievementsQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpGet("uncelebrated")]
    public async Task<IActionResult> ListUncelebrated(CancellationToken cancellationToken) =>
        (await sender.Send(new ListUncelebratedAchievementsQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpPost("celebrate")]
    public async Task<IActionResult> Celebrate(CelebrateAchievementsRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(
            new CelebrateAchievementsCommand(currentUser.UserId!.Value, request.Codes),
            cancellationToken)).ToActionResult();
}
