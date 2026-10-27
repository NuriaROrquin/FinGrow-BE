namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class MonthlyReportDeliveryRepository(FinGrowDbContext dbContext) : IMonthlyReportDeliveryRepository
{
    public void Add(MonthlyReportDelivery delivery) => dbContext.MonthlyReportDeliveries.Add(delivery);

    public Task<MonthlyReportDelivery?> FindAsync(
        Guid companyId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        dbContext.MonthlyReportDeliveries.FirstOrDefaultAsync(
            delivery => delivery.CompanyId == companyId && delivery.PeriodStart == periodStart,
            cancellationToken);
}
