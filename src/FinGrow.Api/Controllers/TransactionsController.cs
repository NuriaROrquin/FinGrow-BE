namespace FinGrow.Api.Controllers;

using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Application.Features.Transactions.GetTransactionById;
using FinGrow.Application.Features.Transactions.GetTransactionSummary;
using FinGrow.Application.Features.Transactions.UpdateTransaction;
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
    public async Task<IActionResult> GetSummary(
        [FromQuery(Name = "dateFrom")] DateOnly? fromDate = null,
        [FromQuery(Name = "dateTo")] DateOnly? toDate = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetTransactionSummaryQuery(fromDate, toDate), cancellationToken)).ToActionResult();

    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] TransactionQueryParameters parameters,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetTransactionHistoryQuery(parameters.ToFilters()), cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTransactionByIdCommand(id), cancellationToken)).ToActionResult();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { Id = id };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }
}
