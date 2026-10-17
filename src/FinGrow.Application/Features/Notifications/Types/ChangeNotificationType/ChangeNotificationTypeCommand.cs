namespace FinGrow.Application.Features.Notifications.Types.ChangeNotificationType;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using FluentValidation;
using MediatR;

public sealed record ChangeNotificationTypeCommand(Guid EmployeeId, NotificationType Type, bool IsEnabled)
    : IRequest<Result<NotificationTypeResponse>>;

public sealed class ChangeNotificationTypeCommandValidator : AbstractValidator<ChangeNotificationTypeCommand>
{
    public ChangeNotificationTypeCommandValidator()
    {
        RuleFor(command => command.Type)
            .IsInEnum()
            .WithMessage("El tipo de alerta no existe.");
    }
}
