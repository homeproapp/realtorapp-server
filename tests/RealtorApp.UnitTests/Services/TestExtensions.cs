using RealtorApp.Contracts.Commands.Invitations.Responses;

namespace RealtorApp.UnitTests.Services;

public static class TestExtensions
{
    public static bool IsSuccess(this SendInvitationCommandResponse response)
    {
        return response.Errors.Count == 0;
    }

    public static bool IsSuccess(this AcceptInvitationCommandResponse response)
    {
        return string.IsNullOrEmpty(response.ErrorMessage);
    }
}
