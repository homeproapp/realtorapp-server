namespace RealtorApp.Contracts.Commands.Settings.Requests;

public class ChangePasswordCommand
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}
