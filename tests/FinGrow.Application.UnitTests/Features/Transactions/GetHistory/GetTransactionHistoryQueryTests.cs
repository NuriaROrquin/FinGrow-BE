namespace FinGrow.Application.UnitTests.Features.Transactions.GetHistory;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Application.Features.Transactions.GetTransactionSummary;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public sealed class GetTransactionHistoryQueryTests
{
    [Fact]
    public async Task Handler_filters_the_repository_by_the_authenticated_user()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, userId);

        var result = await handler.Handle(new GetTransactionHistoryQuery(new TransactionFilters()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        repository.RequestedEmployeeId.ShouldBe(userId);
    }

    [Fact]
    public async Task Handler_forwards_combined_category_and_date_filters()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, userId);

        await handler.Handle(
            new GetTransactionHistoryQuery(new TransactionFilters(
                ExpenseCategory: ExpenseCategory.Alimentos,
                DateFrom: new DateOnly(2026, 9, 1),
                DateTo: new DateOnly(2026, 9, 30))),
            CancellationToken.None);

        repository.RequestedExpenseCategory.ShouldBe(ExpenseCategory.Alimentos);
        repository.RequestedIncomeCategory.ShouldBeNull();
        repository.RequestedFromDate.ShouldBe(new DateOnly(2026, 9, 1));
        repository.RequestedToDate.ShouldBe(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public async Task Handler_forwards_status_filter()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, Guid.NewGuid());

        await handler.Handle(
            new GetTransactionHistoryQuery(new TransactionFilters(Status: TransactionStatus.Confirmed)),
            CancellationToken.None);

        repository.RequestedStatus.ShouldBe(TransactionStatus.Confirmed);
    }

    [Fact]
    public async Task Handler_maps_all_transaction_item_fields()
    {
        var transaction = Transaction.RegisterExpense(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Money.From(18500.50m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Compra supermercado",
            new DateOnly(2026, 9, 5),
            PaymentMethod.CreditCard,
            TransactionSource.MercadoPago,
            TransactionStatus.Confirmed,
            new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));
        var repository = new FakeTransactionReadRepository(new[] { transaction });
        var handler = CreateHandler(repository, transaction.EmployeeId);

        var result = await handler.Handle(new GetTransactionHistoryQuery(new TransactionFilters()), CancellationToken.None);

        var item = result.Value.Items.Single();
        item.OccurredOn.ShouldBe(new DateOnly(2026, 9, 5));
        item.Description.ShouldBe("Compra supermercado");
        item.Category.ShouldBe(nameof(ExpenseCategory.Alimentos));
        item.PaymentMethod.ShouldBe(nameof(PaymentMethod.CreditCard));
        item.Type.ShouldBe(nameof(TransactionType.Expense));
        item.Source.ShouldBe(nameof(TransactionSource.MercadoPago));
        item.Status.ShouldBe(nameof(TransactionStatus.Confirmed));
        item.Amount.ShouldBe(18500.50m);
        item.Currency.ShouldBe(nameof(Currency.ARS));
    }

    [Fact]
    public async Task Handler_orders_items_from_newest_to_oldest_date()
    {
        var older = Transaction.RegisterExpense(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Money.From(100m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Viejo",
            new DateOnly(2025, 1, 10),
            PaymentMethod.CreditCard,
            TransactionSource.MercadoPago,
            TransactionStatus.Confirmed,
            new DateTimeOffset(2025, 1, 10, 12, 0, 0, TimeSpan.Zero));

        var newer = Transaction.RegisterExpense(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Money.From(200m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Nuevo",
            new DateOnly(2026, 9, 12),
            PaymentMethod.CreditCard,
            TransactionSource.MercadoPago,
            TransactionStatus.Confirmed,
            new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));

        var repository = new FakeTransactionReadRepository(new[] { older, newer });
        var handler = CreateHandler(repository, older.EmployeeId);

        var result = await handler.Handle(new GetTransactionHistoryQuery(new TransactionFilters()), CancellationToken.None);

        result.Value.Items.Select(item => item.OccurredOn)
            .ShouldBe(new[] { newer.OccurredOn, older.OccurredOn });
    }

    [Fact]
    public async Task Handler_returns_forbidden_without_an_authenticated_user_id()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, null);

        var result = await handler.Handle(new GetTransactionHistoryQuery(new TransactionFilters()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        repository.WasCalled.ShouldBeFalse();
    }

    private static GetTransactionHistoryQueryHandler CreateHandler(
        FakeTransactionReadRepository repository,
        Guid? userId) =>
        new(repository, new FakeCurrentUser(userId));

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? CompanyId => null;

        public string? FullName => "Fake User";

        public string? Role => "Employee";

        public DateTimeOffset? ExpiresAt => DateTimeOffset.UtcNow.AddHours(1);

        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeTransactionReadRepository : ITransactionReadRepository
    {
        private readonly IReadOnlyList<Transaction> _transactions;

        public FakeTransactionReadRepository(IReadOnlyList<Transaction>? transactions = null) =>
            _transactions = transactions ?? Array.Empty<Transaction>();

        public Guid? RequestedEmployeeId { get; private set; }

        public bool WasCalled { get; private set; }

        public TransactionStatus? RequestedStatus { get; private set; }

        public ExpenseCategory? RequestedExpenseCategory { get; private set; }

        public IncomeCategory? RequestedIncomeCategory { get; private set; }

        public DateOnly? RequestedFromDate { get; private set; }

        public DateOnly? RequestedToDate { get; private set; }

        public PaymentMethod? RequestedPaymentMethod { get; private set; }

        public Task<TransactionSummary> GetSummaryAsync(
            Guid employeeId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransactionSummary(
                _transactions.Count,
                0,
                0,
                0m,
                0m,
                0m,
                0m));

        public Task<TransactionPage> GetPageAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? search,
            TransactionType? type,
            TransactionStatus? status,
            ExpenseCategory? expenseCategory,
            IncomeCategory? incomeCategory,
            PaymentMethod? paymentMethod,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            RequestedEmployeeId = employeeId;
            RequestedStatus = status;
            RequestedExpenseCategory = expenseCategory;
            RequestedIncomeCategory = incomeCategory;
            RequestedFromDate = fromDate;
            RequestedToDate = toDate;
            RequestedPaymentMethod = paymentMethod;

            return Task.FromResult(new TransactionPage(
                _transactions,
                pageNumber,
                pageSize,
                _transactions.Count));
        }
    }
}

public sealed class GetTransactionSummaryQueryTests
{
    [Fact]
    public async Task Handler_returns_summary_totals_for_all_income_and_expense_records()
    {
        var employeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var repository = new FakeTransactionReadRepository();
        var handler = new GetTransactionSummaryQueryHandler(repository, new FakeCurrentUser(employeeId));

        var result = await handler.Handle(new GetTransactionSummaryQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalIncomeArs.ShouldBe(1200m);
        result.Value.TotalIncomeUsd.ShouldBe(250m);
        result.Value.TotalIncomeTransactions.ShouldBe(2);
        result.Value.TotalExpenseArs.ShouldBe(850m);
        result.Value.TotalExpenseUsd.ShouldBe(320m);
        result.Value.TotalExpenseTransactions.ShouldBe(3);
        result.Value.TotalTransactions.ShouldBe(5);
    }

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? CompanyId => null;

        public string? FullName => "Fake User";

        public string? Role => "Employee";

        public DateTimeOffset? ExpiresAt => DateTimeOffset.UtcNow.AddHours(1);

        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeTransactionReadRepository : ITransactionReadRepository
    {
        public Task<TransactionSummary> GetSummaryAsync(
            Guid employeeId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransactionSummary(
                5,
                3,
                2,
                1200m,
                250m,
                850m,
                320m));

        public Task<TransactionPage> GetPageAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? search,
            TransactionType? type,
            TransactionStatus? status,
            ExpenseCategory? expenseCategory,
            IncomeCategory? incomeCategory,
            PaymentMethod? paymentMethod,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransactionPage(Array.Empty<Transaction>(), pageNumber, pageSize, 0));
    }
}

public sealed class GetTransactionHistoryQueryValidatorTests
{
    private readonly GetTransactionHistoryQueryValidator _validator = new();

    [Theory]
    [InlineData(TransactionType.Income)]
    [InlineData(TransactionType.Expense)]
    public async Task Validator_accepts_supported_transaction_types(TransactionType type)
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(Type: type)));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Validator_rejects_invalid_paging(int pageNumber, int pageSize)
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(pageNumber, pageSize)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_rejects_unknown_transaction_type()
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(Type: (TransactionType)999)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_rejects_unknown_status()
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(Status: (TransactionStatus)999)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_rejects_unknown_expense_category()
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(ExpenseCategory: (ExpenseCategory)999)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_rejects_unknown_income_category()
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(IncomeCategory: (IncomeCategory)999)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_rejects_reversed_date_range()
    {
        var result = await _validator.ValidateAsync(new GetTransactionHistoryQuery(new TransactionFilters(
            DateFrom: new DateOnly(2026, 10, 1),
            DateTo: new DateOnly(2026, 9, 1))));

        result.IsValid.ShouldBeFalse();
    }
}
