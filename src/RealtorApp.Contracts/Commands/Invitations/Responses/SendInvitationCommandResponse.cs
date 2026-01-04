namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class SendInvitationCommandResponse
{
    public int InvitationsSent { get; set; }
    public List<string> Errors { get; set; } = [];
}
