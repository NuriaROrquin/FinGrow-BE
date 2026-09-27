namespace FinGrow.Application.UnitTests.Features.Investments.UpdateInvestment;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Investments.UpdateInvestment;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class UpdateInvestmentHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeInvestmentRepository _investments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly UpdateInvestmentHandler _handler;

    public UpdateInvestmentHandlerTests() =>
        _handler = new UpdateInvestmentHandler(_investments, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task The_investment_is_corrected_and_saved()
    {
        var investment = StoredInvestment(EmployeeId);
        var command = new UpdateInvestmentCommand(
            investment.Id,
            EmployeeId,
            AssetName: "Bitcoin",
            InvestmentType.Crypto,
            InvestedAmount: 500m,
            Currency.USD,
            PurchasedOn: new DateOnly(2026, 8, 15));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AssetName.ShouldBe("Bitcoin");
        result.Value.Type.ShouldBe(InvestmentType.Crypto);
        result.Value.InvestedAmount.ShouldBe(500m);
        result.Value.CurrentValue.ShouldBe(500m);
        result.Value.Currency.ShouldBe(Currency.USD);
        result.Value.PurchasedOn.ShouldBe(new DateOnly(2026, 8, 15));
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Another_employees_investment_is_reported_as_not_found()
    {
        var investment = StoredInvestment(Guid.CreateVersion7());
        var command = new UpdateInvestmentCommand(
            investment.Id,
            EmployeeId,
            AssetName: "Bitcoin",
            InvestmentType.Crypto,
            InvestedAmount: 500m,
            Currency.USD,
            PurchasedOn: new DateOnly(2026, 8, 15));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        investment.AssetName.ShouldBe("AL30");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private Investment StoredInvestment(Guid employeeId)
    {
        var investment = Investment.Create(
            employeeId,
            "AL30",
            InvestmentType.Bond,
            Money.From(250000m, Currency.ARS),
            new DateOnly(2026, 9, 1),
            Now);
        _investments.Add(investment);

        return investment;
    }
}
