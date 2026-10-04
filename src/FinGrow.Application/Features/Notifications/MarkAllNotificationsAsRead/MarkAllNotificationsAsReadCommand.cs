namespace FinGrow.Application.Features.Notifications.MarkAllNotificationsAsRead;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.ValueObjects;
using MediatR;

public sealed record MarkAllNotificationsAsReadCommand(NotificationRecipient Recipient)
    : IRequest<Result<MarkedNotificationsResponse>>;
