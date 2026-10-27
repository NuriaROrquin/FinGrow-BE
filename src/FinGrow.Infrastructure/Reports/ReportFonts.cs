namespace FinGrow.Infrastructure.Reports;

using System.Collections.Concurrent;
using PdfSharp.Fonts;

/// <summary>
/// La tipografía del PDF del reporte: Liberation Sans (licencia SIL OFL, ver <c>Resources/Fonts/OFL.txt</c>),
/// que viaja con la API. PDFsharp no busca fuentes del sistema fuera de Windows y la imagen de Docker no
/// trae ninguna: sin esto el reporte saldría distinto en cada entorno, o no saldría.
/// </summary>
internal static class ReportFonts
{
    public const string FamilyName = "Liberation Sans";

    private const string RegularFace = "LiberationSans-Regular";
    private const string BoldFace = "LiberationSans-Bold";

    private static readonly object Gate = new();
    private static readonly ConcurrentDictionary<string, byte[]> Files = new(StringComparer.Ordinal);

    public static string RegularPath => PathOf(RegularFace);

    public static string BoldPath => PathOf(BoldFace);

    /// <summary>PDFsharp permite fijar el resolver una sola vez por proceso.</summary>
    public static void EnsureRegistered()
    {
        lock (Gate)
        {
            GlobalFontSettings.FontResolver ??= new Resolver();
        }
    }

    private static string PathOf(string face) =>
        Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", face + ".ttf");

    /// <summary>Cualquier familia que pida MigraDoc sale con Liberation Sans; la cursiva se simula.</summary>
    private sealed class Resolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
            new(bold ? BoldFace : RegularFace, false, italic);

        public byte[]? GetFont(string faceName) =>
            Files.GetOrAdd(faceName, face => File.ReadAllBytes(PathOf(face)));
    }
}
