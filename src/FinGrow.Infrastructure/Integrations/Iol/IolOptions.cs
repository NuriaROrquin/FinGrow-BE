namespace FinGrow.Infrastructure.Integrations.Iol;

using System.ComponentModel.DataAnnotations;

public sealed class IolOptions
{
    public const string SectionName = "Iol";

    [Url]
    public string ApiBaseUrl { get; init; } = "https://api.invertironline.com/";

    [Range(1, 1440)]
    public int SyncIntervalMinutes { get; init; } = 30;

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}
