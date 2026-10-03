namespace FinGrow.Application.Interfaces;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetHistory;

public interface ITransactionExcelExporter
{
    Task<Result<byte[]>> ExportAsync(
        Guid employeeId,
        TransactionFilters filters,
        CancellationToken cancellationToken = default);
}