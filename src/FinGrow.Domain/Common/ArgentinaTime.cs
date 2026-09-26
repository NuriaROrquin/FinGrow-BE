namespace FinGrow.Domain.Common;

/// <summary>
/// El dia calendario lo decide la hora de Argentina, no UTC: entre las 21 y la medianoche UTC ya
/// es el dia siguiente, y una meta apareceria vencida o un aporte de hoy "en el futuro".
/// Argentina no tiene horario de verano, por eso alcanza con un offset fijo.
/// </summary>
public static class ArgentinaTime
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(-3);

    public static DateOnly DateOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.ToOffset(UtcOffset).DateTime);
}
