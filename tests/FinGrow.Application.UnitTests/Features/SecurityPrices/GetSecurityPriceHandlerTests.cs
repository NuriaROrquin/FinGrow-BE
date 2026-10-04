namespace FinGrow.Application.UnitTests.Features.SecurityPrices;

using Common;
using DTOs;
using FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;
using Fakes;
using Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

public class GetSecurityPriceHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateOnly LastFriday = new(2026, 10, 2);

    private readonly FakeMarketPriceProvider _byma = new();
    private readonly FakeSecurityPriceRepository _stored = new();

    [Fact]
    public async Task A_symbol_with_a_live_price_is_quoted_with_the_price_byma_publishes_today()
    {
        _byma.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));
        _stored.Prices.Add(SecurityPrice.Create("AL30", Currency.ARS, 830m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("AL30", Currency.ARS);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SecurityPriceResponse("AL30", Currency.ARS, 839.40m, Today, "BYMA"));
    }

    [Fact]
    public async Task The_symbol_is_looked_up_trimmed_and_in_upper_case()
    {
        _byma.Prices.Add(new MarketPrice("YPFD", Currency.ARS, 8305m));

        var result = await HandleAsync(" ypfd ", Currency.ARS);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Symbol.ShouldBe("YPFD");
        result.Value.UnitPrice.ShouldBe(8305m);
    }

    [Fact]
    public async Task The_currency_picks_the_variant_and_the_peso_price_is_not_used_for_dollars()
    {
        _byma.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));
        _byma.Prices.Add(new MarketPrice("AL30D", Currency.USD, 0.5397m));

        var dollars = await HandleAsync("AL30D", Currency.USD);
        var wrongVariant = await HandleAsync("AL30", Currency.USD);

        dollars.Value.UnitPrice.ShouldBe(0.5397m);
        wrongVariant.IsFailure.ShouldBeTrue();
        wrongVariant.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task When_byma_publishes_no_prices_like_on_a_weekend_the_last_stored_close_comes_with_its_date()
    {
        _stored.Prices.Add(SecurityPrice.Create("AL30", Currency.ARS, 839.40m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("AL30", Currency.ARS);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SecurityPriceResponse("AL30", Currency.ARS, 839.40m, LastFriday, "BYMA"));
    }

    [Fact]
    public async Task When_byma_does_not_answer_the_last_stored_close_is_used()
    {
        _byma.Failure = new HttpRequestException("open.bymadata.com.ar no responde");
        _stored.Prices.Add(SecurityPrice.Create("AL30", Currency.ARS, 839.40m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("AL30", Currency.ARS);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PricedOn.ShouldBe(LastFriday);
    }

    [Fact]
    public async Task A_slow_source_falls_back_to_the_stored_close_like_one_that_does_not_answer()
    {
        _byma.Failure = new TaskCanceledException("BYMA tardo demasiado");
        _stored.Prices.Add(SecurityPrice.Create("AL30", Currency.ARS, 839.40m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("AL30", Currency.ARS);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UnitPrice.ShouldBe(839.40m);
    }

    [Fact]
    public async Task A_symbol_that_byma_does_not_have_and_was_never_stored_is_not_found_with_a_hint_about_the_variant()
    {
        _byma.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));

        var result = await HandleAsync("XXXX", Currency.ARS);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("SecurityPrice.NotFound");
        result.Error.Description.ShouldContain("XXXX");
        result.Error.Description.ShouldContain("AL30D");
    }

    [Fact]
    public async Task When_byma_does_not_answer_and_nothing_was_stored_the_price_is_unavailable()
    {
        _byma.Failure = new HttpRequestException("open.bymadata.com.ar no responde");

        var result = await HandleAsync("AL30", Currency.ARS);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        result.Error.Code.ShouldBe("SecurityPrice.Unavailable");
    }

    private Task<Result<SecurityPriceResponse>> HandleAsync(string symbol, Currency currency) =>
        new GetSecurityPriceHandler(_byma, _stored, new FakeDateTimeProvider(Now), NullLogger<GetSecurityPriceHandler>.Instance)
            .Handle(new GetSecurityPriceQuery(symbol, currency), CancellationToken.None);
}
