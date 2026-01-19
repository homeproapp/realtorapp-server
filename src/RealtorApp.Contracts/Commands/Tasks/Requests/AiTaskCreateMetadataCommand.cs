namespace RealtorApp.Contracts.Commands.Tasks.Requests;

public class AiTaskCreateMetadataCommand
{
    public string Timestamp { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
