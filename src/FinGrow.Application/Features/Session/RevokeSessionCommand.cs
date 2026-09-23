namespace FinGrow.Application.Features.Session;

using MediatR;

public sealed record RevokeSessionCommand(string RefreshToken) : IRequest;
