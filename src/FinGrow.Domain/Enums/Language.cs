namespace FinGrow.Domain.Enums;

/// <summary>
/// Idiomas de la interfaz. Los nombres son el codigo ISO 639-1 para que el valor que se guarda
/// en la base sea el mismo que usa el frontend para elegir las traducciones.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ReSharper",
    "InconsistentNaming",
    Justification = "Los nombres son codigos ISO 639-1 y se guardan tal cual en la base.")]
public enum Language
{
    es = 1,
    en = 2,
    pt = 3
}
