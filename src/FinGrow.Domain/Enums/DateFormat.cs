namespace FinGrow.Domain.Enums;

/// <summary>
/// Orden en que se muestran dia, mes y anio. Es solo presentacion: las fechas viajan siempre en
/// ISO 8601 y el frontend aplica el formato.
/// </summary>
public enum DateFormat
{
    DayMonthYear = 1,
    MonthDayYear = 2,
    YearMonthDay = 3
}
