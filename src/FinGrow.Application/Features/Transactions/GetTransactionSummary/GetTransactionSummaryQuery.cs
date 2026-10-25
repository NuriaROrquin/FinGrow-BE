namespace FinGrow.Application.Features.Transactions.GetTransactionSummary;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

public sealed record GetTransactionSummaryQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<TransactionSummaryResponse>>;

public sealed record TransactionSummaryResponse(
    IReadOnlyDictionary<Currency, CurrencyTransactionSummary> Currencies,
    IReadOnlyCollection<Currency> OtherCurrencies)
{
    [JsonIgnore]
    public decimal TotalIncomeArs => Currencies.GetValueOrDefault(Currency.ARS)?.Income ?? 0m;

    [JsonIgnore]
    public decimal TotalIncomeUsd => Currencies.GetValueOrDefault(Currency.USD)?.Income ?? 0m;

    [JsonIgnore]
    public int TotalIncomeTransactions => Currencies.Values.Sum(currency => currency.IncomeTransactions);

    [JsonIgnore]
    public decimal TotalExpenseArs => Currencies.GetValueOrDefault(Currency.ARS)?.Expenses ?? 0m;

    [JsonIgnore]
    public decimal TotalExpenseUsd => Currencies.GetValueOrDefault(Currency.USD)?.Expenses ?? 0m;

    [JsonIgnore]
    public int TotalExpenseTransactions => Currencies.Values.Sum(currency => currency.ExpenseTransactions);

    [JsonIgnore]
    public int TotalTransactions => Currencies.Values.Sum(currency => currency.Transactions);
}

internal sealed class GetTransactionSummaryQueryHandler(
    ITransactionReadRepository transactionReadRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetTransactionSummaryQuery, Result<TransactionSummaryResponse>>
{
    public async Task<Result<TransactionSummaryResponse>> Handle(
        GetTransactionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TransactionSummaryResponse>(
                Error.Forbidden("Transactions.Unauthenticated", "Hay que iniciar sesion para consultar el resumen."));
        }

        var summary = await transactionReadRepository.GetSummaryAsync(
            employeeId, request.FromDate, request.ToDate, cancellationToken);

        return Result.Success(new TransactionSummaryResponse(
            summary.Currencies,
            summary.OtherCurrencies));
    }
}
