namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

internal static class WhatsAppAddress
{
    private const string Prefix = "whatsapp:";

    public static bool IsValid(string? address) =>
        address is not null
        && address.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
        && address.Length > Prefix.Length + 1
        && address[Prefix.Length] == '+'
        && address.Skip(Prefix.Length + 1).All(char.IsAsciiDigit);

    public static string ToPhoneNumber(string address) => address[Prefix.Length..];
}
