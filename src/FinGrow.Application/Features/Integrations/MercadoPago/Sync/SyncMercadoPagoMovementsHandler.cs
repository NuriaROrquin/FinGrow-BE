namespace FinGrow.Application.Features.Integrations.MercadoPago.Sync;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class SyncMercadoPagoMovementsHandler : IRequestHandler<SyncMercadoPagoMovementsCommand, Result<MercadoPagoSyncSummary>>
{
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly MercadoPagoSynchronizer _synchronizer;

    public SyncMercadoPagoMovementsHandler(IEmployeeIntegrationRepository integrations, MercadoPagoSynchronizer synchronizer)
    {
        _integrations = integrations;
        _synchronizer = synchronizer;
    }

    public async Task<Result<MercadoPagoSyncSummary>> Handle(SyncMercadoPagoMovementsCommand request, CancellationToken cancellationToken)
    {
        var integration = await _integrations.FindByEmployeeAsync(request.EmployeeId, IntegrationProvider.MercadoPago, cancellationToken);

        return integration is null
            ? Result.Failure<MercadoPagoSyncSummary>(MercadoPagoSynchronizer.NotAuthorized)
            : await _synchronizer.SyncAsync(integration, cancellationToken);
    }
}
