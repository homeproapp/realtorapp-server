using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Settings.Responses;

public class ChangePasswordCommandResponse : ResponseWithError
{
    public bool Success { get; set; }
}
