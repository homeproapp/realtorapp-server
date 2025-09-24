namespace RealtorApp.Contracts.Common;

public abstract class ResponseWithError
{
    public string? ErrorMessage { get; set; }
}