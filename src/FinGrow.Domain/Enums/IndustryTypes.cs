namespace FinGrow.Domain.Enums;

/// <summary>Opciones de industria disponibles para clasificar una empresa.</summary>
public static class IndustryTypes
{
    public const string Technology = "Tecnología";
    public const string FinanceAndInsurance = "Finanzas y seguros";
    public const string Commerce = "Comercio";
    public const string Manufacturing = "Industria y manufactura";
    public const string Healthcare = "Salud";
    public const string Education = "Educación";
    public const string Construction = "Construcción";
    public const string AgricultureAndLivestock = "Agricultura y ganadería";
    public const string ProfessionalServices = "Servicios profesionales";
    public const string Other = "Otro";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Technology,
        FinanceAndInsurance,
        Commerce,
        Manufacturing,
        Healthcare,
        Education,
        Construction,
        AgricultureAndLivestock,
        ProfessionalServices,
        Other,
    };

    public static bool IsValid(string value) =>
        All.Contains(value, StringComparer.Ordinal);
}
