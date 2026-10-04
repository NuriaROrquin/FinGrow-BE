namespace FinGrow.Application.Features.Courses.RateCourse;

using Domain.Entities;
using FluentValidation;

public sealed class RateCourseCommandValidator : AbstractValidator<RateCourseCommand>
{
    public RateCourseCommandValidator()
    {
        RuleFor(command => command.Score)
            .InclusiveBetween(CourseRating.MinScore, CourseRating.MaxScore)
            .WithMessage($"La calificacion tiene que estar entre {CourseRating.MinScore} y {CourseRating.MaxScore}.");
    }
}
