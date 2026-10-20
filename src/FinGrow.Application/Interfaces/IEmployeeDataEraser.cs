namespace FinGrow.Application.Interfaces;

/// <summary>
/// Borra los datos financieros de un empleado que se da de baja (HU-74): movimientos, presupuestos,
/// metas, inversiones, integraciones, puntajes de salud financiera, logros y notificaciones. Sus
/// comprobantes no se borran: al irse los movimientos quedan sueltos y los borra <c>receipts-cleanup</c>.
/// </summary>
public interface IEmployeeDataEraser
{
    /// <summary>
    /// Borra los datos y guarda los cambios pendientes de la unidad de trabajo (la baja del empleado)
    /// en una misma transaccion de la base: o queda dado de baja y sin datos, o no cambia nada.
    /// </summary>
    Task EraseAndSaveAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
