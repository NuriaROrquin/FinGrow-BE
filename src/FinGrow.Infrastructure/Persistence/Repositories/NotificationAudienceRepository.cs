namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class NotificationAudienceRepository(FinGrowDbContext dbContext) : INotificationAudienceRepository
{
    public async Task<IReadOnlyList<Guid>> ListEmployeesWithInvestmentsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Investments
            .Select(investment => investment.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListActiveEmployeeIdsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Employees
            .Where(employee => employee.User.IsActive)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);
}
