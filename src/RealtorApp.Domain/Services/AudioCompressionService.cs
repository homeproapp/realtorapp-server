using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Domain.Services;

public class AudioCompressionService(ILogger<AudioCompressionService> logger) : IAudioCompressionService
{
    private readonly ILogger<AudioCompressionService> _logger = logger;

    public async Task<CompressionResult> CompressAudioAsync(
        Stream inputStream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ogg");

        try
        {
            await using (var inputFile = File.Create(inputPath))
            {
                await inputStream.CopyToAsync(inputFile, cancellationToken);
            }

            var inputSize = new FileInfo(inputPath).Length;

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{inputPath}\" -vn -map_metadata -1 -ac 1 -c:a libopus -b:a 16k -application voip \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFmpeg not found, returning original audio");
                return await CreateFallbackResultAsync(inputPath, originalFileName, cancellationToken);
            }

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg failed with exit code {ExitCode}: {Stderr}", process.ExitCode, stderr);
                return await CreateFallbackResultAsync(inputPath, originalFileName, cancellationToken);
            }

            var outputSize = new FileInfo(outputPath).Length;
            var compressionRatio = (1 - (double)outputSize / inputSize) * 100;
            _logger.LogInformation(
                "Audio compressed: {InputSize:N0} bytes -> {OutputSize:N0} bytes ({Ratio:F1}% reduction)",
                inputSize, outputSize, compressionRatio);

            var outputStream = new FileStream(
                outputPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.DeleteOnClose);

            var outputFileName = Path.ChangeExtension(originalFileName, ".ogg");

            return new CompressionResult
            {
                Stream = outputStream,
                FileName = outputFileName,
                ContentType = "audio/ogg"
            };
        }
        finally
        {
            if (File.Exists(inputPath))
            {
                try { File.Delete(inputPath); }
                catch { /* ignore cleanup errors */ }
            }
        }
    }

    private static async Task<CompressionResult> CreateFallbackResultAsync(
        string inputPath,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();
        await using (var inputFile = File.OpenRead(inputPath))
        {
            await inputFile.CopyToAsync(memoryStream, cancellationToken);
        }
        memoryStream.Position = 0;

        var contentType = Path.GetExtension(originalFileName).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".webm" => "audio/webm",
            _ => "audio/mpeg"
        };

        return new CompressionResult
        {
            Stream = memoryStream,
            FileName = originalFileName,
            ContentType = contentType
        };
    }
}
