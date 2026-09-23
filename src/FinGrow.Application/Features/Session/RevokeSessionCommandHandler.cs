namespace FinGrow.Application.Features.Session;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public RevokeSessionCommandHandler(IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var token = await _refreshTokens.FindByHashAsync(
            Domain.Entities.RefreshToken.Hash(request.RefreshToken),
            cancellationToken);

        if (token is null)
        {
            return;
        }

        token.Revoke(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
