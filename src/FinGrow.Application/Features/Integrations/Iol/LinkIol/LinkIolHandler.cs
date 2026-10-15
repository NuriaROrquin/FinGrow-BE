namespace FinGrow.Application.Features.Integrations.Iol.LinkIol;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.GetIntegration;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class LinkIolHandler : IRequestHandler<LinkIolCommand, Result<IntegrationResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIolAuthClient _iol;
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<LinkIolHandler> _logger;

    public LinkIolHandler(
        ICurrentUser currentUser,
        IIolAuthClient iol,
        IEmployeeIntegrationRepository integrations,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<LinkIolHandler> logger)
    {
        _currentUser = currentUser;
        _iol = iol;
        _integrations = integrations;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<IntegrationResponse>> Handle(LinkIolCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<IntegrationResponse>(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para vincular IOL."));
        }

        // IOL no devuelve un id de cuenta al autenticar: el usuario es lo que identifica la cuenta externa.
        var account = request.Username.Trim().ToLowerInvariant();
        var owner = await _integrations.FindByExternalAccountAsync(IntegrationProvider.Iol, account, cancellationToken);

        if (owner is not null && owner.EmployeeId != employeeId)
        {
            return Result.Failure<IntegrationResponse>(IolErrors.AlreadyLinkedToAnotherEmployee);
        }

        IolTokens tokens;

        try
        {
            tokens = await _iol.LoginAsync(request.Username.Trim(), request.Password, cancellationToken);
        }
        catch (HttpRequestException exception) when (IolErrors.IsRejection(exception))
        {
            LogRejected(_logger, employeeId);

            return Result.Failure<IntegrationResponse>(IolErrors.InvalidCredentials);
        }
        catch (HttpRequestException exception)
        {
            LogUnavailable(_logger, exception, employeeId);

            return Result.Failure<IntegrationResponse>(IolErrors.Unavailable);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogUnavailable(_logger, exception, employeeId);

            return Result.Failure<IntegrationResponse>(IolErrors.Unavailable);
        }

        var now = _clock.UtcNow;
        var integration = await _integrations.FindByEmployeeAsync(employeeId, IntegrationProvider.Iol, cancellationToken);

        if (integration is null)
        {
            integration = EmployeeIntegration.Create(employeeId, IntegrationProvider.Iol, account, now);
            _integrations.Add(integration);
        }
        else
        {
            integration.Relink(account, now);
        }

        integration.Authorize(OAuthGrant.From(tokens.AccessToken, tokens.RefreshToken, now.Add(tokens.ExpiresIn)), now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogLinked(_logger, employeeId);

        return Result.Success(IntegrationResponse.From(integration));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "IOL vinculado al empleado {EmployeeId}.")]
    private static partial void LogLinked(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "IOL rechazo las credenciales del empleado {EmployeeId} al vincular.")]
    private static partial void LogRejected(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "IOL no respondio al vincular al empleado {EmployeeId}.")]
    private static partial void LogUnavailable(ILogger logger, Exception exception, Guid employeeId);
}
