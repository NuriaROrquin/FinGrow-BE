namespace FinGrow.Application.Features.PasswordReset.ResetPassword;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using FinGrow.Application.Common;
using MediatR;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>
{
    // Como LinkIolCommand: que un log del comando no imprima la contraseña ni el token.
    [SuppressMessage("Performance", "CA1822", Justification = "El compilador exige esta firma de instancia para reemplazar el ToString del record.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Token = ***, NewPassword = ***");

        return true;
    }
}
