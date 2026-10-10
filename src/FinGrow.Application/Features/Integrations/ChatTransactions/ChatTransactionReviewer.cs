namespace FinGrow.Application.Features.Integrations.ChatTransactions;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal enum ChatDecision
{
    Confirm,
    Discard
}

internal enum ReviewOutcome
{
    Confirmed,
    Discarded,
    NotReviewable
}

internal sealed record Review(ReviewOutcome Outcome, Transaction? Transaction = null);

internal sealed class ChatTransactionReviewer
{
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ChatTransactionReviewer(ITransactionRepository transactions, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Review> ReviewAsync(
        Guid employeeId,
        Guid transactionId,
        ChatDecision decision,
        PaymentMethod? paymentMethod,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(transactionId, cancellationToken);

        if (transaction is null || transaction.EmployeeId != employeeId || !transaction.IsPending)
        {
            return new Review(ReviewOutcome.NotReviewable);
        }

        var now = _clock.UtcNow;

        if (decision == ChatDecision.Discard)
        {
            transaction.Discard(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new Review(ReviewOutcome.Discarded, transaction);
        }

        if (paymentMethod is { } method && ChatTransactionText.IsAllowed(transaction.Type, method))
        {
            transaction.UpdateDetails(transaction.Amount, transaction.Description, transaction.OccurredOn, method, now);
        }

        transaction.Confirm(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new Review(ReviewOutcome.Confirmed, transaction);
    }
}
