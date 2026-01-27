using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Settings.Responses;

public class UpdateEmailCommandResponse : ResponseWithError
{
    public string? Email { get; set; }
}
