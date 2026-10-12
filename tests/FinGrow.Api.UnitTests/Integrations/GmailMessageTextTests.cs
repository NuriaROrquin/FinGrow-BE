namespace FinGrow.Api.UnitTests.Integrations;

using System.Text;
using FinGrow.Infrastructure.Integrations.Gmail;
using Microsoft.AspNetCore.WebUtilities;

public class GmailMessageTextTests
{
    private const string BillHtml = """
        <html><head><style>td { color: red; }</style><title>Factura</title></head>
        <body>
          <p>Consult&aacute; el detalle de tu factura.</p>
          <a href="https://edenor.com/factura">Ver mi factura</a>
          <table>
            <tr><td>N&uacute;mero de cuenta</td><td>1318691348</td></tr>
            <tr><td>TOTAL A PAGAR</td><td>$73.456,92</td></tr>
            <tr><td>VENCIMIENTO</td><td>01/10/2026</td></tr>
          </table>
        </body></html>
        """;

    [Fact]
    public void An_html_bill_keeps_each_table_row_on_its_own_line()
    {
        var text = GmailMessageText.HtmlToText(BillHtml);

        text.ShouldContain("Consultá el detalle de tu factura.");
        text.ShouldContain("TOTAL A PAGAR $73.456,92");
        text.ShouldContain("VENCIMIENTO 01/10/2026");
        text.ShouldNotContain("color: red");
        text.ShouldNotContain("<");
    }

    [Fact]
    public void A_useless_plain_text_part_falls_back_to_the_html_one()
    {
        var payload = Multipart(
            Part("text/plain", "Tu cliente de correo no soporta HTML."),
            Part("text/html", BillHtml));

        GmailMessageText.Extract(payload).ShouldContain("TOTAL A PAGAR $73.456,92");
    }

    [Fact]
    public void A_complete_plain_text_part_is_preferred()
    {
        var plain = "TOTAL A PAGAR $73.456,92\n" + new string('.', 250);
        var payload = Multipart(Part("text/plain", plain), Part("text/html", "<p>otra cosa</p>"));

        GmailMessageText.Extract(payload).ShouldStartWith("TOTAL A PAGAR $73.456,92");
    }

    [Fact]
    public void Attachments_are_not_read_as_the_body()
    {
        var attachment = Part("text/plain", "contenido del adjunto") with { Filename = "factura.txt" };
        var payload = Multipart(attachment, Part("text/html", "<p>TOTAL A PAGAR $100</p>"));

        GmailMessageText.Extract(payload).ShouldBe("TOTAL A PAGAR $100");
    }

    [Fact]
    public void A_latin1_part_is_decoded_with_its_charset()
    {
        var bytes = Encoding.Latin1.GetBytes("Facturación de electricidad");
        var part = new GmailPayload(
            "text/plain",
            Filename: null,
            new List<GmailHeader> { new("Content-Type", "text/plain; charset=\"ISO-8859-1\"") },
            new GmailBody(WebEncoders.Base64UrlEncode(bytes)),
            Parts: null);

        GmailMessageText.Extract(part).ShouldBe("Facturación de electricidad");
    }

    [Theory]
    [InlineData("Edenor <facturas@Edenor.com.ar>", "edenor.com.ar")]
    [InlineData("juan.perez@gmail.com", "gmail.com")]
    [InlineData("Sin direccion", "")]
    public void Only_the_domain_of_the_sender_is_kept(string from, string expected) =>
        GmailMessageText.SenderDomain(from).ShouldBe(expected);

    private static GmailPayload Part(string mimeType, string content) => new(
        mimeType,
        Filename: string.Empty,
        new List<GmailHeader> { new("Content-Type", $"{mimeType}; charset=UTF-8") },
        new GmailBody(WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(content))),
        Parts: null);

    private static GmailPayload Multipart(params GmailPayload[] parts) => new(
        "multipart/alternative",
        Filename: string.Empty,
        Headers: null,
        new GmailBody(Data: null),
        parts.ToList());
}
