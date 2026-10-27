namespace FinGrow.Application.UnitTests.Features.Transactions.GetHistory;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Application.Features.Transactions.GetTransactionSummary;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
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
    public async Task Each_item_says_which_receipt_it_has()
    {
        var employeeId = Guid.CreateVersion7();
        var withReceipt = NewHistoryExpense(employeeId);
        var withoutReceipt = NewHistoryExpense(employeeId);
        var receipts = new FakeTransactionReceiptRepository();
        var receipt = TransactionReceipt.Upload(employeeId, "image/png", 10, null, DateTimeOffset.UnixEpoch);
        receipt.AttachTo(withReceipt);
        receipts.Receipts.Add(receipt);
        var handler = CreateHandler(new FakeTransactionReadRepository(new[] { withReceipt, withoutReceipt }), employeeId, receipts);

        var result = await handler.Handle(new GetTransactionHistoryQuery(new TransactionFilters()), CancellationToken.None);

        result.Value.Items.Single(item => item.Id == withReceipt.Id).ReceiptId.ShouldBe(receipt.Id);
        result.Value.Items.Single(item => item.Id == withoutReceipt.Id).ReceiptId.ShouldBeNull();
    }

    [Fact]
    public async Task Handler_sorts_by_most_recent_date_by_default()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, Guid.NewGuid());

        await handler.Handle(new GetTransactionHistoryQuery(new TransactionFilters()), CancellationToken.None);

        repository.RequestedSortBy.ShouldBe(TransactionSortField.OccurredOn);
        repository.RequestedSortDirection.ShouldBe(SortDirection.Descending);
    }

    [Fact]
    public async Task Handler_forwards_the_requested_sort_along_with_the_filters()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, Guid.NewGuid());

        await handler.Handle(
            new GetTransactionHistoryQuery(new TransactionFilters(
                PaymentMethod: PaymentMethod.Cash,
                SortBy: TransactionSortField.Amount,
                SortDirection: SortDirection.Ascending)),
            CancellationToken.None);

        repository.RequestedSortBy.ShouldBe(TransactionSortField.Amount);
        repository.RequestedSortDirection.ShouldBe(SortDirection.Ascending);
        repository.RequestedPaymentMethod.ShouldBe(PaymentMethod.Cash);
    }

    [Fact]
    public async Task Handler_keeps_the_order_the_repository_resolved()
    {
        var employeeId = Guid.CreateVersion7();
        var older = NewHistoryExpense(employeeId, new DateOnly(2026, 1, 1));
        var newer = NewHistoryExpense(employeeId, new DateOnly(2026, 9, 1));
        var handler = CreateHandler(new FakeTransactionReadRepository(new[] { older, newer }), employeeId);

        var result = await handler.Handle(
            new GetTransactionHistoryQuery(new TransactionFilters(SortDirection: SortDirection.Ascending)),
            CancellationToken.None);

        result.Value.Items.Select(item => item.Id).ShouldBe(new[] { older.Id, newer.Id });
    }

    private static Transaction NewHistoryExpense(Guid employeeId, DateOnly? occurredOn = null) => Transaction.RegisterExpense(
        employeeId,
        Money.From(1000m, Currency.ARS),
        ExpenseCategory.Alimentos,
        "Supermercado",
        occurredOn ?? new DateOnly(2026, 9, 1),
        PaymentMethod.Cash,
        TransactionSource.Manual,
        TransactionStatus.Confirmed,
        DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task Handler_uses_confirmed_status_by_default()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, Guid.NewGuid());

        await handler.Handle(new GetTransactionHistoryQuery(new TransactionFilters()), CancellationToken.None);

        repository.RequestedStatuses.ShouldBe(new[] { TransactionStatus.Confirmed });
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
            new GetTransactionHistoryQuery(new TransactionFilters(
                Status: new[] { TransactionStatus.Confirmed, TransactionStatus.Eliminated })),
            CancellationToken.None);

        repository.RequestedStatuses.ShouldBe(new[] { TransactionStatus.Confirmed, TransactionStatus.Eliminated });
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
        Guid? userId,
        FakeTransactionReceiptRepository? receipts = null) =>
        new(repository, receipts ?? new FakeTransactionReceiptRepository(), new FakeCurrentUser(userId));

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? CompanyId => null;

        public string? FullName => "Fake User";

        public string? Role => "Employee";

        public Guid? SessionId => null;

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

        public IReadOnlyCollection<TransactionStatus> RequestedStatuses { get; private set; } = Array.Empty<TransactionStatus>();

        public ExpenseCategory? RequestedExpenseCategory { get; private set; }

        public IncomeCategory? RequestedIncomeCategory { get; private set; }

        public DateOnly? RequestedFromDate { get; private set; }

        public DateOnly? RequestedToDate { get; private set; }

        public PaymentMethod? RequestedPaymentMethod { get; private set; }

        public TransactionSortField? RequestedSortBy { get; private set; }

        public SortDirection? RequestedSortDirection { get; private set; }

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
            IReadOnlyCollection<TransactionStatus> statuses,
            ExpenseCategory? expenseCategory,
            IncomeCategory? incomeCategory,
            PaymentMethod? paymentMethod,
            DateOnly? fromDate,
            DateOnly? toDate,
            TransactionSortField sortBy,
            SortDirection sortDirection,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            RequestedEmployeeId = employeeId;
            RequestedStatuses = statuses;
            RequestedExpenseCategory = expenseCategory;
            RequestedIncomeCategory = incomeCategory;
            RequestedFromDate = fromDate;
            RequestedToDate = toDate;
            RequestedPaymentMethod = paymentMethod;
            RequestedSortBy = sortBy;
            RequestedSortDirection = sortDirection;

            return Task.FromResult(new TransactionPage(
                _transactions,
                pageNumber,
                pageSize,
                _transactions.Count));
        }

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
            Task.FromResult(_transactions);
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

        public Guid? SessionId => null;

        public DateTimeOffset? ExpiresAt => DateTimeOffset.UtcNow.AddHours(1);

        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeTransactionReadRepository : ITransactionReadRepository
    {
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
            IReadOnlyCollection<TransactionStatus> statuses,
            ExpenseCategory? expenseCategory,
            IncomeCategory? incomeCategory,
            PaymentMethod? paymentMethod,
            DateOnly? fromDate,
            DateOnly? toDate,
            TransactionSortField sortBy,
            SortDirection sortDirection,
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
            new GetTransactionHistoryQuery(new TransactionFilters(Status: new[] { (TransactionStatus)999 })));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_rejects_unknown_sort_field()
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(SortBy: (TransactionSortField)999)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_rejects_unknown_sort_direction()
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(new TransactionFilters(SortDirection: (SortDirection)999)));

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
