namespace FinGrow.Application.Features.Integrations.GenerateLinkCode;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using MediatR;

internal sealed class GenerateLinkCodeHandler : IRequestHandler<GenerateLinkCodeCommand, Result<LinkCodeResponse>>
{
    private readonly LinkCodeIssuer _issuer;

    public GenerateLinkCodeHandler(LinkCodeIssuer issuer) => _issuer = issuer;

    public async Task<Result<LinkCodeResponse>> Handle(GenerateLinkCodeCommand request, CancellationToken cancellationToken)
    {
        var issued = await _issuer.IssueForCurrentEmployeeAsync(request.Provider, cancellationToken);

        return issued.IsSuccess
            ? Result.Success(new LinkCodeResponse(issued.Value.Code, issued.Value.ExpiresAt))
            : Result.Failure<LinkCodeResponse>(issued.Error);
    }
}
