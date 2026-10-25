namespace FinGrow.Application.Features.Session.ListSessions;

/// <summary>
/// Una sesion abierta. No hay ubicacion a proposito: no se registra la IP ni se geolocaliza, y la
/// pantalla no tiene que inventar una ciudad (HU-73).
/// </summary>
public sealed record ActiveSessionResponse(
    Guid Id,
    string? Browser,
    string? OperatingSystem,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActiveAt,
    bool IsCurrent);
