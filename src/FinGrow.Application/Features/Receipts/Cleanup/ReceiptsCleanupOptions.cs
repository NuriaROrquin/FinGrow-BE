namespace FinGrow.Application.Features.Receipts.Cleanup;

using System.ComponentModel.DataAnnotations;

public sealed class ReceiptsCleanupOptions
{
    public const string SectionName = "ReceiptsCleanup";
    public const int DefaultGraceDays = 7;

    [Range(1, 365, ErrorMessage = "El periodo de gracia de los comprobantes sueltos (ReceiptsCleanup:GraceDays) tiene que estar entre 1 y 365 dias.")]
    public int GraceDays { get; init; } = DefaultGraceDays;

    public TimeSpan GracePeriod => TimeSpan.FromDays(GraceDays);
}
