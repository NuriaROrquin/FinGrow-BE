namespace FinGrow.Application.UnitTests.Features.Integrations.Gmail;

using FinGrow.Application.Features.Integrations.Gmail.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;

public class GmailBillMapperTests
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 10, 1, 2, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("MetroGAS")]
    [InlineData("Metrogas")]
    [InlineData("metrogás")]
    [InlineData(" Metro-Gas ")]
    public void The_same_company_written_differently_gets_the_same_key(string issuer) =>
        GmailBillMapper.IssuerKey(issuer).ShouldBe("metrogas");

    [Fact]
    public void A_bill_is_identified_by_company_due_date_and_amount()
    {
        var bill = new ParsedBill("AySA", 15200.5m, Currency.ARS, new DateOnly(2026, 10, 20), ExpenseCategory.Servicios, 0.9);

        GmailBillMapper.ExternalReference(bill, ReceivedAt).ShouldBe("gmail:aysa:2026-10-20:15200.50");
    }

    [Fact]
    public void Without_a_due_date_the_month_the_email_arrived_in_argentina_is_used()
    {
        var bill = new ParsedBill("Personal", 9999m, Currency.ARS, DueDate: null, ExpenseCategory.Servicios, 0.9);

        GmailBillMapper.ExternalReference(bill, ReceivedAt).ShouldBe("gmail:personal:sin-vencimiento-2026-09:9999.00");
        GmailBillMapper.Description(bill).ShouldBe("Factura Personal");
    }
}
