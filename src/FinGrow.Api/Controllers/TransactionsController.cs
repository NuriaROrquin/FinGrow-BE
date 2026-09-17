namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Application.Features.Transactions.GetTransactionById;
using FinGrow.Application.Features.Transactions.GetTransactionSummary;
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

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        (await sender.Send(new GetTransactionSummaryQuery(), cancellationToken)).ToActionResult();

    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetTransactionHistoryQuery(pageNumber, pageSize, search, type),
            cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTransactionByIdCommand(id), cancellationToken)).ToActionResult();
}
