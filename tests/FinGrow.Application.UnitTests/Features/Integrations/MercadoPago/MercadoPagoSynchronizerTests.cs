namespace FinGrow.Application.UnitTests.Features.Integrations.MercadoPago;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.MercadoPago.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class MercadoPagoSynchronizerTests
{
    private const string MercadoPagoUserId = "228085066";
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeMercadoPagoPaymentsClient _payments = new();
    private readonly FakeMercadoPagoOAuthClient _oauth = new();
    private readonly FakeAiService _ai = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly EmployeeIntegration _integration;

    public MercadoPagoSynchronizerTests()
    {
        _integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.MercadoPago, MercadoPagoUserId, Now.AddDays(-1));
        _integration.Authorize(OAuthGrant.From("access", "refresh", Now.AddDays(170)), Now.AddDays(-1));
    }

    [Fact]
    public async Task A_payment_made_by_the_employee_becomes_a_pending_expense()
    {
        _payments.Payments.Add(Purchase(1, "SWEATER MILA (Verde)", 107940m, "debit_card"));

        var result = await Sync();

        result.Value.Imported.ShouldBe(1);
        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.Type.ShouldBe(TransactionType.Expense);
        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.Source.ShouldBe(TransactionSource.MercadoPago);
        transaction.ExternalReference.ShouldBe("1");
        transaction.Amount.ShouldBe(Money.From(107940m, Currency.ARS));
        transaction.Description.ShouldBe("SWEATER MILA (Verde)");
        transaction.PaymentMethod.ShouldBe(PaymentMethod.DebitCard);
        transaction.EmployeeId.ShouldBe(EmployeeId);
    }

    [Fact]
    public async Task A_payment_collected_by_the_employee_becomes_a_pending_income()
    {
        _payments.Payments.Add(Received(2, 6000m));

        await Sync();

        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.Type.ShouldBe(TransactionType.Income);
        transaction.IncomeCategory.ShouldBe(IncomeCategory.Otros);
        transaction.PaymentMethod.ShouldBe(PaymentMethod.DigitalWallet);
    }

    [Fact]
    public async Task Money_moved_in_from_the_employees_own_bank_is_not_an_income()
    {
        _payments.Payments.Add(Received(3, 100000m) with { OperationType = "account_fund", PaymentTypeId = "bank_transfer" });

        var result = await Sync();

        result.Value.Ignored.ShouldBe(1);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("rejected")]
    [InlineData("refunded")]
    public async Task Only_approved_payments_are_imported(string status)
    {
        _payments.Payments.Add(Purchase(4, "Canva", 10285m, "debit_card") with { Status = status });

        var result = await Sync();

        result.Value.Ignored.ShouldBe(1);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_payment_already_imported_is_not_loaded_twice()
    {
        _payments.Payments.Add(Purchase(5, "Canva", 10285m, "debit_card"));
        await Sync();

        var second = await Sync();

        second.Value.Imported.ShouldBe(0);
        second.Value.AlreadyKnown.ShouldBe(1);
        _transactions.Transactions.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task The_occurred_date_is_the_approval_date_in_argentina()
    {
        _payments.Payments.Add(Purchase(6, "Cena", 5000m, "credit_card") with
        {
            DateApproved = new DateTimeOffset(2026, 9, 10, 1, 30, 0, TimeSpan.Zero),
        });

        await Sync();

        _transactions.Transactions.Single().OccurredOn.ShouldBe(new DateOnly(2026, 9, 9));
    }

    [Fact]
    public async Task Expenses_get_the_category_proposed_by_the_ai()
    {
        _ai.CategoriesByDescription["Mini Escalador Stepper"] = ExpenseCategory.Salud;
        _payments.Payments.Add(Purchase(7, "Mini Escalador Stepper", 72382.11m, "debit_card"));
        _payments.Payments.Add(Received(8, 3400m));

        await Sync();

        _transactions.Transactions.Single(transaction => transaction.Type == TransactionType.Expense)
            .ExpenseCategory.ShouldBe(ExpenseCategory.Salud);
        _ai.Received.ShouldHaveSingleItem().Amount.ShouldBe(-72382.11m);
    }

    [Fact]
    public async Task When_the_ai_is_down_expenses_are_still_imported_as_otros()
    {
        _ai.Unreachable = true;
        _payments.Payments.Add(Purchase(9, "Varios", 3400m, "account_money"));

        var result = await Sync();

        result.IsSuccess.ShouldBeTrue();
        _transactions.Transactions.Single().ExpenseCategory.ShouldBe(ExpenseCategory.Otros);
    }

    [Fact]
    public async Task The_first_sync_looks_back_ninety_days_and_later_ones_resume_with_an_overlap()
    {
        await Sync();
        _clock.UtcNow = Now.AddHours(1);

        await Sync();

        _payments.Searches[0].From.ShouldBe(Now.Subtract(MercadoPagoSynchronizer.InitialLookback).Subtract(MercadoPagoSynchronizer.Overlap));
        _payments.Searches[1].From.ShouldBe(Now.Subtract(MercadoPagoSynchronizer.Overlap));
        _payments.Searches[1].To.ShouldBe(Now.AddHours(1));
        _integration.LastSyncedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public async Task All_pages_are_read()
    {
        _payments.PageSize = 2;
        _payments.Payments.AddRange(Enumerable.Range(10, 5).Select(id => Purchase(id, $"Compra {id}", 100m, "debit_card")));

        var result = await Sync();

        result.Value.Imported.ShouldBe(5);
        _payments.Searches.Count.ShouldBe(3);
    }

    [Fact]
    public async Task A_grant_about_to_expire_is_refreshed_before_syncing()
    {
        _integration.Authorize(OAuthGrant.From("old-access", "old-refresh", Now.AddDays(3)), Now);
        _oauth.Tokens = new MercadoPagoTokens("new-access", "new-refresh", TimeSpan.FromDays(180), MercadoPagoUserId);

        var result = await Sync();

        result.IsSuccess.ShouldBeTrue();
        _integration.Grant!.AccessToken.ShouldBe("new-access");
        _integration.Grant.ExpiresAt.ShouldBe(Now.AddDays(180));
    }

    [Fact]
    public async Task An_integration_without_a_grant_cannot_be_synced()
    {
        var unauthorized = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.MercadoPago, MercadoPagoUserId, Now);

        var result = await Synchronizer().SyncAsync(unauthorized, CancellationToken.None);

        result.Error.ShouldBe(MercadoPagoSynchronizer.NotAuthorized);
    }

    private Task<Result<MercadoPagoSyncSummary>> Sync() => Synchronizer().SyncAsync(_integration, CancellationToken.None);

    private MercadoPagoSynchronizer Synchronizer() => new(
        _payments, _oauth, _ai, _transactions, _unitOfWork, _clock, NullLogger<MercadoPagoSynchronizer>.Instance);

    private static MercadoPagoPayment Purchase(long id, string description, decimal amount, string paymentType) => new(
        id, "approved", "regular_payment", amount, "ARS", description, paymentType,
        CollectorId: null, PayerId: null, Now.AddDays(-2), Now.AddDays(-2));

    private static MercadoPagoPayment Received(long id, decimal amount) => new(
        id, "approved", "money_transfer", amount, "ARS", "Varios", "account_money",
        CollectorId: 228085066, PayerId: 169780195, Now.AddDays(-2), Now.AddDays(-2));
}
