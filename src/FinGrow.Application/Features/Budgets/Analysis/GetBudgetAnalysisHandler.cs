namespace FinGrow.Application.Features.Budgets.Analysis;

using System.Text.Json;
using Common;
using Domain.Enums;
using Domain.Repositories;
using Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Reports.MonthlySummary;

internal sealed partial class GetBudgetAnalysisHandler(
    IBudgetRepository budgetRepository,
    IMonthlySummaryReadRepository monthlySummaryReadRepository,
    IEmployeeRepository employeeRepository,
    IAiService ai,
    IDateTimeProvider dateTimeProvider,
    ILogger<GetBudgetAnalysisHandler> logger)
    : IRequestHandler<GetBudgetAnalysisQuery, Result<BudgetAnalysisResponse>>
{
    /// <summary>Cuántos meses cerrados hacia atrás se analizan, sin contar el mes en curso.</summary>
    public const int AnalyzedMonths = 6;

    /// <summary>Meses con presupuesto que hacen falta para el análisis, contando el mes en curso.</summary>
    public const int RequiredBudgetMonths = 2;

    public async Task<Result<BudgetAnalysisResponse>> Handle(
        GetBudgetAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var today = dateTimeProvider.Today;
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var firstMonth = currentMonth.AddMonths(-AnalyzedMonths);

        // Un presupuesto sin topes no aporta cumplimiento: no cuenta para el análisis.
        var budgets = (await budgetRepository.ListMonthlyAsync(
                request.EmployeeId,
                firstMonth,
                currentMonth,
                cancellationToken))
            .Where(budget => budget.Limits.Count > 0 && budget.Currency == request.Currency)
            .ToList();
        // El mes en curso cuenta para habilitar y la IA lo ve, pero el resumen sale de los meses cerrados:
        // su gasto todavía es parcial. Como hay un solo mes en curso, llegar al mínimo implica al menos uno cerrado.
        var hasCurrentMonthBudget = budgets.Exists(budget => budget.PeriodStart == currentMonth);
        var eligibility = new BudgetAnalysisEligibility(
            budgets.Count >= RequiredBudgetMonths,
            budgets.Count,
            RequiredBudgetMonths,
            AnalyzedMonths,
            hasCurrentMonthBudget);

        if (!eligibility.IsEligible)
        {
            return Result.Success(BudgetAnalysisResponse.NotEligible(request.Currency, eligibility));
        }

        var totals = await monthlySummaryReadRepository.ListMonthlyTotalsAsync(
            request.EmployeeId,
            request.Currency,
            firstMonth,
            currentMonth.AddMonths(1).AddDays(-1),
            cancellationToken);
        var months = MonthlySummaryBuilder.Build(totals, budgets.Select(budget => budget.PeriodStart).ToList());
        var usage = BudgetAnalysisBuilder.Usage(budgets, months);
        var summary = BudgetAnalysisBuilder.Summarize(usage.Where(month => month.PeriodStart < currentMonth).ToList());

        var input = new BudgetRecommendationsInput(
            await LanguageOfAsync(request.EmployeeId, cancellationToken),
            request.Currency,
            usage
                .Select(month => new BudgetRecommendationMonth(
                    month.PeriodStart.Year,
                    month.PeriodStart.Month,
                    month.PeriodStart == currentMonth,
                    month.Limits,
                    month.UnbudgetedExpenses))
                .ToList());

        var recommendations = await RecommendAsync(request.EmployeeId, input, cancellationToken);

        return Result.Success(new BudgetAnalysisResponse(
            request.Currency,
            eligibility,
            summary,
            recommendations?.Recommendations ?? Array.Empty<FinancialRecommendation>(),
            recommendations is not null));
    }

    private async Task<Language> LanguageOfAsync(Guid employeeId, CancellationToken cancellationToken) =>
        (await employeeRepository.GetByIdAsync(employeeId, cancellationToken))?.Language ?? Language.es;

    private async Task<FinancialRecommendations?> RecommendAsync(
        Guid employeeId,
        BudgetRecommendationsInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ai.RecommendBudgetAsync(input, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogAiUnavailable(logger, exception, employeeId);
        }
        catch (JsonException exception)
        {
            LogAiUnavailable(logger, exception, employeeId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogAiUnavailable(logger, exception, employeeId);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "FinGrow-AI no pudo generar recomendaciones de presupuesto para el empleado {EmployeeId}.")]
    private static partial void LogAiUnavailable(ILogger logger, Exception exception, Guid employeeId);
}
