namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;

public sealed class EmployeeAchievement : AggregateRoot
{
    private EmployeeAchievement()
    {
    }

    private EmployeeAchievement(
        Guid id,
        Guid employeeId,
        AchievementCode code,
        DateTimeOffset achievedAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Code = code;
        AchievedAt = achievedAt;
        CreatedAt = createdAt;
    }

    public Guid EmployeeId { get; private set; }

    public AchievementCode Code { get; private set; }

    public DateTimeOffset AchievedAt { get; private set; }

    /// <summary>
    /// Cuando el front lo celebro. Mientras sea null, el front lo muestra la proxima vez que el
    /// empleado abra la app: asi no se pierde un logro ganado desde Telegram o WhatsApp.
    /// </summary>
    public DateTimeOffset? CelebratedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsCelebrated => CelebratedAt is not null;

    /// <summary>
    /// Lo acaba de ganar: se avisa en el momento y queda pendiente de celebrar.
    /// </summary>
    public static EmployeeAchievement Unlock(
        Guid employeeId,
        AchievementCode code,
        DateTimeOffset achievedAt,
        DateTimeOffset recordedAt)
    {
        var achievement = Create(employeeId, code, achievedAt, recordedAt);
        achievement.Raise(new AchievementUnlocked(achievement.Id, employeeId, code, recordedAt));

        return achievement;
    }

    /// <summary>
    /// Lo cumplia antes de que existieran los logros. No se avisa uno por uno: queda pendiente de
    /// celebrar y el front los muestra juntos, con todas las medallas, al abrir la app.
    /// </summary>
    public static EmployeeAchievement Backfill(
        Guid employeeId,
        AchievementCode code,
        DateTimeOffset achievedAt,
        DateTimeOffset recordedAt) =>
        Create(employeeId, code, achievedAt, recordedAt);

    /// <summary>
    /// Lo recupera el job porque fallo el otorgamiento en el momento. Celebrarlo dias
    /// despues confundiria al empleado, asi que nace celebrado.
    /// </summary>
    public static EmployeeAchievement Recover(
        Guid employeeId,
        AchievementCode code,
        DateTimeOffset achievedAt,
        DateTimeOffset recordedAt)
    {
        var achievement = Create(employeeId, code, achievedAt, recordedAt);
        achievement.CelebratedAt = recordedAt;

        return achievement;
    }

    public void MarkCelebrated(DateTimeOffset celebratedAt)
    {
        if (IsCelebrated)
        {
            return;
        }

        if (celebratedAt < CreatedAt)
        {
            throw new DomainException("Un logro no puede celebrarse antes de haberse otorgado.");
        }

        CelebratedAt = celebratedAt;
    }

    private static EmployeeAchievement Create(
        Guid employeeId,
        AchievementCode code,
        DateTimeOffset achievedAt,
        DateTimeOffset recordedAt)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Un logro siempre pertenece a un empleado.");
        }

        if (!Enum.IsDefined(code))
        {
            throw new DomainException($"El logro '{code}' no existe en el catalogo.");
        }

        if (achievedAt > recordedAt)
        {
            throw new DomainException("Un logro no puede registrarse antes de haberse cumplido.");
        }

        return new EmployeeAchievement(Guid.CreateVersion7(), employeeId, code, achievedAt, recordedAt);
    }
}
