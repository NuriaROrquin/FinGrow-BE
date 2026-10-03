namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Common;
using Application.DTOs;
using Application.Features.Budgets.CreateBudget;
using Application.Features.Budgets.DuplicatePreviousBudget;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Budgets.GetBudget;
using Application.Features.Budgets.SetCategoryLimit;
using Application.Features.Budgets.DeleteBudget;
using Application.Features.Budgets.RemoveCategoryLimit;
using Application.Features.Budgets.ChangeCurrency;
using Domain.Enums;

[ApiController]
[Route("api/budgets")]
[Authorize(Roles = Rol.Empleado)]
public sealed class BudgetsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int year, int month, CancellationToken cancellationToken) =>
        (await sender.Send(new GetBudgetQuery(currentUser.UserId!.Value, year, month), cancellationToken)).ToActionResult();

    [HttpPost]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateBudgetCommand command, CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPost("duplicate-previous")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DuplicatePrevious(
        DuplicatePreviousBudgetCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPut("{year:int}/{month:int}/limits")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetLimit(
        int year,
        int month,
        SetBudgetCategoryLimitCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value, Year = year, Month = month };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPut("{year:int}/{month:int}/currency")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeCurrency(
        int year,
        int month,
        ChangeBudgetCurrencyCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { EmployeeId = currentUser.UserId!.Value, Year = year, Month = month };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpDelete("{year:int}/{month:int}/limits/{category}")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveLimit(
        int year,
        int month,
        ExpenseCategory category,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new RemoveBudgetCategoryLimitCommand(currentUser.UserId!.Value, year, month, category),
            cancellationToken)).ToActionResult();

    [HttpDelete("{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int year, int month, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteBudgetCommand(currentUser.UserId!.Value, year, month), cancellationToken))
        .ToActionResult();
}
