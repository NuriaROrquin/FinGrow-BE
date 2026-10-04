namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

internal sealed class GetTwoFactorStatusQueryHandler : IRequestHandler<GetTwoFactorStatusQuery, Result<TwoFactorStatusResponse>>
{
    private readonly TwoFactorAccountFinder _accounts;

    public GetTwoFactorStatusQueryHandler(TwoFactorAccountFinder accounts) => _accounts = accounts;

    public async Task<Result<TwoFactorStatusResponse>> Handle(GetTwoFactorStatusQuery request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindCurrentAsync(cancellationToken);

        return account is null
            ? Result.Failure<TwoFactorStatusResponse>(TwoFactorErrors.NoAutenticado)
            : Result.Success(new TwoFactorStatusResponse(account.IsTwoFactorEnabled));
    }
}
