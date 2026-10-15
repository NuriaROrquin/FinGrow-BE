namespace FinGrow.Application.Interfaces;

public enum IolCountry
{
    Argentina = 1,
    UnitedStates = 2
}

public sealed record IolPosition(
    string Symbol,
    string? Description,
    string Type,
    string Market,
    string Currency,
    decimal Quantity,
    decimal AveragePurchasePrice,
    decimal ValuedAmount);

public sealed record IolOperation(string Symbol, string Type, DateOnly? ExecutedOn);

public interface IIolPortfolioClient
{
    Task<IReadOnlyList<IolPosition>> GetPortfolioAsync(
        string accessToken,
        IolCountry country,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IolOperation>> ListCompletedOperationsAsync(
        string accessToken,
        DateOnly from,
        CancellationToken cancellationToken = default);
}
