namespace FinGrow.Application.Features.Notifications.Channels.ChangeNotificationChannel;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using FluentValidation;
using MediatR;

public sealed record ChangeNotificationChannelCommand(Guid EmployeeId, NotificationChannel Channel, bool IsEnabled)
    : IRequest<Result<NotificationChannelResponse>>;

public sealed class ChangeNotificationChannelCommandValidator : AbstractValidator<ChangeNotificationChannelCommand>
{
    public ChangeNotificationChannelCommandValidator()
    {
        RuleFor(command => command.Channel)
            .Cascade(CascadeMode.Stop)
            .IsInEnum()
            .WithMessage("El canal no existe.")
            .Must(channel => channel.IsConfigurable())
            .WithMessage("Las notificaciones de la app no se pueden desactivar: son el historial de todas las alertas.");
    }
}
