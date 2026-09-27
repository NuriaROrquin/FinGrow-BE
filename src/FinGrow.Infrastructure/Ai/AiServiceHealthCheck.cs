namespace FinGrow.Infrastructure.Ai;

using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class AiServiceHealthCheck : IHealthCheck
{
    private readonly IAiService _aiService;

    public AiServiceHealthCheck(IAiService aiService) => _aiService = aiService;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await _aiService.IsHealthyAsync(cancellationToken);

        return isHealthy
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(context.Registration.FailureStatus, "FinGrow-AI no responde en /health.");
    }
}
