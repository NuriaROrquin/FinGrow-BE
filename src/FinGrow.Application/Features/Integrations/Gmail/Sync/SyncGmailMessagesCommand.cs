namespace FinGrow.Application.Features.Integrations.Gmail.Sync;

using FinGrow.Application.Common;
using MediatR;

public sealed record SyncGmailMessagesCommand(Guid EmployeeId) : IRequest<Result<GmailSyncSummary>>;

public sealed record SyncMyGmailCommand : IRequest<Result<GmailSyncSummary>>;

public sealed record GmailSyncSummary(int Imported, int AlreadyKnown, int NotBills, int Remaining, DateTimeOffset SyncedAt);
