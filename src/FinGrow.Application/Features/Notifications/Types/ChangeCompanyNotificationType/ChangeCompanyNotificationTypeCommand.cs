namespace FinGrow.Application.Features.Notifications.Types.ChangeCompanyNotificationType;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FluentValidation;
using MediatR;

public sealed record ChangeCompanyNotificationTypeCommand(
    Guid CompanyId,
    NotificationType Type,
    bool IsEnabled,
    NotificationCadence Cadence,
    decimal? Threshold) : IRequest<Result<NotificationTypeResponse>>;

public sealed class ChangeCompanyNotificationTypeCommandValidator : AbstractValidator<ChangeCompanyNotificationTypeCommand>
{
    public ChangeCompanyNotificationTypeCommandValidator()
    {
        RuleFor(command => command.Type)
            .Must(CompanyNotificationSetting.IsCompanyType)
            .WithMessage("El tipo de alerta no se puede configurar para una empresa.");
        RuleFor(command => command.Cadence).IsInEnum();
        RuleFor(command => command)
            .Must(command => command.Cadence == NotificationCadence.Monthly)
            .WithMessage("Las alertas empresariales solo se pueden recibir mensualmente.");
        RuleFor(command => command)
            .Must(command => command.Type is not (NotificationType.FinancialWellbeingLow or NotificationType.ParticipationLow)
                || command.Threshold is null or >= 1m and <= 100m)
            .WithMessage("El umbral tiene que estar entre 1 y 100.");
        RuleFor(command => command)
            .Must(command => command.Type is NotificationType.FinancialWellbeingLow or NotificationType.ParticipationLow
                || command.Threshold is null)
            .WithMessage("Solo bienestar y participación admiten un umbral.");
    }
}
