namespace FinGrow.Application.Features.Session.CloseSession;

using FinGrow.Application.Common;
using MediatR;

public sealed record CloseSessionCommand(Guid SessionId) : IRequest<Result>;
