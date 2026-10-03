namespace FinGrow.Application.UnitTests.Features.Dashboard;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Dashboard.GetDashboard;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Application.UnitTests.Fakes;

public sealed class GetDashboardQueryTests
{
    private static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Today = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] InvalidQueryMessages =
    {
        "El dashboard solo soporta ARS y USD.",
        "La fecha desde no puede ser posterior a la fecha hasta.",
    };

    [Fact]
    public async Task Handler_uses_current_month_and_calculates_dashboard_values()
    {
        var repository = new FakeTransactionReadRepository
        {
            SummaryFactory = (_, fromDate, toDate) =>
                fromDate == new DateOnly(2026, 10, 1) && toDate == new DateOnly(2026, 10, 15)
                    ? new TransactionSummary(4, 2, 2, 1000m, 0m, 400m, 0m)
                    : fromDate == new DateOnly(2026, 9, 1) && toDate == new DateOnly(2026, 9, 30)
                        ? new TransactionSummary(3, 2, 1, 800m, 0m, 200m, 0m)
                        : new TransactionSummary(10, 6, 4, 5000m, 0m, 2500m, 0m),
        };
        var handler = new GetDashboardQueryHandler(
            repository,
            new FakeCurrentUser { UserId = EmployeeId },
            new FakeDateTimeProvider(Today));

        var result = await handler.Handle(
            new GetDashboardQuery(Currency.ARS),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new DashboardResponse(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 15),
            Currency.ARS,
            true,
            2500m,
            1000m,
            400m,
            600m,
            60m,
            75m));
        repository.EmployeeIds.ShouldAllBe(employeeId => employeeId == EmployeeId);
    }

    [Fact]
    public async Task Handler_returns_empty_dashboard_without_movements()
    {
        var repository = new FakeTransactionReadRepository
        {
            SummaryFactory = (_, _, _) => new TransactionSummary(0, 0, 0, 0m, 0m, 0m, 0m),
        };
        var handler = new GetDashboardQueryHandler(
            repository,
            new FakeCurrentUser { UserId = EmployeeId },
            new FakeDateTimeProvider(Today));

        var result = await handler.Handle(
            new GetDashboardQuery(Currency.USD, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            CancellationToken.None);

        result.Value.HasMovements.ShouldBeFalse();
        result.Value.Balance.ShouldBeNull();
        result.Value.Income.ShouldBeNull();
        result.Value.Expenses.ShouldBeNull();
        result.Value.SavingsRate.ShouldBeNull();
        repository.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handler_returns_unauthorized_without_authenticated_user()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = new GetDashboardQueryHandler(
            repository,
            new FakeCurrentUser(),
            new FakeDateTimeProvider(Today));

        var result = await handler.Handle(
            new GetDashboardQuery(Currency.ARS),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        repository.Calls.ShouldBeEmpty();
    }

    [Fact]
    public void Validator_rejects_unsupported_currency_and_reversed_dates()
    {
        var validator = new GetDashboardQueryValidator();

        var result = validator.Validate(new GetDashboardQuery(
            Currency.BRL,
            new DateOnly(2026, 10, 15),
            new DateOnly(2026, 10, 1)));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.ErrorMessage).ShouldBe(InvalidQueryMessages);
    }

    private sealed class FakeTransactionReadRepository : ITransactionReadRepository
    {
        public Func<Guid, DateOnly?, DateOnly?, TransactionSummary> SummaryFactory { get; init; } =
            (_, _, _) => new TransactionSummary(0, 0, 0, 0m, 0m, 0m, 0m);

        public List<Guid> EmployeeIds { get; } = new();

        public List<(DateOnly? FromDate, DateOnly? ToDate)> Calls { get; } = new();

        public Task<TransactionSummary> GetSummaryAsync(
            Guid employeeId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            CancellationToken cancellationToken = default)
        {
            EmployeeIds.Add(employeeId);
            Calls.Add((fromDate, toDate));
            return Task.FromResult(SummaryFactory(employeeId, fromDate, toDate));
        }

        public Task<IReadOnlyList<MonthlyExpenseTotal>> GetMonthlyExpensesAsync(
            Guid employeeId,
            Currency currency,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MonthlyExpenseTotal>>(Array.Empty<MonthlyExpenseTotal>());

        public Task<IReadOnlyList<MonthlyIncomeExpenseTotal>> GetMonthlyIncomeExpensesAsync(
            Guid employeeId,
            Currency currency,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MonthlyIncomeExpenseTotal>>(Array.Empty<MonthlyIncomeExpenseTotal>());

        public Task<IReadOnlyList<CategoryExpenseTotal>> GetExpensesByCategoryAsync(
            Guid employeeId,
            Currency currency,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CategoryExpenseTotal>>(Array.Empty<CategoryExpenseTotal>());

        public Task<TransactionPage> GetPageAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? search,
            TransactionType? type,
            IReadOnlyCollection<TransactionStatus> statuses,
            ExpenseCategory? expenseCategory,
            IncomeCategory? incomeCategory,
            PaymentMethod? paymentMethod,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransactionPage(Array.Empty<Transaction>(), pageNumber, pageSize, 0));

        public Task<IReadOnlyList<Transaction>> GetFilteredAsync(
            Guid employeeId,
            string? search,
            TransactionType? type,
            IReadOnlyCollection<TransactionStatus> statuses,
            ExpenseCategory? expenseCategory,
            IncomeCategory? incomeCategory,
            PaymentMethod? paymentMethod,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(Array.Empty<Transaction>());
    }
}
