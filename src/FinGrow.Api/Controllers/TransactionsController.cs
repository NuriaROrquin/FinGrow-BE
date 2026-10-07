namespace FinGrow.Api.Controllers;

using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Transactions.ConfirmTransaction;
using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Features.Transactions.DiscardTransaction;
using FinGrow.Application.Features.Transactions.ListPendingTransactions;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Application.Features.Transactions.GetTransactionById;
using FinGrow.Application.Features.Transactions.GetTransactionSummary;
using FinGrow.Application.Features.Transactions.DeleteTransaction;
using FinGrow.Application.Features.Transactions.UpdateTransaction;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Features.Transactions.GetMonthlyExpenses;
using FinGrow.Application.Features.Transactions.GetMonthlyIncomeExpenses;
using FinGrow.Application.Features.Transactions.GetExpensesByCategory;
using FinGrow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class TransactionsController(
    ISender sender,
    ICurrentUser currentUser,
    ITransactionExcelExporter transactionExcelExporter) : ControllerBase
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

    [HttpGet("monthly-expenses")]
    [ProducesResponseType<MonthlyExpensesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyExpenses(
        [FromQuery] Currency currency = Currency.ARS,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetMonthlyExpensesQuery(currency), cancellationToken)).ToActionResult();

    [HttpGet("income-vs-expenses")]
    [ProducesResponseType<MonthlyIncomeExpensesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyIncomeExpenses(
        [FromQuery] Currency currency = Currency.ARS,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetMonthlyIncomeExpensesQuery(currency), cancellationToken)).ToActionResult();

    [HttpGet("expenses-by-category")]
    [ProducesResponseType<ExpensesByCategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpensesByCategory(
        [FromQuery] Currency currency = Currency.ARS,
        [FromQuery(Name = "dateFrom")] DateOnly? dateFrom = null,
        [FromQuery(Name = "dateTo")] DateOnly? dateTo = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetExpensesByCategoryQuery(currency, dateFrom, dateTo), cancellationToken)).ToActionResult();

    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] TransactionQueryParameters parameters,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetTransactionHistoryQuery(parameters.ToFilters()), cancellationToken)).ToActionResult();

    [HttpGet("pending")]
    [ProducesResponseType<PendingTransactionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken = default) =>
        (await sender.Send(new ListPendingTransactionsQuery(), cancellationToken)).ToActionResult();

    [HttpGet("export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export(
        [FromQuery] TransactionExportQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await transactionExcelExporter.ExportAsync(
            currentUser.UserId!.Value,
            parameters.ToFilters(),
            cancellationToken);

        return result.IsSuccess
            ? File(
                result.Value,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "transacciones.xlsx")
            : result.ToActionResult();
    }

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

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(
        Guid id,
        ConfirmTransactionCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { Id = id };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPost("{id:guid}/discard")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Discard(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new DiscardTransactionCommand(id), cancellationToken)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteTransactionCommand(id), cancellationToken)).ToActionResult();
}
