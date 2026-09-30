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
    private readonly FakeInvestmentReadRepository _investments = new();

    [Fact]
    public async Task The_page_is_requested_for_the_employee_with_every_filter_untouched()
    {
        var filters = new InvestmentFilters(
            PageNumber: 2,
            PageSize: 5,
            Search: "al30",
            Types: new[] { InvestmentType.Bond, InvestmentType.Stock },
            Currencies: new[] { Currency.ARS },
            PurchasedFrom: new DateOnly(2026, 1, 1),
            PurchasedTo: new DateOnly(2026, 9, 30),
            MinInvested: 100m,
            MaxInvested: 5000m,
            Quoted: true,
            Performance: InvestmentPerformance.Loss,
            SortBy: InvestmentSortField.ReturnPercentage,
            SortDirection: SortDirection.Ascending);

        await ListAsync(filters);

        _investments.RequestedEmployeeId.ShouldBe(_employeeId);
        _investments.RequestedFilters.ShouldBe(filters);
    }

    [Fact]
    public async Task Each_investment_of_the_page_shows_its_type_capital_current_value_and_quote_state()
    {
        var stored = Store(_employeeId, "AL30", new DateOnly(2026, 9, 1));
        stored.RecordValuation(Money.From(1200m, Currency.ARS), new DateOnly(2026, 9, 20), ValuationSource.Feed, Now);

        var investment = (await ListAsync(new InvestmentFilters())).Value.Items.ShouldHaveSingleItem();

        investment.Type.ShouldBe(InvestmentType.Bond);
        investment.InvestedAmount.ShouldBe(1000m);
        investment.CurrentValue.ShouldBe(1200m);
        investment.ValuedOn.ShouldBe(new DateOnly(2026, 9, 20));
        investment.ReturnAmount.ShouldBe(200m);
        investment.ReturnPercentage.ShouldBe(20m);
        investment.HasMarketValuation.ShouldBeTrue();
    }

    [Fact]
    public async Task The_paging_information_comes_back_with_the_items()
    {
        for (var day = 1; day <= 7; day++)
        {
            Store(_employeeId, $"Activo {day}", new DateOnly(2026, 9, day));
        }

        var page = (await ListAsync(new InvestmentFilters(PageNumber: 2, PageSize: 3))).Value;

        page.Items.Count.ShouldBe(3);
        page.PageNumber.ShouldBe(2);
        page.PageSize.ShouldBe(3);
        page.TotalCount.ShouldBe(7);
        page.TotalPages.ShouldBe(3);
    }

    private Task<Result<PagedResult<InvestmentResponse>>> ListAsync(InvestmentFilters filters) =>
        new ListInvestmentsHandler(_investments).Handle(new ListInvestmentsQuery(_employeeId, filters), CancellationToken.None);

    private Investment Store(Guid employeeId, string assetName, DateOnly purchasedOn)
    {
        var investment = Investment.Create(employeeId, assetName, InvestmentType.Bond, Money.From(1000m, Currency.ARS), purchasedOn, Now);
        _investments.Investments.Add(investment);
        return investment;
    }
}
