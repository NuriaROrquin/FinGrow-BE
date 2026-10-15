namespace FinGrow.Application.DTOs;

/// <summary>Datos corporativos visibles para la empresa autenticada.</summary>
public sealed record CompanyProfileResponse(
    string LegalName,
    string TaxId,
    string Email,
    string? Industry,
    string? CorporatePhone,
    string? FiscalAddress,
    int? FoundedYear,
    int EmployeeCount);
