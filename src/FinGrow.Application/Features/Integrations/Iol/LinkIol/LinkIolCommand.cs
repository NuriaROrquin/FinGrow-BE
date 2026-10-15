namespace FinGrow.Application.Features.Integrations.Iol.LinkIol;

using System.Text;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.GetIntegration;
using MediatR;

public sealed record LinkIolCommand(string Username, string Password) : IRequest<Result<IntegrationResponse>>
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Username = ").Append(Username).Append(", Password = ***");

        return true;
    }
}
