namespace FinGrow.Application.Features.Notifications.MarkNotificationAsRead;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.ValueObjects;
using MediatR;

public sealed record MarkNotificationAsReadCommand(NotificationRecipient Recipient, Guid NotificationId)
    : IRequest<Result<NotificationResponse>>;
