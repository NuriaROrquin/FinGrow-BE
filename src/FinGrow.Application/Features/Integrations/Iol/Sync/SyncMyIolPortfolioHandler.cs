namespace FinGrow.Application.Features.Integrations.Iol.Sync;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using MediatR;

internal sealed class SyncMyIolPortfolioHandler : IRequestHandler<SyncMyIolPortfolioCommand, Result<IolSyncSummary>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;

    public SyncMyIolPortfolioHandler(ICurrentUser currentUser, ISender sender)
    {
        _currentUser = currentUser;
        _sender = sender;
    }

    public Task<Result<IolSyncSummary>> Handle(SyncMyIolPortfolioCommand request, CancellationToken cancellationToken) =>
        _currentUser.UserId is { } employeeId
            ? _sender.Send(new SyncIolPortfolioCommand(employeeId), cancellationToken)
            : Task.FromResult(Result.Failure<IolSyncSummary>(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para sincronizar IOL.")));
}
