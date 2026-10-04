namespace FinGrow.Application.Features.Notifications.ListNotifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.ValueObjects;
using FluentValidation;
using MediatR;

public sealed record ListNotificationsQuery(
    NotificationRecipient Recipient,
    bool UnreadOnly = false,
    int PageNumber = 1,
    int PageSize = ListNotificationsQuery.DefaultPageSize) : IRequest<Result<PagedResult<NotificationResponse>>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed class ListNotificationsQueryValidator : AbstractValidator<ListNotificationsQuery>
{
    public ListNotificationsQueryValidator()
    {
        RuleFor(query => query.Recipient).NotNull();

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La pagina tiene que ser 1 o mayor.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, ListNotificationsQuery.MaxPageSize)
            .WithMessage($"Se pueden pedir entre 1 y {ListNotificationsQuery.MaxPageSize} notificaciones por pagina.");
    }
}
