namespace FinGrow.Application.Features.Integrations.Gmail.Sync;

using System.Globalization;

/// <summary>
/// Primer filtro: lo resuelve el buscador de Gmail. Busca por contenido y no por remitente
/// para que un reenvio (el remitente es el propio empleado) tambien entre. Quien decide si de verdad
/// es una factura es FinGrow-AI.
/// </summary>
internal static class GmailBillSearch
{
    internal static readonly IReadOnlyList<string> Keywords = new[]
    {
        "factura",
        "\"total a pagar\"",
        "\"importe a pagar\"",
        "\"monto a pagar\"",
        "vencimiento",
        "\"resumen de cuenta\"",

        // Empresas cuyos mails no siempre dicen "factura" en el texto.
        "naturgy",
    };

    public static string Query(DateTimeOffset since) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"({string.Join(" OR ", Keywords)}) -category:promotions -category:social after:{since.ToUnixTimeSeconds()}");
}
