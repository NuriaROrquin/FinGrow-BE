namespace FinGrow.Application.Features.Integrations.GetIntegration;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetIntegrationHandler : IRequestHandler<GetIntegrationQuery, Result<IntegrationResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeIntegrationRepository _integrations;

    public GetIntegrationHandler(ICurrentUser currentUser, IEmployeeIntegrationRepository integrations)
    {
        _currentUser = currentUser;
        _integrations = integrations;
    }

    public async Task<Result<IntegrationResponse>> Handle(GetIntegrationQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<IntegrationResponse>(
                Error.Forbidden("Integrations.Unauthenticated", $"Hay que iniciar sesion para consultar {request.Provider}."));
        }

        var integration = await _integrations.FindByEmployeeAsync(employeeId, request.Provider, cancellationToken);

        return Result.Success(integration is null
            ? IntegrationResponse.NotLinked
            : new IntegrationResponse(true, integration.ExternalAccountId, integration.LinkedAt));
    }
}
