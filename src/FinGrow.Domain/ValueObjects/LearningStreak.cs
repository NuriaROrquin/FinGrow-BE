namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;

public sealed record LearningStreak(int Weeks, bool StudiedThisWeek)
{
    private const int DaysInWeek = 7;

    // La racha se mide en semanas (lunes a domingo, hora de Argentina) y no en dias: quien estudia
    // en ratos libres no la pierde por un dia sin lecciones. Una semana sin lecciones la corta.
    // La semana en curso todavia puede completarse, asi que si aun no estudio no cuenta como corte:
    // la racha sigue viva hasta que termine.
    public static LearningStreak From(IEnumerable<DateTimeOffset> completedAt, DateOnly today)
    {
        var studiedWeeks = completedAt
            .Select(instant => StartOfWeek(ArgentinaTime.DateOf(instant)))
            .ToHashSet();

        var currentWeek = StartOfWeek(today);
        var studiedThisWeek = studiedWeeks.Contains(currentWeek);

        var week = studiedThisWeek ? currentWeek : currentWeek.AddDays(-DaysInWeek);
        var weeks = 0;

        while (studiedWeeks.Contains(week))
        {
            weeks++;
            week = week.AddDays(-DaysInWeek);
        }

        return new LearningStreak(weeks, studiedThisWeek);
    }

    private static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + DaysInWeek - 1) % DaysInWeek));
}
