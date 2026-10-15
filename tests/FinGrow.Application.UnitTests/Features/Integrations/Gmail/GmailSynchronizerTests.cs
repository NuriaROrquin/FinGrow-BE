namespace FinGrow.Application.UnitTests.Features.Integrations.Gmail;

using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Gmail.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class GmailSynchronizerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 15, 0, 0, TimeSpan.Zero);
    private static readonly ParsedBill EdenorBill = new(
        "Edenor", 73456.92m, Currency.ARS, new DateOnly(2026, 10, 1), ExpenseCategory.Servicios, 0.95);

    private readonly FakeGmailMailboxClient _mailbox = new();
    private readonly FakeGmailOAuthClient _oauth = new();
    private readonly FakeAiService _ai = new();
    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Employee _employee = CreateEmployee();
    private readonly EmployeeIntegration _integration;

    public GmailSynchronizerTests()
    {
        _employees.Employees.Add(_employee);
        _integration = EmployeeIntegration.Create(_employee.Id, IntegrationProvider.Gmail, "juan.perez@gmail.com", Now.AddDays(-1));
        _integration.Authorize(OAuthGrant.From("access-token", "refresh-token", Now.AddMinutes(50)), Now.AddDays(-1));
        _integration.DequeueDomainEvents();
        _ai.ParseBill = email => email.Body.Contains("TOTAL A PAGAR", StringComparison.Ordinal)
            ? new ParsedBillEmail(BillParsingOutcome.Bill, EdenorBill, "claude-test")
            : ParsedBillEmail.Of(BillParsingOutcome.NotABill, "claude-test");
    }

    [Fact]
    public async Task A_bill_email_becomes_a_pending_expense_from_gmail()
    {
        Receive("m1", Now.AddHours(-2), "TOTAL A PAGAR $73.456,92\nVENCIMIENTO 01/10/2026");

        var result = await Sync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Imported.ShouldBe(1);
        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.EmployeeId.ShouldBe(_employee.Id);
        transaction.Type.ShouldBe(TransactionType.Expense);
        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.Source.ShouldBe(TransactionSource.Gmail);
        transaction.Amount.ShouldBe(Money.From(73456.92m, Currency.ARS));
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Servicios);
        transaction.Description.ShouldBe("Factura Edenor (vence 01/10/2026)");
        transaction.OccurredOn.ShouldBe(new DateOnly(2026, 10, 11));
        transaction.PaymentMethod.ShouldBe(GmailBillMapper.DefaultPaymentMethod);
        transaction.ExternalReference.ShouldBe("gmail:edenor:2026-10-01:73456.92");
        _integration.LastSyncedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Only_the_sender_domain_and_the_text_reach_the_ai()
    {
        Receive("m1", Now.AddHours(-2), new string('x', GmailSynchronizer.MaxBodyLength + 500), senderDomain: "gmail.com");

        await Sync();

        var (email, currency) = _ai.ParsedEmails.ShouldHaveSingleItem();
        email.Sender.ShouldBe("gmail.com");
        email.Subject.ShouldBe("Tu factura");
        email.Body.Length.ShouldBe(GmailSynchronizer.MaxBodyLength);
        currency.ShouldBe(_employee.PreferredCurrency);
    }

    [Fact]
    public async Task The_original_and_a_forward_of_the_same_bill_are_proposed_once()
    {
        Receive("original", Now.AddDays(-3), "TOTAL A PAGAR $73.456,92", senderDomain: "edenor.com.ar");
        Receive("forward", Now.AddDays(-1), "---------- Forwarded message ---------\nTOTAL A PAGAR $73.456,92", senderDomain: "gmail.com");

        var result = await Sync();

        _transactions.Transactions.ShouldHaveSingleItem();
        result.Value.Imported.ShouldBe(1);
        result.Value.AlreadyKnown.ShouldBe(1);
    }

    [Fact]
    public async Task A_bill_already_proposed_in_an_earlier_run_is_not_proposed_again()
    {
        Receive("m1", Now.AddHours(-2), "TOTAL A PAGAR $73.456,92");
        await Sync();
        Receive("m2", Now.AddHours(-1), "Fwd: TOTAL A PAGAR $73.456,92");

        await Sync();

        _transactions.Transactions.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Emails_that_are_not_bills_are_ignored()
    {
        Receive("m1", Now.AddHours(-2), "Tu turno con el medico vence el viernes");

        var result = await Sync();

        result.Value.NotBills.ShouldBe(1);
        _transactions.Transactions.ShouldBeEmpty();
        _integration.LastSyncedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task A_ride_receipt_is_not_proposed_even_if_the_ai_takes_it_for_a_bill()
    {
        var ride = new ParsedBill("Cabify", 8500m, Currency.ARS, DueDate: null, ExpenseCategory.Transporte, 0.9);
        _ai.ParseBill = _ => new ParsedBillEmail(BillParsingOutcome.Bill, ride, "claude-test");
        Receive("m1", Now.AddHours(-2), "Gracias por viajar con Cabify. Total $8.500");

        var result = await Sync();

        result.Value.Imported.ShouldBe(0);
        result.Value.NotBills.ShouldBe(1);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_first_sync_looks_back_thirty_days_and_later_ones_continue_from_the_last()
    {
        await Sync();
        _integration.MarkSynced(Now.AddHours(-1));
        await Sync();

        var first = Now.Subtract(GmailSynchronizer.InitialLookback).Subtract(GmailSynchronizer.Overlap).ToUnixTimeSeconds();
        var second = Now.AddHours(-1).Subtract(GmailSynchronizer.Overlap).ToUnixTimeSeconds();
        _mailbox.Searches[0].Query.ShouldEndWith($"after:{first}");
        _mailbox.Searches[1].Query.ShouldEndWith($"after:{second}");
        _mailbox.Searches[0].Query.ShouldContain("factura");
        _mailbox.Searches[0].Query.ShouldContain("naturgy");
    }

    [Fact]
    public async Task A_big_backlog_is_read_oldest_first_and_resumed_on_the_next_run()
    {
        _mailbox.PageSize = 10;

        for (var minute = 1; minute <= GmailSynchronizer.MaxMessagesPerRun + 5; minute++)
        {
            Receive($"m{minute}", Now.AddDays(-2).AddMinutes(minute), "nada");
        }

        var result = await Sync();

        result.Value.Remaining.ShouldBe(5);
        _mailbox.ReadMessages[0].ShouldBe("m1");
        _mailbox.ReadMessages.Count.ShouldBe(GmailSynchronizer.MaxMessagesPerRun);
        _integration.LastSyncedAt.ShouldBe(Now.AddDays(-2).AddMinutes(GmailSynchronizer.MaxMessagesPerRun));
    }

    [Fact]
    public async Task An_access_token_about_to_expire_is_renewed_keeping_the_refresh_token()
    {
        _integration.Authorize(OAuthGrant.From("old-access", "refresh-token", Now.AddMinutes(2)), Now.AddDays(-1));
        _oauth.Tokens = new GmailTokens("new-access", RefreshToken: null, TimeSpan.FromHours(1), CanReadMail: true);

        await Sync();

        _oauth.RefreshedTokens.ShouldHaveSingleItem().ShouldBe("refresh-token");
        _integration.Grant!.AccessToken.ShouldBe("new-access");
        _integration.Grant.RefreshToken.ShouldBe("refresh-token");
        _mailbox.Searches.ShouldHaveSingleItem().AccessToken.ShouldBe("new-access");
    }

    [Fact]
    public async Task A_rejected_access_token_is_renewed_and_the_search_retried_once()
    {
        _mailbox.RejectedTokens.Add("access-token");
        _oauth.Tokens = new GmailTokens("new-access", RefreshToken: null, TimeSpan.FromHours(1), CanReadMail: true);

        var result = await Sync();

        result.IsSuccess.ShouldBeTrue();
        _mailbox.Searches.ShouldHaveSingleItem().AccessToken.ShouldBe("new-access");
        _integration.Status.ShouldBe(IntegrationStatus.Active);
    }

    [Fact]
    public async Task When_the_employee_revokes_access_the_integration_fails_and_announces_it()
    {
        _mailbox.RejectedTokens.Add("access-token");
        _oauth.RefreshFailure = new HttpRequestException(
            "Google respondio 400 al pedir el token (refresh_token): {\"error\": \"invalid_grant\"}", inner: null, HttpStatusCode.BadRequest);

        var result = await Sync();

        result.Error.ShouldBe(GmailSynchronizer.AccessLost);
        _integration.Status.ShouldBe(IntegrationStatus.Error);
        _integration.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<IntegrationAccessLost>();
        _unitOfWork.SaveCount.ShouldBe(1);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_google_outage_is_not_blamed_on_the_employee()
    {
        _integration.Authorize(OAuthGrant.From("old-access", "refresh-token", Now.AddMinutes(2)), Now.AddDays(-1));
        _oauth.RefreshFailure = new HttpRequestException("Google respondio 503", inner: null, HttpStatusCode.ServiceUnavailable);

        await Should.ThrowAsync<HttpRequestException>(() => Sync());

        _integration.Status.ShouldBe(IntegrationStatus.Active);
    }

    [Fact]
    public async Task A_failed_integration_is_not_synchronized()
    {
        _integration.MarkFailed("Sin acceso", Now.AddHours(-1));
        Receive("m1", Now.AddHours(-2), "TOTAL A PAGAR $73.456,92");

        var result = await Sync();

        result.Error.ShouldBe(GmailSynchronizer.AccessLost);
        _mailbox.Searches.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_ai_is_down_the_watermark_stays_so_no_email_is_skipped()
    {
        _integration.MarkSynced(Now.AddHours(-3));
        _ai.Unreachable = true;
        Receive("m1", Now.AddHours(-2), "TOTAL A PAGAR $73.456,92");

        var result = await Sync();

        result.Error.ShouldBe(GmailSynchronizer.AiUnavailable);
        _integration.LastSyncedAt.ShouldBe(Now.AddHours(-3));
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_a_linked_gmail_there_is_nothing_to_sync()
    {
        var mercadoPago = EmployeeIntegration.Create(_employee.Id, IntegrationProvider.MercadoPago, "123", Now);

        var result = await Synchronizer().SyncAsync(mercadoPago, CancellationToken.None);

        result.Error.ShouldBe(GmailSynchronizer.NotLinked);
    }

    private void Receive(string id, DateTimeOffset receivedAt, string body, string senderDomain = "edenor.com.ar") =>
        _mailbox.Messages.Add(new GmailMessage(id, "Tu factura", senderDomain, receivedAt, body));

    private Task<Result<GmailSyncSummary>> Sync() => Synchronizer().SyncAsync(_integration, CancellationToken.None);

    private GmailSynchronizer Synchronizer() => new(
        _mailbox,
        _oauth,
        _ai,
        _employees,
        _transactions,
        _unitOfWork,
        new FakeDateTimeProvider(Now),
        NullLogger<GmailSynchronizer>.Instance);

    private static Employee CreateEmployee() => Employee.Create(
        Guid.CreateVersion7(),
        departmentId: null,
        "Juan Perez",
        Email.From("juan.perez@empresa.com"),
        null,
        "hash-bcrypt",
        Currency.ARS,
        new DateOnly(2026, 1, 15),
        Now);
}
