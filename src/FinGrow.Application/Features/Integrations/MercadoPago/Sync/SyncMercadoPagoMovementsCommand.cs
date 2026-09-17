namespace FinGrow.Application.Features.Integrations.MercadoPago.Sync;

using FinGrow.Application.Common;
using MediatR;

public sealed record SyncMercadoPagoMovementsCommand(Guid EmployeeId) : IRequest<Result<MercadoPagoSyncSummary>>;

public sealed record SyncMyMercadoPagoCommand : IRequest<Result<MercadoPagoSyncSummary>>;

public sealed record MercadoPagoSyncSummary(int Imported, int AlreadyKnown, int Ignored, DateTimeOffset SyncedAt);
