namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

/// <summary>
/// Un archivo adjunto a un correo encolado (HU-82: el PDF del reporte mensual). Se guarda en base64
/// porque así lo pide Resend y porque así lo puede cifrar el mismo conversor que el cuerpo.
/// </summary>
public sealed class EmailAttachment : ValueObject
{
    public const int MaxFileNameLength = 200;

    /// <summary>Resend acepta hasta 40 MB por correo; un reporte pesa cientos de KB.</summary>
    public const int MaxContentBytes = 10 * 1024 * 1024;

    private EmailAttachment(string fileName, string base64Content)
    {
        FileName = fileName;
        Base64Content = base64Content;
    }

    public string FileName { get; }

    public string Base64Content { get; }

    public static EmailAttachment From(string fileName, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length is 0 or > MaxContentBytes)
        {
            throw new DomainException(
                $"Un adjunto tiene que tener contenido y no puede superar los {MaxContentBytes / 1024 / 1024} MB.");
        }

        return new EmailAttachment(EnsureFileName(fileName), Convert.ToBase64String(content));
    }

    public static EmailAttachment FromStored(string fileName, string base64Content) =>
        string.IsNullOrWhiteSpace(base64Content)
            ? throw new DomainException("Un adjunto guardado tiene que tener contenido.")
            : new EmailAttachment(EnsureFileName(fileName), base64Content);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return Base64Content;
    }

    private static string EnsureFileName(string fileName)
    {
        var trimmed = (fileName ?? string.Empty).Trim();

        return trimmed.Length is 0 or > MaxFileNameLength
            ? throw new DomainException(
                $"El nombre del adjunto es obligatorio y no puede superar los {MaxFileNameLength} caracteres.")
            : trimmed;
    }
}
