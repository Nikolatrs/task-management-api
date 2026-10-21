namespace TaskManagementApi.Models;

public record TaskItem(int Id, string Title, bool IsCompleted, DateTime CreatedAT, DateTime UpdateAT);