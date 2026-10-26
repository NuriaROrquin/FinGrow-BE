namespace FinGrow.Application.Interfaces;

/// <summary>
/// Si la sesion de un access token sigue abierta (HU-73): lo consulta la autenticacion en cada pedido,
/// asi cerrar una sesion, cambiar o restablecer la contrasena o darse de baja corta el acceso en el
/// proximo pedido en vez de cuando vence el access token.
/// </summary>
public interface IOpenSessionChecker
{
    Task<bool> IsOpenAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
}
