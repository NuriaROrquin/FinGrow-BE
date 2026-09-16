namespace FinGrow.Application.Features.Integrations.MercadoPago.StartMercadoPagoLink;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using MediatR;

internal sealed class StartMercadoPagoLinkHandler
    : IRequestHandler<StartMercadoPagoLinkCommand, Result<StartMercadoPagoLinkResponse>>
{
    private readonly LinkCodeIssuer _issuer;
    private readonly IMercadoPagoOAuthClient _oauth;

    public StartMercadoPagoLinkHandler(LinkCodeIssuer issuer, IMercadoPagoOAuthClient oauth)
    {
        _issuer = issuer;
        _oauth = oauth;
    }

    public async Task<Result<StartMercadoPagoLinkResponse>> Handle(
        StartMercadoPagoLinkCommand request,
        CancellationToken cancellationToken)
    {
        var issued = await _issuer.IssueForCurrentEmployeeAsync(IntegrationProvider.MercadoPago, cancellationToken);

        if (issued.IsFailure)
        {
            return Result.Failure<StartMercadoPagoLinkResponse>(issued.Error);
        }

        var url = _oauth.BuildAuthorizationUrl(issued.Value.Code);

        return Result.Success(new StartMercadoPagoLinkResponse(url, issued.Value.ExpiresAt));
    }
}
