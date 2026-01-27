namespace RealtorApp.Contracts.Commands.Settings.Requests;

public class UpdateEmailCommand
{
    public required string NewEmail { get; set; }
    public required string CurrentPassword { get; set; }
}
