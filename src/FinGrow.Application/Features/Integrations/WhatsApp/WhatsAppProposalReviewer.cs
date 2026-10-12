namespace FinGrow.Application.Features.Integrations.WhatsApp;

using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal sealed class WhatsAppProposalReviewer
{
    public static readonly TimeSpan ProposalWindow = TimeSpan.FromHours(24);

    private readonly ITransactionRepository _transactions;
    private readonly ChatTransactionReviewer _reviewer;
    private readonly IDateTimeProvider _clock;

    public WhatsAppProposalReviewer(ITransactionRepository transactions, ChatTransactionReviewer reviewer, IDateTimeProvider clock)
    {
        _transactions = transactions;
        _reviewer = reviewer;
        _clock = clock;
    }

    public async Task<Review?> TryReviewAsync(Guid employeeId, string text, CancellationToken cancellationToken)
    {
        if (WhatsAppReviewReply.Parse(text) is not { } reply)
        {
            return null;
        }

        var since = _clock.UtcNow - ProposalWindow;
        var pending = await _transactions.ListPendingByEmployeeAsync(employeeId, cancellationToken);
        var proposal = pending
            .Where(transaction => transaction.Source == TransactionSource.WhatsApp && transaction.CreatedAt >= since)
            .MaxBy(transaction => transaction.CreatedAt);

        if (proposal is null)
        {
            return null;
        }

        PaymentMethod? paymentMethod = null;

        if (reply.Option is { } option)
        {
            var paymentMethods = ChatTransactionText.PaymentMethodsFor(proposal.Type);

            if (option > paymentMethods.Count)
            {
                return null;
            }

            paymentMethod = paymentMethods[option - 1];
        }

        return await _reviewer.ReviewAsync(employeeId, proposal.Id, reply.Decision, paymentMethod, cancellationToken);
    }
}
