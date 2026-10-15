namespace FinGrow.Application.Features.Integrations.Gmail.Sync;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using MediatR;

internal sealed class SyncMyGmailHandler : IRequestHandler<SyncMyGmailCommand, Result<GmailSyncSummary>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;

    public SyncMyGmailHandler(ICurrentUser currentUser, ISender sender)
    {
        _currentUser = currentUser;
        _sender = sender;
    }

    public Task<Result<GmailSyncSummary>> Handle(SyncMyGmailCommand request, CancellationToken cancellationToken) =>
        _currentUser.UserId is { } employeeId
            ? _sender.Send(new SyncGmailMessagesCommand(employeeId), cancellationToken)
            : Task.FromResult(Result.Failure<GmailSyncSummary>(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para sincronizar Gmail.")));
}
