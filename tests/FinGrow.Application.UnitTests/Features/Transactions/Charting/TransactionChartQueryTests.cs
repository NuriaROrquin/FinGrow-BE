namespace FinGrow.Application.UnitTests.Features.Transactions.Charting;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions;
using FinGrow.Application.Features.Transactions.GetExpensesByCategory;
using FinGrow.Application.Features.Transactions.GetMonthlyExpenses;
using FinGrow.Application.Features.Transactions.GetMonthlyIncomeExpenses;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;

public sealed class TransactionChartQueryTests
{
    private static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Today = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] ExpectedMonths =
    {
        "2026-05", "2026-06", "2026-07", "2026-08", "2026-09", "2026-10",
    };
    private static readonly decimal[] ExpectedExpenseTotals = { 125000.50m, 0m, 0m, 0m, 0m, 800m };

    [Fact]
    public async Task Monthly_expenses_returns_six_ascending_months_with_zeroes_and_forwards_scope()
    {
        var repository = new FakeTransactionReadRepository
        {
            MonthlyExpenses = new[]
            {
                new MonthlyExpenseTotal(2026, 5, 125000.50m),
                new MonthlyExpenseTotal(2026, 10, 800m),
            },
        };
        var handler = new GetMonthlyExpensesQueryHandler(
            repository,
            new FakeCurrentUser { UserId = EmployeeId },
            new FakeDateTimeProvider(Today));

        var result = await handler.Handle(new GetMonthlyExpensesQuery(Currency.ARS), CancellationToken.None);

        result.Value.Items.Select(item => item.Month).ShouldBe(ExpectedMonths);
        result.Value.Items.Select(item => item.TotalExpense).ShouldBe(ExpectedExpenseTotals);
        repository.EmployeeId.ShouldBe(EmployeeId);
        repository.Currency.ShouldBe(Currency.ARS);
        repository.FromDate.ShouldBe(new DateOnly(2026, 5, 1));
        repository.ToDate.ShouldBe(new DateOnly(2026, 10, 31));
    }

    [Fact]
    public async Task Income_vs_expenses_returns_both_totals_and_keeps_the_requested_currency()
    {
        var repository = new FakeTransactionReadRepository
        {
            MonthlyIncomeExpenses = new[] { new MonthlyIncomeExpenseTotal(2026, 9, 400000m, 125000.50m) },
        };
        var handler = new GetMonthlyIncomeExpensesQueryHandler(
            repository,
            new FakeCurrentUser { UserId = EmployeeId },
            new FakeDateTimeProvider(Today));

        var result = await handler.Handle(new GetMonthlyIncomeExpensesQuery(Currency.USD), CancellationToken.None);

        result.Value.Items.Single(item => item.Month == "2026-09").ShouldBe(
            new MonthlyIncomeExpenseItem("2026-09", 400000m, 125000.50m, Currency.USD));
        result.Value.Items.Count.ShouldBe(6);
    }

    [Fact]
    public async Task Expenses_by_category_uses_inclusive_custom_range_and_excludes_zero_totals()
    {
        var repository = new FakeTransactionReadRepository
        {
            CategoryExpenses = new[]
            {
                new CategoryExpenseTotal(ExpenseCategory.Alimentos, 120000m),
                new CategoryExpenseTotal(ExpenseCategory.Transporte, 0m),
            },
        };
        var handler = new GetExpensesByCategoryQueryHandler(
            repository,
            new FakeCurrentUser { UserId = EmployeeId },
            new FakeDateTimeProvider(Today));

        var result = await handler.Handle(
            new GetExpensesByCategoryQuery(
                Currency.BRL,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 10, 1)),
            CancellationToken.None);

        result.Value.Items.ShouldBe(new[]
        {
            new ExpenseCategoryItem(nameof(ExpenseCategory.Alimentos), 120000m, Currency.BRL),
        });
        repository.EmployeeId.ShouldBe(EmployeeId);
        repository.Currency.ShouldBe(Currency.BRL);
        repository.FromDate.ShouldBe(new DateOnly(2026, 4, 1));
        repository.ToDate.ShouldBe(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public async Task Chart_handlers_return_unauthorized_without_a_session_user()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = new GetExpensesByCategoryQueryHandler(
            repository,
            new FakeCurrentUser(),
            new FakeDateTimeProvider(Today));

        var result = await handler.Handle(
            new GetExpensesByCategoryQuery(Currency.ARS),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        repository.WasCalled.ShouldBeFalse();
    }

    private sealed class FakeTransactionReadRepository : ITransactionReadRepository
    {
        public IReadOnlyList<MonthlyExpenseTotal> MonthlyExpenses { get; init; } = Array.Empty<MonthlyExpenseTotal>();
        public IReadOnlyList<MonthlyIncomeExpenseTotal> MonthlyIncomeExpenses { get; init; } = Array.Empty<MonthlyIncomeExpenseTotal>();
        public IReadOnlyList<CategoryExpenseTotal> CategoryExpenses { get; init; } = Array.Empty<CategoryExpenseTotal>();
        public Guid? EmployeeId { get; private set; }
        public Currency? Currency { get; private set; }
        public DateOnly? FromDate { get; private set; }
        public DateOnly? ToDate { get; private set; }
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<MonthlyExpenseTotal>> GetMonthlyExpensesAsync(
            Guid employeeId,
            Currency currency,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default)
        {
            Capture(employeeId, currency, fromDate, toDate);
            return Task.FromResult(MonthlyExpenses);
        }

        public Task<IReadOnlyList<MonthlyIncomeExpenseTotal>> GetMonthlyIncomeExpensesAsync(
            Guid employeeId,
            Currency currency,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default)
        {
            Capture(employeeId, currency, fromDate, toDate);
            return Task.FromResult(MonthlyIncomeExpenses);
        }

        public Task<IReadOnlyList<CategoryExpenseTotal>> GetExpensesByCategoryAsync(
            Guid employeeId,
            Currency currency,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default)
        {
            Capture(employeeId, currency, fromDate, toDate);
            return Task.FromResult(CategoryExpenses);
        }

        public Task<TransactionSummary> GetSummaryAsync(
            Guid employeeId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransactionSummary(0, 0, 0, 0m, 0m, 0m, 0m));

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
            Task.FromResult(new TransactionPage(Array.Empty<Domain.Entities.Transaction>(), pageNumber, pageSize, 0));

        public Task<IReadOnlyList<Domain.Entities.Transaction>> GetFilteredAsync(
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
            Task.FromResult<IReadOnlyList<Domain.Entities.Transaction>>(Array.Empty<Domain.Entities.Transaction>());

        private void Capture(Guid employeeId, Currency currency, DateOnly fromDate, DateOnly toDate)
        {
            WasCalled = true;
            EmployeeId = employeeId;
            Currency = currency;
            FromDate = fromDate;
            ToDate = toDate;
        }
    }
}
