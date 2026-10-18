namespace FinGrow.Application.Features.Notifications.Types.ListCompanyNotificationTypes;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListCompanyNotificationTypesQuery(Guid CompanyId)
    : IRequest<Result<IReadOnlyList<NotificationTypeResponse>>>;
