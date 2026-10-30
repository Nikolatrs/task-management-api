using TaskManagementApi.Models;

namespace TaskManagementApi.Repositories;
public interface ITaskRepository
{
    Task<List<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<TaskItem> CreateAsync(PostTaskRequest request);
    Task<TaskItem?> UpdateAsync(int id, PutTaskRequest request);
    Task<TaskItem?> PatchAsync(int id, PatchTaskRequest request);
}

