namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface INotificationChannelSettingRepository
{
    void Add(NotificationChannelSetting setting);

    Task<IReadOnlyList<NotificationChannelSetting>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
