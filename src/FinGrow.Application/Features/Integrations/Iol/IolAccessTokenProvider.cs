namespace FinGrow.Application.Features.Integrations.Iol;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed partial class IolAccessTokenProvider
{
    internal static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(2);

    private readonly IIolAuthClient _iol;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<IolAccessTokenProvider> _logger;

    public IolAccessTokenProvider(
        IIolAuthClient iol,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<IolAccessTokenProvider> logger)
    {
        _iol = iol;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<string>> GetAccessTokenAsync(EmployeeIntegration integration, CancellationToken cancellationToken)
    {
        if (integration.Provider != IntegrationProvider.Iol || integration.Grant is not { } grant)
        {
            return Result.Failure<string>(IolErrors.NotLinked);
        }

        if (integration.Status == IntegrationStatus.Error)
        {
            return Result.Failure<string>(IolErrors.GrantExpired);
        }

        var now = _clock.UtcNow;

        if (!grant.ExpiresWithin(RefreshWindow, now))
        {
            return Result.Success(grant.AccessToken);
        }

        if (!grant.CanRefresh)
        {
            return await FailAsync(integration, now, cancellationToken);
        }

        try
        {
            var tokens = await _iol.RefreshAsync(grant.RefreshToken!, cancellationToken);
            var renewed = OAuthGrant.From(tokens.AccessToken, tokens.RefreshToken, now.Add(tokens.ExpiresIn));

            integration.Authorize(renewed, now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(renewed.AccessToken);
        }
        catch (HttpRequestException exception) when (IolErrors.IsRejection(exception))
        {
            LogRefreshRejected(_logger, integration.EmployeeId);

            return await FailAsync(integration, now, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogRefreshUnavailable(_logger, exception, integration.EmployeeId);

            return StillValidOrUnavailable(grant, now);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogRefreshUnavailable(_logger, exception, integration.EmployeeId);

            return StillValidOrUnavailable(grant, now);
        }
    }

    private static Result<string> StillValidOrUnavailable(OAuthGrant grant, DateTimeOffset now) =>
        grant.ExpiresWithin(TimeSpan.Zero, now)
            ? Result.Failure<string>(IolErrors.Unavailable)
            : Result.Success(grant.AccessToken);

    private async Task<Result<string>> FailAsync(EmployeeIntegration integration, DateTimeOffset now, CancellationToken cancellationToken)
    {
        integration.MarkFailed(IolErrors.GrantExpired.Description, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Failure<string>(IolErrors.GrantExpired);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "IOL rechazo el refresh token del empleado {EmployeeId}: la integracion queda en error hasta que la vuelva a vincular.")]
    private static partial void LogRefreshRejected(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "IOL no respondio al renovar el token del empleado {EmployeeId}.")]
    private static partial void LogRefreshUnavailable(ILogger logger, Exception exception, Guid employeeId);
}
