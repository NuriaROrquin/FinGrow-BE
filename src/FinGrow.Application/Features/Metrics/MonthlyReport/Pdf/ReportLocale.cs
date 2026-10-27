namespace FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;

using System.Globalization;
using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

/// <summary>
/// Meses, números y fechas del reporte en el idioma de la empresa. Se escriben acá y no con
/// <see cref="CultureInfo"/> para no depender de los datos de ICU de la imagen: el mismo reporte sale
/// igual en Windows, en Docker y en los tests.
/// </summary>
internal sealed class ReportLocale
{
    public static readonly ReportLocale Spanish = new(
        new[] { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" },
        new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sept", "Oct", "Nov", "Dic" },
        (month, year) => month + " de " + year,
        decimalSeparator: ",",
        groupSeparator: ".");

    public static readonly ReportLocale English = new(
        new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" },
        new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
        (month, year) => month + " " + year,
        decimalSeparator: ".",
        groupSeparator: ",");

    public static readonly ReportLocale Portuguese = new(
        new[] { "janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro" },
        new[] { "Jan.", "Fev.", "Mar.", "Abr.", "Mai.", "Jun.", "Jul.", "Ago.", "Set.", "Out.", "Nov.", "Dez." },
        (month, year) => month + " de " + year,
        decimalSeparator: ",",
        groupSeparator: ".");

    private readonly IReadOnlyList<string> _months;
    private readonly IReadOnlyList<string> _shortMonths;
    private readonly Func<string, string, string> _monthAndYear;
    private readonly string _decimalSeparator;
    private readonly string _groupSeparator;

    private ReportLocale(
        IReadOnlyList<string> months,
        IReadOnlyList<string> shortMonths,
        Func<string, string, string> monthAndYear,
        string decimalSeparator,
        string groupSeparator)
    {
        _months = months;
        _shortMonths = shortMonths;
        _monthAndYear = monthAndYear;
        _decimalSeparator = decimalSeparator;
        _groupSeparator = groupSeparator;
    }

    public static ReportLocale For(Language language) => language switch
    {
        Language.en => English,
        Language.pt => Portuguese,
        _ => Spanish,
    };

    /// <summary>"septiembre de 2026", como <c>monthAndYear</c> del front.</summary>
    public string MonthAndYear(DateOnly date) =>
        _monthAndYear(_months[date.Month - 1], date.Year.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// "Sept" en el eje de un gráfico, igual que el panel (<c>toLocaleDateString</c> con
    /// <c>month: "short"</c> y la primera letra en mayúscula); con el año corto si la serie cruza de
    /// año ("Sept 26"), porque en el papel no hay tooltip que lo aclare.
    /// </summary>
    public string ShortMonth(DateOnly date, bool withYear) =>
        withYear
            ? _shortMonths[date.Month - 1] + " " + (date.Year % 100).ToString("00", CultureInfo.InvariantCulture)
            : _shortMonths[date.Month - 1];

    /// <summary>Con separador de miles y hasta <paramref name="maximumFractionDigits"/> decimales, como <c>formatNumber</c>.</summary>
    public string Number(decimal value, int maximumFractionDigits = 0)
    {
        var rounded = decimal.Round(value, maximumFractionDigits, MidpointRounding.AwayFromZero);
        var pattern = maximumFractionDigits == 0 ? "#,##0" : "#,##0." + new string('#', maximumFractionDigits);

        return rounded
            .ToString(pattern, CultureInfo.InvariantCulture)
            .Replace(",", "\u0001", StringComparison.Ordinal)
            .Replace(".", _decimalSeparator, StringComparison.Ordinal)
            .Replace("\u0001", _groupSeparator, StringComparison.Ordinal);
    }

    public string Number(int value) => Number((decimal)value);

    /// <summary>"26/10/2026 08:00" en la hora de Argentina, como <c>formatDateTime</c> con el formato por defecto.</summary>
    public static string DateAndTime(DateTimeOffset instant) =>
        instant.ToOffset(ArgentinaTime.UtcOffset).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
}
