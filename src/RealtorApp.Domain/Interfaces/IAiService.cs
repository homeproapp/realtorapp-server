using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IAiService
{
    Task<AiCreatedTaskDto[]> ProcessSessionWithClient(FileUploadRequest audio, FileUploadRequest[] images, AiTaskCreateMetadataCommand[] metadata);
}
