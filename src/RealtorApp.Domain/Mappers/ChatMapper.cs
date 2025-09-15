using RealtorApp.Contracts.Commands.Chat;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Mappers;

public static class ChatMapper
{
    public static Message NewMessageToDbModel(SendMessageCommand command)
    {
        //TODO:
        return new();
    }

    public static SendMessageCommandResponse DbModelToMessageResponse(Message message)
    {
        //TODO:
        return new();
    }
}
