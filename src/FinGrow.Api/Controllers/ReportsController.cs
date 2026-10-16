namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Common;
using Application.Features.Reports.ExpenseBreakdown;
using Application.Features.Reports.FinancialInsights;
using Application.Features.Reports.MonthlySummary;
using Application.Features.Reports.SavingsVsGoals;
using Application.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("savings-vs-goals")]
    public async Task<IActionResult> SavingsVsGoals(
        [FromQuery] Currency currency = Currency.ARS,
        [FromQuery] int? months = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetSavingsVsGoalsQuery(currentUser.UserId!.Value, currency, months),
            cancellationToken)).ToActionResult();

    [HttpGet("monthly-summary")]
    [Authorize(Roles = Rol.Empleado)]
    public async Task<IActionResult> GetMonthlySummary(
        [FromQuery(Name = "from"), BindRequired] DateOnly fromDate,
        [FromQuery(Name = "to"), BindRequired] DateOnly toDate,
        [FromQuery] Currency currency = Currency.ARS,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetMonthlySummaryQuery(currentUser.UserId!.Value, currency, fromDate, toDate),
            cancellationToken)).ToActionResult();

    [HttpGet("insights")]
    [Authorize(Roles = Rol.Empleado)]
    public async Task<IActionResult> GetInsights(
        [FromQuery(Name = "from"), BindRequired] DateOnly fromDate,
        [FromQuery(Name = "to"), BindRequired] DateOnly toDate,
        [FromQuery] Currency currency = Currency.ARS,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetFinancialInsightsQuery(currentUser.UserId!.Value, currency, fromDate, toDate),
            cancellationToken)).ToActionResult();

    [HttpGet("expense-breakdown")]
    [Authorize(Roles = Rol.Empleado)]
    public async Task<IActionResult> GetExpenseBreakdown(
        [FromQuery(Name = "from"), BindRequired] DateOnly fromDate,
        [FromQuery(Name = "to"), BindRequired] DateOnly toDate,
        [FromQuery] Currency currency = Currency.ARS,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetExpenseBreakdownQuery(currentUser.UserId!.Value, currency, fromDate, toDate),
            cancellationToken)).ToActionResult();
}
