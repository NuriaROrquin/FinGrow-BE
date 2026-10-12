namespace FinGrow.Application.Features.Integrations.Iol.Sync;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class SyncIolPortfolioHandler : IRequestHandler<SyncIolPortfolioCommand, Result<IolSyncSummary>>
{
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IolSynchronizer _synchronizer;

    public SyncIolPortfolioHandler(IEmployeeIntegrationRepository integrations, IolSynchronizer synchronizer)
    {
        _integrations = integrations;
        _synchronizer = synchronizer;
    }

    public async Task<Result<IolSyncSummary>> Handle(SyncIolPortfolioCommand request, CancellationToken cancellationToken)
    {
        var integration = await _integrations.FindByEmployeeAsync(request.EmployeeId, IntegrationProvider.Iol, cancellationToken);

        return integration is null
            ? Result.Failure<IolSyncSummary>(IolErrors.NotLinked)
            : await _synchronizer.SyncAsync(integration, cancellationToken);
    }
}
