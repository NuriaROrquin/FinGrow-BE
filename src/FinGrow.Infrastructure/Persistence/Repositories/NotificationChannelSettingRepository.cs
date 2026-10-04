namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class NotificationChannelSettingRepository(FinGrowDbContext dbContext) : INotificationChannelSettingRepository
{
    public void Add(NotificationChannelSetting setting) => dbContext.NotificationChannelSettings.Add(setting);

    public async Task<IReadOnlyList<NotificationChannelSetting>> ListByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificationChannelSettings
            .Where(setting => setting.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
}
