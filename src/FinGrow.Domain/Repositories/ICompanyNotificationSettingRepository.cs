namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface ICompanyNotificationSettingRepository
{
    void Add(CompanyNotificationSetting setting);

    Task<IReadOnlyList<CompanyNotificationSetting>> ListByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
