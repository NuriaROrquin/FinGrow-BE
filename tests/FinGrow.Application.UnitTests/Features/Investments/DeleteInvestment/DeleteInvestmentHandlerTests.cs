namespace FinGrow.Application.UnitTests.Features.Investments.DeleteInvestment;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Investments.DeleteInvestment;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class DeleteInvestmentHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeInvestmentRepository _investments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DeleteInvestmentHandler _handler;

    public DeleteInvestmentHandlerTests() =>
        _handler = new DeleteInvestmentHandler(_investments, _unitOfWork);

    [Fact]
    public async Task The_investment_is_removed_from_the_portfolio()
    {
        var investment = StoredInvestment(EmployeeId);

        var result = await _handler.Handle(new DeleteInvestmentCommand(investment.Id, EmployeeId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _investments.Investments.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Another_employees_investment_is_reported_as_not_found_and_kept()
    {
        var investment = StoredInvestment(Guid.CreateVersion7());

        var result = await _handler.Handle(new DeleteInvestmentCommand(investment.Id, EmployeeId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _investments.Investments.ShouldHaveSingleItem();
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
