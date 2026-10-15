namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

/// <summary>
/// Un logro del catalogo. El catalogo es fijo y lo define <see cref="AchievementCatalog"/>: la
/// migracion lo carga en la base y nadie lo edita desde la aplicacion. La clave es el codigo y no un
/// Guid porque es lo que comparten la base, la API y las medallas del front.
/// </summary>
public sealed class Achievement
{
    public const int MaxNameLength = 80;
    public const int MaxDescriptionLength = 200;
    public const int MaxConditionLength = 500;

    private Achievement()
    {
    }

    private Achievement(
        AchievementCode code,
        string name,
        string description,
        string condition,
        AchievementEvaluation evaluation,
        bool isMajor)
    {
        Code = code;
        Name = name;
        Description = description;
        Condition = condition;
        Evaluation = evaluation;
        IsMajor = isMajor;
    }

    public AchievementCode Code { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Condition { get; private set; } = string.Empty;

    public AchievementEvaluation Evaluation { get; private set; }

    public bool IsMajor { get; private set; }

    internal static Achievement Define(
        AchievementCode code,
        string name,
        string description,
        string condition,
        AchievementEvaluation evaluation,
        bool isMajor = false) =>
        new(
            code,
            RequiredText.Ensure(name, MaxNameLength, "El nombre del logro"),
            RequiredText.Ensure(description, MaxDescriptionLength, "La descripcion del logro"),
            RequiredText.Ensure(condition, MaxConditionLength, "La condicion del logro"),
            evaluation,
            isMajor);
}
