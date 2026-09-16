namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Features.Transactions.GetTransactionById;
using FinGrow.Application.Features.Transactions.ListTransactions;
using FinGrow.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class TransactionsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new ListTransactionsCommand(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTransactionByIdCommand(id), cancellationToken)).ToActionResult();
}
