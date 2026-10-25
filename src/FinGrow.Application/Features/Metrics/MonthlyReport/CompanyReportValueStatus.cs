namespace FinGrow.Application.Features.Metrics.MonthlyReport;

/// <summary>
/// Por qué un dato del reporte mensual viaja o no viaja. Un dato que falta nunca es un cero: el
/// reporte dice si no hay foto de ese mes o si el grupo era demasiado chico para mostrarlo sin
/// identificar a nadie.
/// </summary>
public enum CompanyReportValueStatus
{
    Visible = 1,
    NoData = 2,
    TooFewActiveEmployees = 3,
    TooFewScoredEmployees = 4
}
