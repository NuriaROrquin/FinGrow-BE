namespace FinGrow.Application.UnitTests.Features.Integrations.WhatsApp;

using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.WhatsApp;

public class WhatsAppReviewReplyTests
{
    [Theory]
    [InlineData("SÍ")]
    [InlineData("si")]
    [InlineData(" Sí. ")]
    [InlineData("dale!")]
    [InlineData("Ok")]
    [InlineData("👍")]
    [InlineData("👍🏽")]
    public void An_affirmative_answer_confirms(string text)
    {
        WhatsAppReviewReply.Parse(text).ShouldBe(new WhatsAppReviewReply(ChatDecision.Confirm));
    }

    [Theory]
    [InlineData("NO")]
    [InlineData("no.")]
    [InlineData("Descartar")]
    [InlineData("cancelar")]
    public void A_negative_answer_discards(string text)
    {
        WhatsAppReviewReply.Parse(text).ShouldBe(new WhatsAppReviewReply(ChatDecision.Discard));
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData(" 5. ", 5)]
    public void A_single_digit_picks_an_option_and_confirms(string text, int option)
    {
        WhatsAppReviewReply.Parse(text).ShouldBe(new WhatsAppReviewReply(ChatDecision.Confirm, option));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("500")]
    [InlineData("sí, gasté 500 en el super")]
    [InlineData("Gasté $500 en el súper")]
    [InlineData("nose")]
    public void Anything_else_is_not_a_review_answer(string text)
    {
        WhatsAppReviewReply.Parse(text).ShouldBeNull();
    }
}
