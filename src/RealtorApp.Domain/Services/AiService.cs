using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class AiService : IAiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AiService> _logger;
    private readonly IAudioCompressionService _audioCompression;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public AiService(
        IHttpClientFactory httpClientFactory,
        ILogger<AiService> logger,
        IAudioCompressionService audioCompression,
        AppSettings settings)
    {
        _logger = logger;
        _http = httpClientFactory.CreateClient("OpenAI");
        _audioCompression = audioCompression;
    }

    private async Task<string> TranscribeAudioAsync(FileUploadRequest audio)
    {
        using var compressed = await _audioCompression.CompressAudioAsync(audio.Content, audio.FileName);

        var audioBytes = await ReadStreamToBytes(compressed.Stream);

        using var multipart = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(audioBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue(compressed.ContentType);
        multipart.Add(byteContent, "file", compressed.FileName);

        multipart.Add(new StringContent("whisper-1"), "model");
        multipart.Add(new StringContent("verbose_json"), "response_format");
        multipart.Add(new StringContent("segment"), "timestamp_granularities[]");

        var response = await _http.PostAsync("audio/transcriptions", multipart);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Whisper transcription error: {Error}", error);
            return string.Empty;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        if (!doc.RootElement.TryGetProperty("segments", out var segments))
        {
            return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var segment in segments.EnumerateArray())
        {
            var start = segment.GetProperty("start").GetDouble();
            var text = segment.GetProperty("text").GetString()?.Trim() ?? string.Empty;
            var timestamp = TimeSpan.FromSeconds(start).ToString(@"mm\:ss");
            sb.AppendLine($"[{timestamp}] {text}");
        }

        return sb.ToString();
    }


    public async Task<AiCreatedTaskDto[]> ProcessSessionWithClient(
        FileUploadRequest audio,
        FileUploadRequest[] images,
        AiTaskCreateMetadataCommand[] metadata)
    {
        var transcript = await TranscribeAudioAsync(audio);

        if (string.IsNullOrEmpty(transcript))
        {
            _logger.LogWarning("Transcription returned empty, cannot process tasks");
            return [];
        }

        var fileNameToMetadata = metadata.ToDictionary(m => m.FileName, m => m);

        var imageList = new List<string>();
        var imageParts = new List<object>();

        foreach (var image in images)
        {
            if (!fileNameToMetadata.TryGetValue(image.FileName, out var imageMetadata))
            {
                _logger.LogWarning(
                    "No metadata found for image {FileName}, skipping",
                    image.FileName);
                continue;
            }

            imageList.Add($"- Filename: \"{image.FileName}\" | Timestamp: {imageMetadata.Timestamp}");

            var imageBytes = await ReadStreamToBytes(image.Content);
            var imageBase64 = Convert.ToBase64String(imageBytes);
            var dataUrl = $"data:{image.ContentType};base64,{imageBase64}";

            imageParts.Add(new
            {
                type = "input_image",
                image_url = dataUrl
            });

            imageParts.Add(new
            {
                type = "input_text",
                text = $"[Image: {image.FileName} at {imageMetadata.Timestamp}]"
            });
        }

        var imageListText = imageList.Count > 0
            ? $"## Available Images (use these EXACT filenames)\n\n{string.Join("\n", imageList)}"
            : "## No images provided";

        var contentParts = new List<object>
        {
            new
            {
                type = "input_text",
                text = $"## Walkthrough Transcript\n\n{transcript}\n\n{imageListText}"
            }
        };

        contentParts.AddRange(imageParts);

        var requestBody = new
        {
            model = "gpt-4o-mini",
            instructions = AiConstants.TaskExtractionSystemPrompt,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = contentParts
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "ai_created_tasks",
                    schema = JsonDocument.Parse(AiConstants.TasksOutputSchema).RootElement.GetProperty("schema"),
                    strict = true
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("responses", httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI error: {Error}", error);
            return [];
        }

        var responseJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseJson);

        var outputText =
            doc.RootElement
               .GetProperty("output")[0]
               .GetProperty("content")[0]
               .GetProperty("text")
               .GetString();

        if (string.IsNullOrEmpty(outputText))
        {
            _logger.LogWarning("OpenAI response contained empty text");
            return [];
        }

        AiTasksResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<AiTasksResponse>(outputText, _jsonOptions);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse response");
            result = new();
        }


        return result?.Tasks ?? [];
    }

    private static async Task<byte[]> ReadStreamToBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private sealed class AiTasksResponse
    {
        public AiCreatedTaskDto[] Tasks { get; set; } = [];
    }
}
