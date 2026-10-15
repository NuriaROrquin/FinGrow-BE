namespace FinGrow.Application.Features.Integrations.Gmail.Sync;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class SyncGmailMessagesHandler : IRequestHandler<SyncGmailMessagesCommand, Result<GmailSyncSummary>>
{
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly GmailSynchronizer _synchronizer;

    public SyncGmailMessagesHandler(IEmployeeIntegrationRepository integrations, GmailSynchronizer synchronizer)
    {
        _integrations = integrations;
        _synchronizer = synchronizer;
    }

    public async Task<Result<GmailSyncSummary>> Handle(SyncGmailMessagesCommand request, CancellationToken cancellationToken)
    {
        var integration = await _integrations.FindByEmployeeAsync(request.EmployeeId, IntegrationProvider.Gmail, cancellationToken);

        return integration is null
            ? Result.Failure<GmailSyncSummary>(GmailSynchronizer.NotLinked)
            : await _synchronizer.SyncAsync(integration, cancellationToken);
    }
}
