namespace FinGrow.Application.UnitTests.Features.ExchangeRates;

using Common;
using DTOs;
using FinGrow.Application.Features.ExchangeRates.GetMepQuote;
using Fakes;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

public class GetMepQuoteHandlerTests
{
    private readonly FakeExchangeRateProvider _provider = new();

    [Fact]
    public async Task The_quote_is_returned_as_dollars_to_pesos_with_buy_sell_and_update_time()
    {
        var result = await HandleAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.BaseCurrency.ShouldBe(Currency.USD);
        result.Value.QuoteCurrency.ShouldBe(Currency.ARS);
        result.Value.Buy.ShouldBe(1544.30m);
        result.Value.Sell.ShouldBe(1557.30m);
        result.Value.UpdatedAt.ShouldBe(new DateTimeOffset(2026, 9, 27, 14, 57, 0, TimeSpan.Zero));
        result.Value.Source.ShouldBe("DolarApi");
    }

    [Fact]
    public async Task An_unreachable_source_is_reported_as_unavailable_instead_of_throwing()
    {
        _provider.Failure = new HttpRequestException("dolarapi.com no responde");

        var result = await HandleAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        result.Error.Code.ShouldBe("ExchangeRate.MepUnavailable");
    }

    [Fact]
    public async Task A_source_that_times_out_is_reported_as_unavailable()
    {
        _provider.Failure = new TaskCanceledException("timeout", new TimeoutException());

        var result = await HandleAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
    }

    [Fact]
    public async Task A_request_cancelled_by_the_caller_is_not_reported_as_unavailable()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _provider.Failure = new TaskCanceledException("cancelled");

        await Should.ThrowAsync<TaskCanceledException>(() => HandleAsync(cancellation.Token));
    }

    private Task<Result<MepQuoteResponse>> HandleAsync(CancellationToken cancellationToken = default) =>
        new GetMepQuoteHandler(_provider, NullLogger<GetMepQuoteHandler>.Instance)
            .Handle(new GetMepQuoteQuery(), cancellationToken);
}
