namespace FinGrow.Application.Features.Integrations.ChatTransactions;

using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal enum ProposalOutcome
{
    Proposed,
    AlreadyProposed,
    TooLong,
    MissingAmount,
    MultipleTransactions,
    NotATransaction,
    AiUnavailable
}

internal sealed record Proposal(
    ProposalOutcome Outcome,
    Transaction? Transaction = null,
    bool AsksPaymentMethod = false,
    bool DateIsEstimate = false)
{
    public static Proposal Of(ProposalOutcome outcome) => new(outcome);
}

internal sealed partial class ChatTransactionProposer
{
    public const Currency DefaultCurrency = Currency.ARS;

    public const PaymentMethod DefaultPaymentMethod = PaymentMethod.Cash;

    public const int MaxTextLength = 1000;

    private readonly IAiService _ai;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ChatTransactionProposer> _logger;

    public ChatTransactionProposer(
        IAiService ai,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<ChatTransactionProposer> logger)
    {
        _ai = ai;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Proposal> ProposeAsync(
        Guid employeeId,
        TransactionSource source,
        string externalReference,
        string text,
        CancellationToken cancellationToken)
    {
        var known = await _transactions.ListExistingExternalReferencesAsync(
            employeeId, source, new[] { externalReference }, cancellationToken);

        if (known.Count > 0)
        {
            return Proposal.Of(ProposalOutcome.AlreadyProposed);
        }

        if (text.Length > MaxTextLength)
        {
            return Proposal.Of(ProposalOutcome.TooLong);
        }

        var parsed = await ParseAsync(employeeId, text, cancellationToken);

        if (parsed is null)
        {
            return Proposal.Of(ProposalOutcome.AiUnavailable);
        }

        switch (parsed.Outcome)
        {
            case MessageParsingOutcome.MissingAmount:
                return Proposal.Of(ProposalOutcome.MissingAmount);
            case MessageParsingOutcome.MultipleTransactions:
                return Proposal.Of(ProposalOutcome.MultipleTransactions);
            case MessageParsingOutcome.NotATransaction:
                return Proposal.Of(ProposalOutcome.NotATransaction);
        }

        if (parsed.Transaction is not { } proposed)
        {
            return Proposal.Of(ProposalOutcome.AiUnavailable);
        }

        var paymentMethod = ChatTransactionText.IsAllowed(proposed.Type, proposed.PaymentMethod)
            ? proposed.PaymentMethod
            : null;
        var (occurredOn, dateIsEstimate) = ResolveOccurredOn(proposed);
        var transaction = Register(employeeId, source, externalReference, proposed, occurredOn, paymentMethod, parsed.Model);

        _transactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new Proposal(
            ProposalOutcome.Proposed,
            transaction,
            AsksPaymentMethod: paymentMethod is null,
            DateIsEstimate: dateIsEstimate);
    }

    private Transaction Register(
        Guid employeeId,
        TransactionSource source,
        string externalReference,
        ParsedTransaction proposed,
        DateOnly occurredOn,
        PaymentMethod? paymentMethod,
        string? model)
    {
        var now = _clock.UtcNow;
        var amount = Money.From(proposed.Amount, proposed.Currency);

        if (proposed.Type == TransactionType.Expense)
        {
            var expenseCategory = proposed.ExpenseCategory ?? throw MissingCategory();
            var expense = Transaction.RegisterExpense(
                employeeId,
                amount,
                expenseCategory,
                proposed.Description,
                occurredOn,
                paymentMethod ?? DefaultPaymentMethod,
                source,
                TransactionStatus.Pending,
                now,
                externalReference);
            expense.SuggestExpenseCategory(expenseCategory, proposed.Confidence, model, now);

            return expense;
        }

        var incomeCategory = proposed.IncomeCategory ?? throw MissingCategory();
        var income = Transaction.RegisterIncome(
            employeeId,
            amount,
            incomeCategory,
            proposed.Description,
            occurredOn,
            paymentMethod ?? DefaultPaymentMethod,
            source,
            TransactionStatus.Pending,
            now,
            externalReference);
        income.SuggestIncomeCategory(incomeCategory, proposed.Confidence, model, now);

        return income;
    }

    private (DateOnly OccurredOn, bool IsEstimate) ResolveOccurredOn(ParsedTransaction proposed)
    {
        var today = _clock.Today;

        return proposed.OccurredOn is { } date && date <= today
            ? (date, proposed.OccurredOnIsEstimate)
            : (today, false);
    }

    private static InvalidOperationException MissingCategory() =>
        new("FinGrow-AI propuso un movimiento sin la categoria que corresponde a su tipo.");

    private async Task<ParsedMessage?> ParseAsync(Guid employeeId, string text, CancellationToken cancellationToken)
    {
        try
        {
            return await _ai.ParseTransactionMessageAsync(text, DefaultCurrency, _clock.Today, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogAiUnavailable(_logger, exception, employeeId);
        }
        catch (JsonException exception)
        {
            LogAiUnavailable(_logger, exception, employeeId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogAiUnavailable(_logger, exception, employeeId);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "FinGrow-AI no pudo interpretar el mensaje del empleado {EmployeeId}; no se registro ningun movimiento.")]
    private static partial void LogAiUnavailable(ILogger logger, Exception exception, Guid employeeId);
}
