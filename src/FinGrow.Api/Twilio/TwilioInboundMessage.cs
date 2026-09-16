namespace FinGrow.Api.Twilio;

using System.Globalization;
using FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

internal static class TwilioInboundMessage
{
    public static ReceiveWhatsAppMessageCommand ToCommand(IFormCollection form) =>
        new(
            TwilioWhatsAppAddress.ToPhoneNumber(form["From"].ToString()),
            form["Body"].ToString(),
            form["MessageSid"].ToString(),
            ReadMedia(form));

    private static List<WhatsAppInboundMedia> ReadMedia(IFormCollection form)
    {
        var count = int.TryParse(form["NumMedia"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

        return Enumerable.Range(0, count)
            .Select(index => ReadMedia(form, index))
            .OfType<WhatsAppInboundMedia>()
            .ToList();
    }

    private static WhatsAppInboundMedia? ReadMedia(IFormCollection form, int index) =>
        Uri.TryCreate(form[$"MediaUrl{index}"], UriKind.Absolute, out var url)
            ? new WhatsAppInboundMedia(url, form[$"MediaContentType{index}"].ToString())
            : null;
}
