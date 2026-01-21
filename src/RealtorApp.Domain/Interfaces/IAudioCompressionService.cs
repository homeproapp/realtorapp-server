namespace RealtorApp.Domain.Interfaces;

public interface IAudioCompressionService
{
    Task<CompressionResult> CompressAudioAsync(
        Stream inputStream,
        string originalFileName,
        CancellationToken cancellationToken = default);
}

public sealed class CompressionResult : IDisposable
{
    public required Stream Stream { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }

    public void Dispose() => Stream.Dispose();
}
