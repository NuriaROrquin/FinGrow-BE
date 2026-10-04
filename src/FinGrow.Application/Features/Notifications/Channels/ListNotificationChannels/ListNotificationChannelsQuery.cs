namespace FinGrow.Application.Features.Notifications.Channels.ListNotificationChannels;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListNotificationChannelsQuery(Guid EmployeeId)
    : IRequest<Result<IReadOnlyList<NotificationChannelResponse>>>;
