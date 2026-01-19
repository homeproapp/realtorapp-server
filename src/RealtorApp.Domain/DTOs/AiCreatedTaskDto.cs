using RealtorApp.Contracts.Enums;

namespace RealtorApp.Domain.DTOs;

public class AiCreatedTaskDto
{
    public required string Title { get; set; }
    public required string Room { get; set; }
    public string? Description { get; set; }
    public required TaskPriority Priority { get; set; }
    public string[] AssociatedImagesFileNames { get; set; } = [];

}
