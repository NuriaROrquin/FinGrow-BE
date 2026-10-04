namespace FinGrow.Application.Features.Notifications.CountUnreadNotifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.ValueObjects;
using MediatR;

public sealed record CountUnreadNotificationsQuery(NotificationRecipient Recipient)
    : IRequest<Result<UnreadNotificationsResponse>>;
