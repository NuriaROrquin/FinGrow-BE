namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class CompanyNotificationSettingRepository(FinGrowDbContext dbContext) : ICompanyNotificationSettingRepository
{
    public void Add(CompanyNotificationSetting setting) => dbContext.CompanyNotificationSettings.Add(setting);

    public async Task<IReadOnlyList<CompanyNotificationSetting>> ListByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await dbContext.CompanyNotificationSettings
            .Where(setting => setting.CompanyId == companyId)
            .ToListAsync(cancellationToken);
}
