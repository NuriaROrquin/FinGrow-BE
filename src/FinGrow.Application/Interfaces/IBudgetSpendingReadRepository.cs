namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface IBudgetSpendingReadRepository
{
    /// <summary>
    /// Gasto confirmado del periodo del presupuesto, por categoria y en la moneda del presupuesto.
    /// Las categorias sin gasto no aparecen en el diccionario.
    /// </summary>
    Task<IReadOnlyDictionary<ExpenseCategory, decimal>> GetSpentByCategoryAsync(
        Budget budget,
        CancellationToken cancellationToken = default);
}
