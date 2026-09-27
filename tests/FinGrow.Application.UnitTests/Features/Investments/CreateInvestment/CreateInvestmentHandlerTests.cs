namespace FinGrow.Application.UnitTests.Features.Investments.CreateInvestment;

using FinGrow.Application.Features.Investments.CreateInvestment;
using Fakes;
using Domain.Enums;

public class CreateInvestmentHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeInvestmentRepository _investments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateInvestmentHandler _handler;

    public CreateInvestmentHandlerTests() =>
        _handler = new CreateInvestmentHandler(_investments, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task A_new_investment_is_persisted_for_the_employee_with_its_asset_type_capital_and_date()
    {
        var employeeId = Guid.CreateVersion7();
        var command = new CreateInvestmentCommand(
            employeeId,
            AssetName: "AL30",
            InvestmentType.Bond,
            InvestedAmount: 250000m,
            Currency.ARS,
            PurchasedOn: new DateOnly(2026, 9, 1));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AssetName.ShouldBe("AL30");
        result.Value.Type.ShouldBe(InvestmentType.Bond);
        result.Value.InvestedAmount.ShouldBe(250000m);
        result.Value.Currency.ShouldBe(Currency.ARS);
        result.Value.PurchasedOn.ShouldBe(new DateOnly(2026, 9, 1));
        _unitOfWork.SaveCount.ShouldBe(1);

        var stored = _investments.Investments.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task A_new_investment_starts_worth_what_was_invested_with_no_return()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Value.CurrentValue.ShouldBe(result.Value.InvestedAmount);
        result.Value.ValuedOn.ShouldBe(result.Value.PurchasedOn);
        result.Value.ReturnAmount.ShouldBe(0m);
        result.Value.ReturnPercentage.ShouldBe(0m);
    }

    [Fact]
    public async Task The_asset_name_is_stored_trimmed()
    {
        var result = await _handler.Handle(ValidCommand() with { AssetName = "  Bitcoin  " }, CancellationToken.None);

        result.Value.AssetName.ShouldBe("Bitcoin");
    }

    private static CreateInvestmentCommand ValidCommand() => new(
        Guid.CreateVersion7(),
        AssetName: "S&P 500 ETF",
        InvestmentType.Etf,
        InvestedAmount: 1000m,
        Currency.USD,
        PurchasedOn: new DateOnly(2026, 9, 1));
}
