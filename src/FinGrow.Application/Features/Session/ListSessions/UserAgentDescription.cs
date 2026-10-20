namespace FinGrow.Application.Features.Session.ListSessions;

/// <summary>
/// Saca navegador y sistema operativo de un User-Agent para mostrar la sesion. Lo que no se
/// reconoce queda en null: la pantalla dice "desconocido" en lugar de adivinar.
/// </summary>
internal static class UserAgentDescription
{
    // El orden importa: Edge y Opera tambien dicen "Chrome", Chrome tambien dice "Safari", y un
    // iPhone dice "like Mac OS X".
    private static readonly (string Token, string Name)[] Browsers =
    {
        ("Edg", "Edge"),
        ("OPR/", "Opera"),
        ("SamsungBrowser/", "Samsung Internet"),
        ("Firefox/", "Firefox"),
        ("FxiOS/", "Firefox"),
        ("CriOS/", "Chrome"),
        ("Chrome/", "Chrome"),
        ("Safari/", "Safari"),
    };

    private static readonly (string Token, string Name)[] OperatingSystems =
    {
        ("Windows", "Windows"),
        ("iPhone", "iOS"),
        ("iPad", "iPadOS"),
        ("Android", "Android"),
        ("CrOS", "ChromeOS"),
        ("Mac OS X", "macOS"),
        ("Macintosh", "macOS"),
        ("Linux", "Linux"),
    };

    public static string? BrowserOf(string? userAgent) => Match(userAgent, Browsers);

    public static string? OperatingSystemOf(string? userAgent) => Match(userAgent, OperatingSystems);

    private static string? Match(string? userAgent, (string Token, string Name)[] candidates)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        foreach (var (token, name) in candidates)
        {
            if (userAgent.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }
}
