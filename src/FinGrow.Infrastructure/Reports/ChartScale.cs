namespace FinGrow.Infrastructure.Reports;

/// <summary>
/// Las marcas del eje Y de un gráfico del reporte, con el mismo criterio que recharts en el panel
/// (<c>getNiceTickValues</c> con cinco marcas): de abajo hasta un tope redondo que alcance el valor
/// más alto, en cuatro pasos iguales. Con 5 empleados el eje va de 0 a 8 y con 100 % de 0 a 100.
/// Los pasos nunca bajan de 1 porque todos los indicadores son enteros o porcentajes.
/// </summary>
internal static class ChartScale
{
    private const int Steps = 4;

    /// <param name="minimum">Dónde empieza el eje.</param>
    /// <param name="fixedMaximum">Tope fijo, como el 100 del puntaje de salud; <c>null</c> lo calcula.</param>
    /// <param name="highest">El valor más alto de las series del eje.</param>
    public static IReadOnlyList<decimal> Ticks(decimal minimum, decimal? fixedMaximum, decimal highest)
    {
        var top = fixedMaximum ?? Math.Max(highest, minimum);
        var step = Step((top - minimum) / Steps);

        return Enumerable.Range(0, Steps + 1).Select(index => minimum + (step * index)).ToList();
    }

    /// <summary>
    /// Redondea hacia arriba el paso: a décimas de su potencia de diez si tiene un dígito entero
    /// (1,25 → 2) y a múltiplos de 0,05 si no (15 → 15, 23 → 25), como <c>getFormatStep</c> de recharts.
    /// </summary>
    private static decimal Step(decimal rough)
    {
        if (rough <= 1m)
        {
            return 1m;
        }

        var digits = (int)Math.Floor(Math.Log10((double)rough)) + 1;
        var magnitude = Pow10(digits);
        var increment = digits == 1 ? 0.1m : 0.05m;

        return Math.Ceiling(rough / magnitude / increment) * increment * magnitude;
    }

    private static decimal Pow10(int exponent)
    {
        var value = 1m;

        for (var index = 0; index < exponent; index++)
        {
            value *= 10m;
        }

        return value;
    }
}
