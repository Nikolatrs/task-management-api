namespace TaskManagementApi.Models;

public record ChangeTaskCompletionRequest()
{
    public bool? IsCompleted { get; init; }
}