namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IMonthlyReportDeliveryRepository
{
    void Add(MonthlyReportDelivery delivery);

    Task<MonthlyReportDelivery?> FindAsync(
        Guid companyId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default);
}
