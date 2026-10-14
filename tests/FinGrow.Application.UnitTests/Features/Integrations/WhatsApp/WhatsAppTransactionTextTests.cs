namespace FinGrow.Application.UnitTests.Features.Integrations.WhatsApp;

using FinGrow.Application.Features.Integrations.WhatsApp;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class WhatsAppTransactionTextTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_proposal_with_a_payment_method_asks_to_answer_yes_or_no()
    {
        var proposal = WhatsAppTransactionText.Proposal(Expense(PaymentMethod.DebitCard), asksPaymentMethod: false);

        proposal.ShouldBe(
            "💸 Gasto: $ 2.500 (ARS)\n" +
            "🏷️ Categoría: Alimentos\n" +
            "📝 Súper\n" +
            "📅 12/10/2026\n" +
            "💳 Débito\n" +
            "\n" +
            "¿Lo registro? Respondé SÍ o NO.");
    }

    [Fact]
    public void A_proposal_with_an_estimated_date_marks_the_date()
    {
        var proposal = WhatsAppTransactionText.Proposal(Expense(PaymentMethod.DebitCard), asksPaymentMethod: false, dateIsEstimate: true);

        proposal.ShouldBe(
            "💸 Gasto: $ 2.500 (ARS)\n" +
            "🏷️ Categoría: Alimentos\n" +
            "📝 Súper\n" +
            "📅 12/10/2026 (estimada)\n" +
            "💳 Débito\n" +
            "\n" +
            "¿Lo registro? Respondé SÍ o NO.");
    }

    [Fact]
    public void A_proposal_without_a_payment_method_lists_numbered_options()
    {
        var proposal = WhatsAppTransactionText.Proposal(Expense(PaymentMethod.Cash), asksPaymentMethod: true);

        proposal.ShouldEndWith(
            "¿Cómo lo pagaste? Respondé con el número:\n" +
            "1. Efectivo\n" +
            "2. Débito\n" +
            "3. Crédito\n" +
            "4. Transferencia\n" +
            "5. Billetera virtual\n" +
            "\n" +
            "O respondé NO para descartarlo.");
        proposal.ShouldNotContain("💳");
    }

    [Fact]
    public void An_income_only_offers_the_payment_methods_of_an_income()
    {
        var income = Transaction.RegisterIncome(
            Guid.CreateVersion7(),
            Money.From(10000m, Currency.ARS),
            IncomeCategory.Freelance,
            "Freelance",
            DateOnly.FromDateTime(Now.UtcDateTime),
            PaymentMethod.Cash,
            TransactionSource.WhatsApp,
            TransactionStatus.Pending,
            Now,
            "whatsapp:+5491112345678:SM1");

        var proposal = WhatsAppTransactionText.Proposal(income, asksPaymentMethod: true);

        proposal.ShouldContain("¿Cómo lo cobraste? Respondé con el número:\n1. Efectivo\n2. Transferencia\n3. Billetera virtual");
    }

    private static Transaction Expense(PaymentMethod paymentMethod) => Transaction.RegisterExpense(
        Guid.CreateVersion7(),
        Money.From(2500m, Currency.ARS),
        ExpenseCategory.Alimentos,
        "Súper",
        DateOnly.FromDateTime(Now.UtcDateTime),
        paymentMethod,
        TransactionSource.WhatsApp,
        TransactionStatus.Pending,
        Now,
        "whatsapp:+5491112345678:SM1");
}
