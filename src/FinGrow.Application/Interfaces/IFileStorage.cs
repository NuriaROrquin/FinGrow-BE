namespace FinGrow.Application.Interfaces;

public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<StoredFile?> OpenAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public sealed record StoredFile(Stream Content, string ContentType, long Length);

public sealed class FileStorageUnavailableException : Exception
{
    public FileStorageUnavailableException()
    {
    }

    public FileStorageUnavailableException(string message) : base(message)
    {
    }

    public FileStorageUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
