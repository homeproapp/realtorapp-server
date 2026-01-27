using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Settings.Responses;

public class UploadAvatarCommandResponse : ResponseWithError
{
    public long? ProfileImageId { get; set; }
}
