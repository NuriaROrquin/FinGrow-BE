namespace FinGrow.Infrastructure.Storage;

using System.ComponentModel.DataAnnotations;

public sealed class S3StorageOptions
{
    public const string SectionName = "Storage";

    [Required(ErrorMessage = "Falta configurar la URL del almacenamiento de archivos (Storage:ServiceUrl).")]
    [Url]
    public string ServiceUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar el bucket del almacenamiento de archivos (Storage:BucketName).")]
    public string BucketName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar la access key del almacenamiento de archivos (Storage:AccessKey).")]
    public string AccessKey { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar la secret key del almacenamiento de archivos (Storage:SecretKey).")]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string Region { get; init; } = "auto";

    public bool ForcePathStyle { get; init; } = true;

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}
