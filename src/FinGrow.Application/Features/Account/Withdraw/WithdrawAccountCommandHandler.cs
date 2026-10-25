namespace FinGrow.Application.Features.Account.Withdraw;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Baja pedida por el propio empleado (HU-74, derecho de supresion de la Ley 25.326). Lo que la
/// empresa ya ve agregado no se toca: las fotos de metricas guardan conteos, no filas del empleado.
/// </summary>
internal sealed partial class WithdrawAccountCommandHandler : IRequestHandler<WithdrawAccountCommand, Result>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("Account.NoAutenticado", "Hay que iniciar sesión para darse de baja.");

    private static readonly Error ContrasenaIncorrecta =
        Error.Validation("Account.ContrasenaIncorrecta", "La contraseña no es correcta.");

    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IGmailOAuthClient _gmail;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmployeeDataEraser _eraser;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<WithdrawAccountCommandHandler> _logger;

    public WithdrawAccountCommandHandler(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IEmployeeIntegrationRepository integrations,
        IGmailOAuthClient gmail,
        IPasswordHasher passwordHasher,
        IEmployeeDataEraser eraser,
        IRefreshTokenRepository refreshTokens,
        IDateTimeProvider clock,
        ILogger<WithdrawAccountCommandHandler> logger)
    {
        _currentUser = currentUser;
        _employees = employees;
        _integrations = integrations;
        _gmail = gmail;
        _passwordHasher = passwordHasher;
        _eraser = eraser;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> Handle(WithdrawAccountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId
            || await _employees.GetByIdAsync(employeeId, cancellationToken) is not { IsWithdrawn: false } employee)
        {
            return Result.Failure(NoAutenticado);
        }

        if (!_passwordHasher.Verify(request.Password, employee.User.PasswordHash))
        {
            return Result.Failure(ContrasenaIncorrecta);
        }

        await RevokeGmailAccessAsync(employeeId, cancellationToken);

        var now = _clock.UtcNow;
        employee.Withdraw(now);
        await _eraser.EraseAndSaveAsync(employeeId, cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(employeeId, now, cancellationToken);

        LogWithdrawn(_logger, employeeId);

        return Result.Success();
    }

    // Las demas integraciones se cortan al borrar la fila; Gmail ademas guarda un permiso en la
    // cuenta de Google. Si Google no responde la baja sigue: el empleado puede quitarlo desde ahi.
    private async Task RevokeGmailAccessAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var gmail = await _integrations.FindByEmployeeAsync(employeeId, IntegrationProvider.Gmail, cancellationToken);

        if (gmail?.Grant is not { } grant)
        {
            return;
        }

        try
        {
            await _gmail.RevokeAsync(grant.RefreshToken ?? grant.AccessToken, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogRevokeFailed(_logger, exception, employeeId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "El empleado {EmployeeId} se dio de baja y se borraron sus datos financieros.")]
    private static partial void LogWithdrawn(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo revocar en Google el acceso a Gmail del empleado {EmployeeId} al darse de baja.")]
    private static partial void LogRevokeFailed(ILogger logger, Exception exception, Guid employeeId);
}
