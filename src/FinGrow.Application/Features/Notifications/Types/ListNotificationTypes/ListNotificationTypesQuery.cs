namespace FinGrow.Application.Features.Notifications.Types.ListNotificationTypes;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListNotificationTypesQuery(Guid EmployeeId)
    : IRequest<Result<IReadOnlyList<NotificationTypeResponse>>>;
