using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Tasks.Responses;

public class AiTaskCreateCommandResponse : ResponseWithError
{
    public long[] TaskIds { get; set; } = [];
}
