namespace FinGrow.Domain.Entities;

using System.Security.Cryptography;
using System.Text;
using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class IntegrationLinkCode : AggregateRoot
{
    public const int Length = 8;
    public const int HashLength = 64;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private IntegrationLinkCode()
    {
    }

    private IntegrationLinkCode(
        Guid id,
        Guid employeeId,
        IntegrationProvider provider,
        string codeHash,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Provider = provider;
        CodeHash = codeHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt.Add(Lifetime);
    }

    public Guid EmployeeId { get; private set; }

    public IntegrationProvider Provider { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static IntegrationLinkCode Create(
        Guid employeeId,
        IntegrationProvider provider,
        string code,
        DateTimeOffset createdAt)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Un codigo de vinculacion siempre pertenece a un empleado.");
        }

        var normalized = Normalize(code);

        if (normalized.Length != Length)
        {
            throw new DomainException($"El codigo de vinculacion tiene {Length} caracteres.");
        }

        return new IntegrationLinkCode(Guid.CreateVersion7(), employeeId, provider, Hash(normalized), createdAt);
    }

    public static string GenerateCode() => RandomNumberGenerator.GetString(Alphabet, Length);

    public static string Normalize(string code) =>
        (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();

    public static string Hash(string code) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(code))));

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

    public void Redeem(DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            throw new DomainException("El codigo de vinculacion ya fue usado.");
        }

        if (now >= ExpiresAt)
        {
            throw new DomainException("El codigo de vinculacion vencio.");
        }

        UsedAt = now;
    }
}
