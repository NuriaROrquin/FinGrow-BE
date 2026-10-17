namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class NotificationTypeSettingRepository(FinGrowDbContext dbContext) : INotificationTypeSettingRepository
{
    public void Add(NotificationTypeSetting setting) => dbContext.NotificationTypeSettings.Add(setting);

    public async Task<IReadOnlyList<NotificationTypeSetting>> ListByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificationTypeSettings
            .Where(setting => setting.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
}
