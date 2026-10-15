namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;

public sealed class EmployeeIntegration : AggregateRoot
{
    public const int MaxExternalAccountIdLength = 200;
    public const int MaxErrorReasonLength = 300;

    private EmployeeIntegration()
    {
    }

    private EmployeeIntegration(
        Guid id,
        Guid employeeId,
        IntegrationProvider provider,
        string externalAccountId,
        DateTimeOffset linkedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Provider = provider;
        ExternalAccountId = externalAccountId;
        LinkedAt = linkedAt;
        CreatedAt = linkedAt;
        UpdatedAt = linkedAt;

        Raise(new IntegrationLinked(id, employeeId, provider, linkedAt));
    }

    public Guid EmployeeId { get; private set; }

    public IntegrationProvider Provider { get; private set; }

    public string ExternalAccountId { get; private set; } = string.Empty;

    public DateTimeOffset LinkedAt { get; private set; }

    public OAuthGrant? Grant { get; private set; }

    public IntegrationStatus Status { get; private set; } = IntegrationStatus.Active;

    public string? ErrorReason { get; private set; }

    public DateTimeOffset? LastSyncedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static EmployeeIntegration Create(
        Guid employeeId,
        IntegrationProvider provider,
        string externalAccountId,
        DateTimeOffset linkedAt)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Una integracion siempre pertenece a un empleado.");
        }

        return new EmployeeIntegration(
            Guid.CreateVersion7(),
            employeeId,
            provider,
            EnsureValidExternalAccountId(externalAccountId),
            linkedAt);
    }

    public void Relink(string externalAccountId, DateTimeOffset linkedAt)
    {
        ExternalAccountId = EnsureValidExternalAccountId(externalAccountId);
        LinkedAt = linkedAt;
        UpdatedAt = linkedAt;
        Raise(new IntegrationLinked(Id, EmployeeId, Provider, linkedAt));
    }

    public void Authorize(OAuthGrant grant, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(grant);

        if (!Provider.LinksWithOAuth())
        {
            throw new DomainException($"{Provider} no se vincula por OAuth.");
        }

        Grant = grant;
        Status = IntegrationStatus.Active;
        ErrorReason = null;
        UpdatedAt = now;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        var trimmed = (reason ?? string.Empty).Trim();

        ErrorReason = trimmed.Length switch
        {
            0 => throw new DomainException("Una integracion con error necesita el motivo para mostrarselo al empleado."),
            > MaxErrorReasonLength => throw new DomainException(
                $"El motivo del error no puede superar los {MaxErrorReasonLength} caracteres."),
            _ => trimmed
        };
        UpdatedAt = now;

        // Se avisa al pasar a error, no en cada sincronizacion que lo vuelva a encontrar asi.
        if (Status != IntegrationStatus.Error)
        {
            Status = IntegrationStatus.Error;
            Raise(new IntegrationAccessLost(Id, EmployeeId, Provider, now));
        }
    }

    public void MarkSynced(DateTimeOffset syncedAt)
    {
        LastSyncedAt = syncedAt;
        UpdatedAt = syncedAt;
    }

    private static string EnsureValidExternalAccountId(string externalAccountId)
    {
        var trimmed = (externalAccountId ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("La cuenta externa de la integracion es obligatoria."),
            > MaxExternalAccountIdLength => throw new DomainException(
                $"La cuenta externa no puede superar los {MaxExternalAccountIdLength} caracteres."),
            _ => trimmed
        };
    }
}
