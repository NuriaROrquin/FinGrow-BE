namespace FinGrow.Application.Features.Articles.ListArticles;

using FluentValidation;

public sealed class ListArticlesQueryValidator : AbstractValidator<ListArticlesQuery>
{
    public ListArticlesQueryValidator()
    {
        RuleFor(query => query.Category)
            .IsInEnum()
            .WithMessage("La categoria no es valida.");

        RuleFor(query => query.MaxReadingTimeMinutes)
            .GreaterThan(0)
            .When(query => query.MaxReadingTimeMinutes.HasValue)
            .WithMessage("El tiempo maximo de lectura tiene que ser mayor a cero.");
    }
}
