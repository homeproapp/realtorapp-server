using File = RealtorApp.Domain.Models.File;

namespace RealtorApp.Domain.Extensions;

public static class FileExtensions
{
    public static string GetContentType(this File file)
    {
        if (string.IsNullOrEmpty(file.FileExtension))
        {
            return "application/octet-stream";
        }

        var extension = file.FileExtension.TrimStart('.').ToLowerInvariant();

        return extension switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
