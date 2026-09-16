namespace FinGrow.Application.Features.Integrations.WhatsApp.GetWhatsAppIntegration;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetWhatsAppIntegrationHandler
    : IRequestHandler<GetWhatsAppIntegrationQuery, Result<WhatsAppIntegrationResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeIntegrationRepository _integrations;

    public GetWhatsAppIntegrationHandler(ICurrentUser currentUser, IEmployeeIntegrationRepository integrations)
    {
        _currentUser = currentUser;
        _integrations = integrations;
    }

    public async Task<Result<WhatsAppIntegrationResponse>> Handle(
        GetWhatsAppIntegrationQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<WhatsAppIntegrationResponse>(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para consultar WhatsApp."));
        }

        var integration = await _integrations.FindByEmployeeAsync(
            employeeId, IntegrationProvider.WhatsApp, cancellationToken);

        return Result.Success(integration is null
            ? WhatsAppIntegrationResponse.NotLinked
            : new WhatsAppIntegrationResponse(true, integration.ExternalAccountId, integration.LinkedAt));
    }
}
