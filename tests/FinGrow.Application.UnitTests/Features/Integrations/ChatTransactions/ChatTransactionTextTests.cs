namespace FinGrow.Application.UnitTests.Features.Integrations.ChatTransactions;

using FinGrow.Application.Features.Integrations.ChatTransactions;

public class ChatTransactionTextTests
{
    [Fact]
    public void What_was_heard_in_a_voice_note_is_quoted()
    {
        ChatTransactionText.Heard("Gasté 2500 en el súper").ShouldBe("🎙️ Entendí: «Gasté 2500 en el súper»");
    }
}
