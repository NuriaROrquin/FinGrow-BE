namespace FinGrow.Application.Features.Metrics.GetCompanyInsights;

using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Preocupaciones (HU-61) y recomendaciones (HU-64) de un mes con foto. Lo usan el panel, siempre con
/// el último mes fotografiado, y el PDF del reporte mensual (HU-82), con el mes del reporte. Si la IA
/// no responde, las preocupaciones salen igual y <c>RecommendationsAvailable</c> viaja en <c>false</c>.
/// </summary>
internal sealed partial class CompanyInsightsBuilder(
    IMetricsSnapshotRepository snapshots,
    IFinancialHealthScoreRepository scores,
    MetricsAnonymizationPolicy anonymizationPolicy,
    IAiService ai,
    ILogger<CompanyInsightsBuilder> logger)
{
    public async Task<CompanyInsightsResponse> BuildAsync(
        Company company,
        DateOnly period,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(company);

        var minimumGroupSize = anonymizationPolicy.CompanyMinimumEmployees;
        var monthScores = await scores.ListByCompanyAsync(company.Id, period, cancellationToken);

        if (!anonymizationPolicy.CanRevealCompany(monthScores.Count))
        {
            return CompanyInsightsResponse.Anonymized(period, minimumGroupSize);
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
            await snapshots.FindCompanyAsync(company.Id, period, cancellationToken),
            anonymizationPolicy,
            concerns);

        var recommendations = await RecommendAsync(company.Id, input, cancellationToken);

        return new CompanyInsightsResponse(
            period,
            false,
            minimumGroupSize,
            monthScores.Count,
            concerns.Select(CompanyConcernResponse.From).ToList(),
            recommendations?.Recommendations ?? Array.Empty<FinancialRecommendation>(),
            recommendations is not null);
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
