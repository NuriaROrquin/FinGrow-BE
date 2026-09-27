namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.Investments.CreateInvestment;
using Application.Features.Investments.DeleteInvestment;
using Application.Features.Investments.GetPortfolioSummary;
using Application.Features.Investments.ListInvestments;
using Application.Features.Investments.UpdateInvestment;
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

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        (await sender.Send(new GetPortfolioSummaryQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvestmentCommand command, CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateInvestmentCommand command, CancellationToken cancellationToken)
    {
        command = command with { Id = id, EmployeeId = currentUser.UserId!.Value };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteInvestmentCommand(id, currentUser.UserId!.Value), cancellationToken)).ToActionResult();
}
