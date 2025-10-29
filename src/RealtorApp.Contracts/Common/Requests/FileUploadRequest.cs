namespace RealtorApp.Contracts.Common.Requests;

public class FileUploadRequest : IDisposable
{
    public required Stream Content { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string FileExtension { get; set; }
    public long ContentLength { get; set; }

    public void Dispose()
    {
        Content?.Dispose();
    }
}
