namespace FinGrow.Api.Logging;

public sealed class AxiomOptions
{
    public const string SectionName = "Axiom";
    public const string ServiceName = "fingrow-api";
    public const string DatasetHeader = "X-Axiom-Dataset";
    public const string DefaultDomain = "api.axiom.co";

    public string ApiToken { get; init; } = string.Empty;

    public string Dataset { get; init; } = string.Empty;

    public string Domain { get; init; } = DefaultDomain;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(ApiToken) || !string.IsNullOrWhiteSpace(Dataset);

    public string LogsEndpoint
    {
        get
        {
            var domain = string.IsNullOrWhiteSpace(Domain) ? DefaultDomain : Domain.Trim().TrimEnd('/');
            var baseUrl = domain.Contains("://", StringComparison.Ordinal) ? domain : $"https://{domain}";
            return $"{baseUrl}/v1/logs";
        }
    }

    public void EnsureComplete()
    {
        if (string.IsNullOrWhiteSpace(ApiToken))
        {
            throw new InvalidOperationException(
                "Para mandar los logs a Axiom falta configurar el token de ingesta (Axiom:ApiToken).");
        }

        if (string.IsNullOrWhiteSpace(Dataset))
        {
            throw new InvalidOperationException(
                "Para mandar los logs a Axiom falta configurar el dataset (Axiom:Dataset).");
        }
    }
}
