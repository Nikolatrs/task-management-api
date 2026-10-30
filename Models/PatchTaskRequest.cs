namespace TaskManagementApi.Models;
public record PatchTaskRequest() 
{
    public string? Title { get; init; }
    public bool? IsCompleted { get; init; }
}