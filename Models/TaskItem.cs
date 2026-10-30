namespace TaskManagementApi.Models;

public record TaskItem(int Id, string Title, bool IsCompleted, DateTime CreatedAt, DateTime UpdatedAt);