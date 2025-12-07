namespace RealtorApp.Contracts;

public class UpdateTaskStatusCommand
{
    public Enums.TaskStatus NewStatus { get; set; }
}
