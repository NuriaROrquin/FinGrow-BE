namespace FinGrow.Application.Features.Notifications.Types;

using FinGrow.Domain.Enums;

internal static class CompanyNotificationTypes
{
    public static readonly NotificationType[] All =
    {
        NotificationType.FinancialWellbeingLow,
        NotificationType.ParticipationLow,
        NotificationType.EmployeeAdded,
        NotificationType.EmployeeRemoved,
    };
}
