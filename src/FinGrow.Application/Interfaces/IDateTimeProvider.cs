namespace FinGrow.Application.Interfaces;

using Domain.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    /// <summary>El dia de hoy en Argentina.</summary>
    DateOnly Today => ArgentinaTime.DateOf(UtcNow);
}
