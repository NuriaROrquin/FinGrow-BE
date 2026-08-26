namespace FinGrow.Application.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
