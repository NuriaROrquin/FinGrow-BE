namespace FinGrow.Application.Interfaces;

public interface ITwilioMediaClient
{
    Task<TwilioMedia> DownloadAsync(Uri url, CancellationToken cancellationToken = default);
}

public sealed record TwilioMedia(ReadOnlyMemory<byte> Content, string ContentType);
