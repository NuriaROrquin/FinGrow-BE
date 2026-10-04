namespace FinGrow.Application.Features.Notifications;

using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public sealed record NotificationRequest(
    NotificationRecipient Recipient,
    NotificationType Type,
    string Title,
    string Body,
    string DeduplicationKey,
    TimeSpan DeduplicationWindow);
