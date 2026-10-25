namespace FinGrow.Application.Features.PasswordReset.RequestPasswordReset;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

/// <summary>
/// HU-72: responde éxito exista o no la cuenta, para no revelar qué emails están registrados. Las
/// salidas tempranas no son errores: son los casos en que no se manda nada.
/// </summary>
internal sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
{
    // Frena a quien aprieta el botón varias veces o usa el formulario para llenarle la casilla a otro.
    internal static readonly TimeSpan ResendInterval = TimeSpan.FromMinutes(1);

    private readonly IUserRepository _users;
    private readonly SessionAccountResolver _accounts;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IOutboundEmailRepository _outboundEmails;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly PasswordResetOptions _options;

    public RequestPasswordResetCommandHandler(
        IUserRepository users,
        SessionAccountResolver accounts,
        IPasswordResetTokenRepository tokens,
        IOutboundEmailRepository outboundEmails,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        IOptions<PasswordResetOptions> options)
    {
        _users = users;
        _accounts = accounts;
        _tokens = tokens;
        _outboundEmails = outboundEmails;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.From(request.Email);
        }
        catch (DomainException)
        {
            return Result.Success();
        }

        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is not { IsActive: true }
            || await _accounts.ResolveAsync(user, cancellationToken) is not { } account)
        {
            return Result.Success();
        }

        var now = _clock.UtcNow;

        if (await _tokens.AnyIssuedAfterAsync(user.Id, now - ResendInterval, cancellationToken))
        {
            return Result.Success();
        }

        var value = PasswordResetToken.GenerateValue();
        _tokens.Add(PasswordResetToken.Issue(user.Id, value, now));
        _outboundEmails.Add(PasswordResetEmail.Queue(user.Email, account.FullName, _options.LinkFor(value), now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
