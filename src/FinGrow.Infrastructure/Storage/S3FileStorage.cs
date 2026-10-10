namespace FinGrow.Infrastructure.Storage;

using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Options;

internal sealed class S3FileStorage(IAmazonS3 s3, IOptions<S3StorageOptions> options) : IFileStorage
{
    private readonly string _bucket = options.Value.BucketName;

    public Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        TranslateFailuresAsync(() => s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
                // R2 rechaza el envío por partes que el SDK de AWS usa por defecto.
                UseChunkEncoding = false,
            },
            cancellationToken));

    public async Task<StoredFile?> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await TranslateFailuresAsync(() => s3.GetObjectAsync(_bucket, key, cancellationToken));

            return new StoredFile(response.ResponseStream, response.Headers.ContentType, response.ContentLength);
        }
        catch (FileStorageUnavailableException exception)
            when (exception.InnerException is AmazonS3Exception { StatusCode: HttpStatusCode.NotFound })
        {
            return null;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        TranslateFailuresAsync(() => s3.DeleteObjectAsync(_bucket, key, cancellationToken));

    private static async Task<T> TranslateFailuresAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (AmazonServiceException exception)
        {
            throw new FileStorageUnavailableException(
                $"El almacenamiento respondio {(int)exception.StatusCode} ({exception.ErrorCode}).", exception);
        }
        catch (AmazonClientException exception)
        {
            throw new FileStorageUnavailableException("No se pudo hablar con el almacenamiento.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new FileStorageUnavailableException("No se pudo hablar con el almacenamiento.", exception);
        }
    }
}
