namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface INotificationTypeSettingRepository
{
    void Add(NotificationTypeSetting setting);

    Task<IReadOnlyList<NotificationTypeSetting>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
