namespace FinGrow.Infrastructure.Services;

using FinGrow.Application.Interfaces;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
