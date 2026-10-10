namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class TransactionReceipt : AggregateRoot
{
    public const int MaxStorageKeyLength = 200;
    public const int MaxContentTypeLength = 100;
    public const int MaxFileNameLength = 255;

    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, byte[]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
        ["image/png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
        ["application/pdf"] = "%PDF-"u8.ToArray(),
    };

    private TransactionReceipt()
    {
    }

    private TransactionReceipt(
        Guid id,
        Guid employeeId,
        string contentType,
        long sizeBytes,
        string? fileName,
        DateTimeOffset uploadedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        StorageKey = $"receipts/{employeeId:N}/{id:N}";
        ContentType = contentType;
        SizeBytes = sizeBytes;
        FileName = fileName;
        UploadedAt = uploadedAt;
    }

    public Guid EmployeeId { get; private set; }

    public Guid? TransactionId { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string? FileName { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public static IReadOnlyCollection<string> AllowedContentTypes => Signatures.Keys;

    public static bool IsSupportedContentType(string? contentType) =>
        contentType is not null && Signatures.ContainsKey(contentType);

    public static bool MatchesSignature(string contentType, ReadOnlySpan<byte> content) =>
        Signatures.TryGetValue(contentType, out var signature) && content.StartsWith(signature);

    public static TransactionReceipt Upload(
        Guid employeeId,
        string contentType,
        long sizeBytes,
        string? fileName,
        DateTimeOffset uploadedAt)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Un comprobante siempre pertenece a un empleado.");
        }

        if (!IsSupportedContentType(contentType))
        {
            throw new DomainException($"El tipo de archivo '{contentType}' no se admite como comprobante.");
        }

        if (sizeBytes is <= 0 or > MaxSizeBytes)
        {
            throw new DomainException(
                $"Un comprobante tiene que pesar más de 0 bytes y como máximo {MaxSizeBytes / (1024 * 1024)} MB.");
        }

        return new TransactionReceipt(
            Guid.CreateVersion7(),
            employeeId,
            contentType.ToLowerInvariant(),
            sizeBytes,
            NormalizeFileName(fileName),
            uploadedAt);
    }

    public void AttachTo(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.EmployeeId != EmployeeId)
        {
            throw new DomainException("Un comprobante solo se asocia a un movimiento del mismo empleado.");
        }

        TransactionId = transaction.Id;
    }

    private static string? NormalizeFileName(string? fileName)
    {
        var name = Path.GetFileName(fileName?.Trim());

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return name.Length > MaxFileNameLength ? name[..MaxFileNameLength] : name;
    }
}
