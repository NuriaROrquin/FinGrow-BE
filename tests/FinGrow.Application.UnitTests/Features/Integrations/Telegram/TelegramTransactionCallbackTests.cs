namespace FinGrow.Application.UnitTests.Features.Integrations.Telegram;

using System.Text;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Telegram;
using FinGrow.Domain.Enums;

public class TelegramTransactionCallbackTests
{
    private static readonly Guid TransactionId = Guid.CreateVersion7();

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, PaymentMethod.Cash)]
    [InlineData(false, PaymentMethod.DigitalWallet)]
    public void A_callback_survives_the_round_trip_through_telegram(bool discard, PaymentMethod? paymentMethod)
    {
        var decision = discard ? ChatDecision.Discard : ChatDecision.Confirm;
        var callback = new TelegramTransactionCallback(TransactionId, decision, paymentMethod);

        TelegramTransactionCallback.Parse(callback.Encode()).ShouldBe(callback);
    }

    [Fact]
    public void The_encoded_data_fits_in_the_64_bytes_telegram_allows()
    {
        var callback = new TelegramTransactionCallback(TransactionId, ChatDecision.Confirm, PaymentMethod.DigitalWallet);

        Encoding.UTF8.GetByteCount(callback.Encode()).ShouldBeLessThanOrEqualTo(64);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tx:ok")]
    [InlineData("tx:ok:no-es-un-guid")]
    [InlineData("otro:ok:0199a3b2c4d5e6f708192a3b4c5d6e7f")]
    [InlineData("tx:pm99:0199a3b2c4d5e6f708192a3b4c5d6e7f")]
    [InlineData("tx:pm-1:0199a3b2c4d5e6f708192a3b4c5d6e7f")]
    [InlineData("tx:borrar:0199a3b2c4d5e6f708192a3b4c5d6e7f")]
    public void Data_that_was_not_produced_by_the_bot_is_rejected(string? data)
    {
        TelegramTransactionCallback.Parse(data).ShouldBeNull();
    }
}
