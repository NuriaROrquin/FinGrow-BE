namespace FinGrow.Application.Features.Courses.ListCourses;

using FluentValidation;

public sealed class ListCoursesQueryValidator : AbstractValidator<ListCoursesQuery>
{
    public ListCoursesQueryValidator()
    {
        RuleFor(query => query.Level)
            .IsInEnum()
            .WithMessage("El nivel no es valido.");

        RuleFor(query => query.MaxDurationMinutes)
            .GreaterThan(0)
            .When(query => query.MaxDurationMinutes.HasValue)
            .WithMessage("La duracion maxima tiene que ser mayor a cero.");

        RuleFor(query => query.ProgressStatus)
            .IsInEnum()
            .WithMessage("El estado de progreso no es valido.");
    }
}
