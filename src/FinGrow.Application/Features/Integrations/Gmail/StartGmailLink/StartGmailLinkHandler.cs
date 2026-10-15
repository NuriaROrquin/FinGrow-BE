namespace FinGrow.Application.Features.Integrations.Gmail.StartGmailLink;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using MediatR;

internal sealed class StartGmailLinkHandler : IRequestHandler<StartGmailLinkCommand, Result<StartGmailLinkResponse>>
{
    private readonly LinkCodeIssuer _issuer;
    private readonly IGmailOAuthClient _oauth;

    public StartGmailLinkHandler(LinkCodeIssuer issuer, IGmailOAuthClient oauth)
    {
        _issuer = issuer;
        _oauth = oauth;
    }

    public async Task<Result<StartGmailLinkResponse>> Handle(
        StartGmailLinkCommand request,
        CancellationToken cancellationToken)
    {
        var issued = await _issuer.IssueForCurrentEmployeeAsync(IntegrationProvider.Gmail, cancellationToken);

        if (issued.IsFailure)
        {
            return Result.Failure<StartGmailLinkResponse>(issued.Error);
        }

        var url = _oauth.BuildAuthorizationUrl(issued.Value.Code);

        return Result.Success(new StartGmailLinkResponse(url, issued.Value.ExpiresAt));
    }
}
