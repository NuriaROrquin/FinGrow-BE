namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.Investments.CreateInvestment;
using Application.Features.Investments.ListInvestments;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/investments")]
[Authorize]
public sealed class InvestmentsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new ListInvestmentsQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvestmentCommand command, CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }
}
