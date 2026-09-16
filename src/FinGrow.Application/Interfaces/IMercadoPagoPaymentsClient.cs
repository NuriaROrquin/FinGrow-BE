namespace FinGrow.Application.Interfaces;

public sealed record MercadoPagoPayment(
    long Id,
    string Status,
    string OperationType,
    decimal TransactionAmount,
    string CurrencyId,
    string? Description,
    string? PaymentTypeId,
    long? CollectorId,
    long? PayerId,
    DateTimeOffset DateCreated,
    DateTimeOffset? DateApproved);

public sealed record MercadoPagoPaymentsPage(IReadOnlyList<MercadoPagoPayment> Results, int Total);

public interface IMercadoPagoPaymentsClient
{
    Task<MercadoPagoPaymentsPage> SearchUpdatedBetweenAsync(
        string accessToken,
        DateTimeOffset from,
        DateTimeOffset to,
        int offset,
        CancellationToken cancellationToken = default);
}
