namespace FinGrow.Application.Features.Integrations.MercadoPago.Sync;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using MediatR;

internal sealed class SyncMyMercadoPagoHandler : IRequestHandler<SyncMyMercadoPagoCommand, Result<MercadoPagoSyncSummary>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;

    public SyncMyMercadoPagoHandler(ICurrentUser currentUser, ISender sender)
    {
        _currentUser = currentUser;
        _sender = sender;
    }

    public Task<Result<MercadoPagoSyncSummary>> Handle(SyncMyMercadoPagoCommand request, CancellationToken cancellationToken) =>
        _currentUser.UserId is { } employeeId
            ? _sender.Send(new SyncMercadoPagoMovementsCommand(employeeId), cancellationToken)
            : Task.FromResult(Result.Failure<MercadoPagoSyncSummary>(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para sincronizar Mercado Pago.")));
}
