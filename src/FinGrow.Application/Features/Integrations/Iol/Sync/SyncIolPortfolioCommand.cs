namespace FinGrow.Application.Features.Integrations.Iol.Sync;

using FinGrow.Application.Common;
using MediatR;

public sealed record SyncIolPortfolioCommand(Guid EmployeeId) : IRequest<Result<IolSyncSummary>>;

public sealed record SyncMyIolPortfolioCommand : IRequest<Result<IolSyncSummary>>;

public sealed record IolSyncSummary(
    int Imported,
    int Updated,
    int Unchanged,
    int Ignored,
    int NoLongerHeld,
    DateTimeOffset SyncedAt);
