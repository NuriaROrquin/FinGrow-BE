namespace FinGrow.Application.Features.Dashboard.GetDashboard;

using Common;
using Domain.Enums;
using FluentValidation;
using Interfaces;
using MediatR;

public sealed record GetDashboardQuery(
    Currency Currency,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<DashboardResponse>>;

/// <summary>Resumen financiero del empleado para el período solicitado.</summary>
public sealed record DashboardResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    Currency Currency,
    bool HasMovements,
    decimal? Balance,
    decimal? Income,
    decimal? Expenses,
    decimal? Savings,
    decimal? SavingsRate,
    decimal? PreviousPeriodSavingsRate);

public sealed class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
    public GetDashboardQueryValidator()
    {
        RuleFor(query => query.Currency)
            .Must(currency => currency is Currency.ARS or Currency.USD)
            .WithMessage("El dashboard solo soporta ARS y USD.");

        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue
                || !query.ToDate.HasValue
                || query.FromDate <= query.ToDate)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");
    }
}

internal sealed class GetDashboardQueryHandler(
    ITransactionReadRepository transactionReadRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetDashboardQuery, Result<DashboardResponse>>
{
    public async Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<DashboardResponse>(
                Error.Unauthorized(
                    "Dashboard.Unauthenticated",
                    "Hay que iniciar sesion para consultar el dashboard."));
        }

        var currentDate = dateTimeProvider.Today;
        var fromDate = request.FromDate
            ?? new DateOnly(currentDate.Year, currentDate.Month, 1);
        var toDate = request.ToDate ?? currentDate;

        var summary = await transactionReadRepository.GetSummaryAsync(
            employeeId,
            fromDate,
            toDate,
            cancellationToken);

        var income = GetIncome(summary, request.Currency);
        var expenses = GetExpenses(summary, request.Currency);
        var previousPeriodStart = new DateOnly(fromDate.Year, fromDate.Month, 1).AddMonths(-1);
        var previousPeriodEnd = previousPeriodStart.AddMonths(1).AddDays(-1);
        var previousSummary = await transactionReadRepository.GetSummaryAsync(
            employeeId,
            previousPeriodStart,
            previousPeriodEnd,
            cancellationToken);
        var previousIncome = GetIncome(previousSummary, request.Currency);
        var previousExpenses = GetExpenses(previousSummary, request.Currency);
        var previousPeriodSavingsRate = CalculateSavingsRate(previousIncome, previousExpenses);
        var hasMovements = income > 0m || expenses > 0m;

        if (!hasMovements)
        {
            return Result.Success(new DashboardResponse(
                fromDate,
                toDate,
                request.Currency,
                false,
                null,
                null,
                null,
                null,
                null,
                previousPeriodSavingsRate));
        }

        var savings = income - expenses;
        var savingsRate = CalculateSavingsRate(income, expenses);
        var accumulatedSummary = await transactionReadRepository.GetSummaryAsync(
            employeeId,
            toDate: toDate,
            cancellationToken: cancellationToken);
        var accumulatedIncome = request.Currency == Currency.ARS
            ? accumulatedSummary.TotalIncomeArs
            : accumulatedSummary.TotalIncomeUsd;
        var accumulatedExpenses = request.Currency == Currency.ARS
            ? accumulatedSummary.TotalExpenseArs
            : accumulatedSummary.TotalExpenseUsd;
        var balance = accumulatedIncome - accumulatedExpenses;

        return Result.Success(new DashboardResponse(
            fromDate,
            toDate,
            request.Currency,
            true,
            balance,
            income,
            expenses,
            savings,
            savingsRate,
            previousPeriodSavingsRate));
    }

    private static decimal GetIncome(TransactionSummary summary, Currency currency) =>
        currency == Currency.ARS ? summary.TotalIncomeArs : summary.TotalIncomeUsd;

    private static decimal GetExpenses(TransactionSummary summary, Currency currency) =>
        currency == Currency.ARS ? summary.TotalExpenseArs : summary.TotalExpenseUsd;

    private static decimal? CalculateSavingsRate(decimal income, decimal expenses) =>
        income > 0m ? (income - expenses) / income * 100m : null;
}
