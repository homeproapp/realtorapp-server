using Microsoft.Extensions.Logging;
using Moq;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Services;

namespace RealtorApp.UnitTests.Services;

public class AudioCompressionServiceTests : IDisposable
{
    private readonly Mock<ILogger<AudioCompressionService>> _mockLogger;
    private readonly AudioCompressionService _service;
    private readonly List<string> _tempFiles = [];

    public AudioCompressionServiceTests()
    {
        _mockLogger = new Mock<ILogger<AudioCompressionService>>();
        _service = new AudioCompressionService(_mockLogger.Object);
    }

    [Fact]
    public async Task CompressAudioAsync_WithValidAudio_ReturnsCompressedOggFile()
    {
        var testAudioPath = await CreateTestWavFileAsync();
        await using var inputStream = File.OpenRead(testAudioPath);

        using var result = await _service.CompressAudioAsync(inputStream, "test.wav");

        Assert.NotNull(result);
        Assert.Equal("test.ogg", result.FileName);
        Assert.Equal("audio/ogg", result.ContentType);
        Assert.True(result.Stream.Length > 0);
    }

    [Fact]
    public async Task CompressAudioAsync_WithValidAudio_ProducesValidOggStream()
    {
        var testAudioPath = await CreateTestWavFileAsync();
        await using var inputStream = File.OpenRead(testAudioPath);

        using var result = await _service.CompressAudioAsync(inputStream, "recording.mp3");

        var buffer = new byte[4];
        var bytesRead = await result.Stream.ReadAsync(buffer);

        Assert.Equal(4, bytesRead);
        Assert.Equal((byte)'O', buffer[0]);
        Assert.Equal((byte)'g', buffer[1]);
        Assert.Equal((byte)'g', buffer[2]);
        Assert.Equal((byte)'S', buffer[3]);
    }

    [Fact]
    public async Task CompressAudioAsync_WithMp3Extension_ChangesExtensionToOgg()
    {
        var testAudioPath = await CreateTestWavFileAsync();
        await using var inputStream = File.OpenRead(testAudioPath);

        using var result = await _service.CompressAudioAsync(inputStream, "my-recording.mp3");

        Assert.Equal("my-recording.ogg", result.FileName);
    }

    [Fact]
    public async Task CompressAudioAsync_WithInvalidAudioData_ReturnsFallbackWithOriginalData()
    {
        var invalidData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var inputStream = new MemoryStream(invalidData);

        using var result = await _service.CompressAudioAsync(inputStream, "invalid.mp3");

        Assert.Equal("invalid.mp3", result.FileName);
        Assert.Equal("audio/mpeg", result.ContentType);

        using var ms = new MemoryStream();
        await result.Stream.CopyToAsync(ms);
        Assert.Equal(invalidData, ms.ToArray());
    }

    [Fact]
    public async Task CompressAudioAsync_WithWavExtension_ReturnsFallbackWithCorrectContentType()
    {
        var invalidData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var inputStream = new MemoryStream(invalidData);

        using var result = await _service.CompressAudioAsync(inputStream, "test.wav");

        Assert.Equal("audio/wav", result.ContentType);
    }

    [Fact]
    public async Task CompressAudioAsync_WithM4aExtension_ReturnsFallbackWithCorrectContentType()
    {
        var invalidData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var inputStream = new MemoryStream(invalidData);

        using var result = await _service.CompressAudioAsync(inputStream, "test.m4a");

        Assert.Equal("audio/mp4", result.ContentType);
    }

    [Fact]
    public async Task CompressAudioAsync_DisposingResult_CleansUpResources()
    {
        var testAudioPath = await CreateTestWavFileAsync();
        await using var inputStream = File.OpenRead(testAudioPath);

        var result = await _service.CompressAudioAsync(inputStream, "test.wav");
        var stream = result.Stream;

        result.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Fact]
    public async Task CompressAudioAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var testAudioPath = await CreateTestWavFileAsync();
        await using var inputStream = File.OpenRead(testAudioPath);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.CompressAudioAsync(inputStream, "test.wav", cts.Token));
    }

    private async Task<string> CreateTestWavFileAsync()
    {
        var tempPath = Path.GetTempFileName();
        _tempFiles.Add(tempPath);

        var sampleRate = 8000;
        var durationSeconds = 1;
        var numSamples = sampleRate * durationSeconds;
        var byteRate = sampleRate * 2;
        var dataSize = numSamples * 2;

        await using var fs = File.Create(tempPath);
        await using var bw = new BinaryWriter(fs);

        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);

        bw.Write("fmt "u8);
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)2);
        bw.Write((short)16);

        bw.Write("data"u8);
        bw.Write(dataSize);

        for (var i = 0; i < numSamples; i++)
        {
            var sample = (short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 10000);
            bw.Write(sample);
        }

        return tempPath;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); }
            catch { /* ignore */ }
        }
        GC.SuppressFinalize(this);
    }
}
