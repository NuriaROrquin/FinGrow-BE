namespace FinGrow.Application.UnitTests.Features.Investments.ListInvestments;

using Common;
using DTOs;
using FinGrow.Application.Features.Investments.ListInvestments;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class ListInvestmentsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 15, 0, 0, TimeSpan.Zero);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeInvestmentRepository _investments = new();

    [Fact]
    public async Task Only_the_investments_of_the_employee_are_listed()
    {
        Store(_employeeId, "AL30", new DateOnly(2026, 9, 1));
        Store(Guid.CreateVersion7(), "Bitcoin", new DateOnly(2026, 9, 2));

        var result = await ListAsync();

        var investment = result.Value.ShouldHaveSingleItem();
        investment.AssetName.ShouldBe("AL30");
    }

    [Fact]
    public async Task The_most_recent_purchase_comes_first()
    {
        Store(_employeeId, "AL30", new DateOnly(2026, 8, 1));
        Store(_employeeId, "Bitcoin", new DateOnly(2026, 9, 2));

        var result = await ListAsync();

        result.Value.Count.ShouldBe(2);
        result.Value[0].AssetName.ShouldBe("Bitcoin");
        result.Value[1].AssetName.ShouldBe("AL30");
    }

    [Fact]
    public async Task Each_investment_shows_its_type_capital_and_current_value()
    {
        var stored = Store(_employeeId, "AL30", new DateOnly(2026, 9, 1));
        stored.RecordValuation(Money.From(1200m, Currency.ARS), new DateOnly(2026, 9, 20), ValuationSource.Manual, Now);

        var investment = (await ListAsync()).Value.ShouldHaveSingleItem();

        investment.Type.ShouldBe(InvestmentType.Bond);
        investment.InvestedAmount.ShouldBe(1000m);
        investment.CurrentValue.ShouldBe(1200m);
        investment.ValuedOn.ShouldBe(new DateOnly(2026, 9, 20));
        investment.ReturnAmount.ShouldBe(200m);
        investment.ReturnPercentage.ShouldBe(20m);
    }

    private Task<Result<IReadOnlyList<InvestmentResponse>>> ListAsync() =>
        new ListInvestmentsHandler(_investments).Handle(new ListInvestmentsQuery(_employeeId), CancellationToken.None);

    private Investment Store(Guid employeeId, string assetName, DateOnly purchasedOn)
    {
        var investment = Investment.Create(employeeId, assetName, InvestmentType.Bond, Money.From(1000m, Currency.ARS), purchasedOn, Now);
        _investments.Add(investment);
        return investment;
    }
}
