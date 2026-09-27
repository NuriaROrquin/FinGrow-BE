namespace FinGrow.Application.UnitTests.Validations.Investments;

using FinGrow.Application.Features.Investments.CreateInvestment;
using Fakes;
using FinGrow.Application.Validations.Investments;
using Domain.Entities;
using Domain.Enums;

public class CreateInvestmentValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 27);

    private readonly CreateInvestmentValidator _validator = new(new FakeDateTimeProvider(Now));

    private static CreateInvestmentCommand ValidCommand() => new(
        Guid.CreateVersion7(),
        AssetName: "S&P 500 ETF",
        InvestmentType.Etf,
        InvestedAmount: 1000m,
        Currency.USD,
        PurchasedOn: Today.AddMonths(-1));

    [Fact]
    public void A_valid_command_passes()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(InvestmentType.Etf)]
    [InlineData(InvestmentType.Stock)]
    [InlineData(InvestmentType.Bond)]
    [InlineData(InvestmentType.MutualFund)]
    [InlineData(InvestmentType.Crypto)]
    public void Every_supported_asset_type_is_accepted(InvestmentType type)
    {
        var result = _validator.Validate(ValidCommand() with { Type = type });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void An_asset_type_outside_the_supported_ones_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Type = (InvestmentType)99 });

        result.IsValid.ShouldBeFalse();
        var error = result.Errors.ShouldHaveSingleItem();
        error.PropertyName.ShouldBe(nameof(CreateInvestmentCommand.Type));
        error.ErrorMessage.ShouldContain("Etf, Stock, Bond, MutualFund, Crypto");
    }

    [Fact]
    public void A_purchase_made_today_passes()
    {
        var result = _validator.Validate(ValidCommand() with { PurchasedOn = Today });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Late_at_night_today_is_the_argentinian_date_not_the_utc_one()
    {
        var lateNightInBuenosAires = new DateTimeOffset(2026, 9, 28, 2, 30, 0, TimeSpan.Zero);
        var validator = new CreateInvestmentValidator(new FakeDateTimeProvider(lateNightInBuenosAires));

        var result = validator.Validate(ValidCommand() with { PurchasedOn = new DateOnly(2026, 9, 27) });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_purchase_date_in_the_future_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { PurchasedOn = Today.AddDays(1) });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateInvestmentCommand.PurchasedOn));
    }

    [Fact]
    public void A_missing_purchase_date_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { PurchasedOn = default });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateInvestmentCommand.PurchasedOn));
    }

    [Fact]
    public void A_zero_invested_amount_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { InvestedAmount = 0m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateInvestmentCommand.InvestedAmount));
    }

    [Fact]
    public void A_negative_invested_amount_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { InvestedAmount = -100m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateInvestmentCommand.InvestedAmount));
    }

    [Fact]
    public void An_empty_asset_name_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { AssetName = "   " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateInvestmentCommand.AssetName));
    }

    [Fact]
    public void An_asset_name_longer_than_the_maximum_is_rejected()
    {
        var tooLong = new string('a', Investment.MaxAssetNameLength + 1);

        var result = _validator.Validate(ValidCommand() with { AssetName = tooLong });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateInvestmentCommand.AssetName));
    }

    [Fact]
    public void An_unknown_currency_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Currency = (Currency)999 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateInvestmentCommand.Currency));
    }
}
