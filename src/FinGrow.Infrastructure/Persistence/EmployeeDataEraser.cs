namespace FinGrow.Infrastructure.Persistence;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class EmployeeDataEraser : IEmployeeDataEraser
{
    private readonly FinGrowDbContext _dbContext;

    public EmployeeDataEraser(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public async Task EraseAndSaveAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Las hijas (aportes, valuaciones, limites) se van por cascada. Los comprobantes tienen
        // ON DELETE SET NULL sobre el movimiento: quedan sueltos para receipts-cleanup.
        await _dbContext.Transactions.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Budgets.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Goals.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Investments.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.EmployeeIntegrations.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.IntegrationLinkCodes.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.FinancialHealthScores.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.EmployeeAchievements.Where(row => row.EmployeeId == employeeId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Notifications
            .Where(row => row.RecipientType == NotificationRecipientType.Employee && row.RecipientId == employeeId)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
