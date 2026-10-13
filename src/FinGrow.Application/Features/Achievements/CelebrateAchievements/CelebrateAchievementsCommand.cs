namespace FinGrow.Application.Features.Achievements.CelebrateAchievements;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using FluentValidation;
using MediatR;

/// <summary>
/// Recibe los codigos que el front mostro y no "todos los pendientes": un logro que llega mientras
/// se muestra el toast no puede quedar marcado como celebrado sin que nadie lo haya visto.
/// </summary>
public sealed record CelebrateAchievementsCommand(Guid EmployeeId, IReadOnlyCollection<AchievementCode> Codes)
    : IRequest<Result<CelebratedAchievementsResponse>>;

public sealed record CelebratedAchievementsResponse(int Celebrated);

public sealed class CelebrateAchievementsCommandValidator : AbstractValidator<CelebrateAchievementsCommand>
{
    public CelebrateAchievementsCommandValidator()
    {
        RuleFor(command => command.Codes)
            .NotEmpty()
            .WithMessage("Indica que logros se celebraron.");

        RuleForEach(command => command.Codes)
            .IsInEnum()
            .WithMessage("El logro indicado no existe en el catalogo.");
    }
}
