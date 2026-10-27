namespace FinGrow.Api.UnitTests;

using FinGrow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

internal static class TestSessions
{
    /// <summary>
    /// Los tests de la API no tienen base: el chequeo de sesion abierta (HU-73) daria por cerrada la
    /// sesion de cualquier token de prueba. Lo cubren los tests de sesiones, con su propio fake.
    /// </summary>
    public static IServiceCollection TreatEverySessionAsOpen(this IServiceCollection services) =>
        services.AddSingleton<IOpenSessionChecker, AlwaysOpenSessionChecker>();

    private sealed class AlwaysOpenSessionChecker : IOpenSessionChecker
    {
        public Task<bool> IsOpenAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
