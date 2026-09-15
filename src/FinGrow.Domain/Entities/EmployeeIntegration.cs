namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class EmployeeIntegration : AggregateRoot
{
    public const int MaxExternalAccountIdLength = 200;

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
    }

    public Guid EmployeeId { get; private set; }

    public IntegrationProvider Provider { get; private set; }

    public string ExternalAccountId { get; private set; } = string.Empty;

    public DateTimeOffset LinkedAt { get; private set; }

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
