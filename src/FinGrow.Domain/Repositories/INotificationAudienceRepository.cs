namespace FinGrow.Domain.Repositories;

/// <summary>
/// A quienes les corresponde una alerta que no nace de un movimiento propio: los que tienen inversiones
/// para evaluar y todos los que pueden entrar a la app para enterarse del contenido nuevo.
/// </summary>
public interface INotificationAudienceRepository
{
    Task<IReadOnlyList<Guid>> ListEmployeesWithInvestmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListActiveEmployeeIdsAsync(CancellationToken cancellationToken = default);
}
