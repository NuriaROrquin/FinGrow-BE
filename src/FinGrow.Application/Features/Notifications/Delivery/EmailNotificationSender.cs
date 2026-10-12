namespace FinGrow.Application.Features.Notifications.Delivery;

using System.Net;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal sealed class EmailNotificationSender(
    IEmployeeRepository employees,
    IOutboundEmailRepository outboundEmails,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification.RecipientType != NotificationRecipientType.Employee
            || notification.Type is not (NotificationType.BudgetWarning or NotificationType.BudgetExceeded))
        {
            return false;
        }

        var employee = await employees.GetByIdAsync(notification.RecipientId, cancellationToken);

        if (employee is null)
        {
            return false;
        }

        var textBody = $"{notification.Title}\n\n{notification.Body}";
        var htmlBody = $"<h1>{WebUtility.HtmlEncode(notification.Title)}</h1><p>{WebUtility.HtmlEncode(notification.Body)}</p>";

        outboundEmails.Add(OutboundEmail.Queue(
            employee.Email,
            notification.Title,
            htmlBody,
            textBody,
            clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
