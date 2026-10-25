namespace FinGrow.Application.Features.Metrics.GetCompanyInsights;

using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class GetCompanyInsightsQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    IFinancialHealthScoreRepository scores,
    MetricsAnonymizationPolicy anonymizationPolicy,
    IAiService ai,
    ILogger<GetCompanyInsightsQueryHandler> logger)
    : IRequestHandler<GetCompanyInsightsQuery, Result<CompanyInsightsResponse>>
{
    public async Task<Result<CompanyInsightsResponse>> Handle(
        GetCompanyInsightsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyInsightsResponse>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        var company = await companies.GetByIdAsync(companyId, cancellationToken);

        if (company is null)
        {
            return Result.Failure<CompanyInsightsResponse>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var minimumGroupSize = anonymizationPolicy.CompanyMinimumEmployees;
        var periods = await snapshots.ListCompanyPeriodStartsAsync(companyId, cancellationToken);

        if (periods.Count == 0)
        {
            return Result.Success(CompanyInsightsResponse.WithoutPeriod(minimumGroupSize));
        }

        // Los puntajes los calcula metrics-snapshot junto con la foto: el último mes fotografiado
        // es el último mes cerrado que se puede medir.
        var period = periods.Max();
        var monthScores = await scores.ListByCompanyAsync(companyId, period, cancellationToken);

        if (!anonymizationPolicy.CanRevealCompany(monthScores.Count))
        {
            return Result.Success(CompanyInsightsResponse.Anonymized(period, minimumGroupSize));
        }

        // Una preocupación que afecta a menos empleados que el umbral no se muestra ni viaja a la IA:
        // con uno o dos afectados, la empresa podría adivinar quiénes son.
        var concerns = CompanyConcernsCalculator
            .Count(monthScores.Select(score => score.Factors).ToList())
            .Where(concern => anonymizationPolicy.CanRevealCompany(concern.AffectedEmployees))
            .ToList();

        var input = CompanyInsightsInputBuilder.Build(
            company.Language,
            period,
            monthScores.Count,
            await snapshots.FindCompanyAsync(companyId, period, cancellationToken),
            anonymizationPolicy,
            concerns);

        var recommendations = await RecommendAsync(companyId, input, cancellationToken);

        return Result.Success(new CompanyInsightsResponse(
            period,
            false,
            minimumGroupSize,
            monthScores.Count,
            concerns.Select(CompanyConcernResponse.From).ToList(),
            recommendations?.Recommendations ?? Array.Empty<FinancialRecommendation>(),
            recommendations is not null));
    }

    private async Task<FinancialRecommendations?> RecommendAsync(
        Guid companyId,
        CompanyRecommendationsInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ai.RecommendCompanyAsync(input, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogAiUnavailable(logger, exception, companyId);
        }
        catch (JsonException exception)
        {
            LogAiUnavailable(logger, exception, companyId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogAiUnavailable(logger, exception, companyId);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "FinGrow-AI no pudo generar recomendaciones para la empresa {CompanyId}.")]
    private static partial void LogAiUnavailable(ILogger logger, Exception exception, Guid companyId);
}
