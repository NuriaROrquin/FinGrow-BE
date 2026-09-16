namespace FinGrow.Api.Twilio;

internal static class TwilioWhatsAppAddress
{
    private const string Prefix = "whatsapp:";

    public static string ToPhoneNumber(string address) =>
        address.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) ? address[Prefix.Length..] : address;
}
