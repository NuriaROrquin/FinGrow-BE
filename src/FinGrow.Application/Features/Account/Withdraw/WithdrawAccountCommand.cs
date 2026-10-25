namespace FinGrow.Application.Features.Account.Withdraw;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using FinGrow.Application.Common;
using MediatR;

public sealed record WithdrawAccountCommand(string Password) : IRequest<Result>
{
    [SuppressMessage("Performance", "CA1822", Justification = "El compilador de records exige que PrintMembers sea de instancia.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Password = ***");

        return true;
    }
}
