namespace FinGrow.Application.UnitTests.Features.Transactions.GetHistory;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetHistory;
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

        var result = await handler.Handle(new GetTransactionHistoryQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        repository.RequestedEmployeeId.ShouldBe(userId);
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

        var result = await handler.Handle(new GetTransactionHistoryQuery(), CancellationToken.None);

        var item = result.Value.Items.Single();
        item.OccurredOn.ShouldBe(new DateOnly(2026, 9, 5));
        item.Description.ShouldBe("Compra supermercado");
        item.Category.ShouldBe(nameof(ExpenseCategory.Alimentos));
        item.PaymentMethod.ShouldBe(nameof(PaymentMethod.CreditCard));
        item.Type.ShouldBe(nameof(TransactionType.Expense));
        item.Source.ShouldBe(nameof(TransactionSource.MercadoPago));
        item.Amount.ShouldBe(18500.50m);
        item.Currency.ShouldBe(nameof(Currency.ARS));
    }

    [Fact]
    public async Task Handler_returns_forbidden_without_an_authenticated_user_id()
    {
        var repository = new FakeTransactionReadRepository();
        var handler = CreateHandler(repository, null);

        var result = await handler.Handle(new GetTransactionHistoryQuery(), CancellationToken.None);

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

        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeTransactionReadRepository : ITransactionReadRepository
    {
        private readonly IReadOnlyList<Transaction> _transactions;

        public FakeTransactionReadRepository(IReadOnlyList<Transaction>? transactions = null) =>
            _transactions = transactions ?? Array.Empty<Transaction>();

        public Guid? RequestedEmployeeId { get; private set; }

        public bool WasCalled { get; private set; }

        public Task<TransactionPage> GetPageAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? search,
            TransactionType? type,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            RequestedEmployeeId = employeeId;

            return Task.FromResult(new TransactionPage(
                _transactions,
                pageNumber,
                pageSize,
                _transactions.Count,
                new Dictionary<string, decimal>(),
                new Dictionary<string, decimal>()));
        }
    }
}

public sealed class GetTransactionHistoryQueryValidatorTests
{
    private readonly GetTransactionHistoryQueryValidator _validator = new();

    [Theory]
    [InlineData("income")]
    [InlineData("expense")]
    [InlineData("ingreso")]
    [InlineData("gasto")]
    [InlineData("  GASTO  ")]
    public async Task Validator_accepts_supported_transaction_types(string type)
    {
        var result = await _validator.ValidateAsync(new GetTransactionHistoryQuery(Type: type));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(1, 20, "transfer")]
    public async Task Validator_rejects_invalid_paging_or_transaction_type(
        int pageNumber,
        int pageSize,
        string? type = null)
    {
        var result = await _validator.ValidateAsync(
            new GetTransactionHistoryQuery(pageNumber, pageSize, Type: type));

        result.IsValid.ShouldBeFalse();
    }
}