using Npgsql;
using TaskManagementApi.Extensions;
using TaskManagementApi.Models;

namespace TaskManagementApi.Repositories;

public class NpgsqlTaskRepository : ITaskRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlTaskRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        List<TaskItem> tasks = new();
        await using var command = _dataSource.CreateCommand(
            """
            SELECT id, title, is_completed, created_at, updated_at 
            FROM public.tasks
            ORDER BY id ASC
            """);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(reader.ReadTaskItem());
        }
        return tasks;
    }

    public async Task<TaskItem?> GetByIDAsync(int id)
    {
        await using var command = _dataSource.CreateCommand(
    """
            SELECT id, title, is_completed, created_at, updated_at 
            FROM public.tasks 
            WHERE id = @id
            """);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader.ReadTaskItem();
        }
        return null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var command = _dataSource.CreateCommand(
            """
            DELETE FROM public.tasks 
            WHERE id = @id;
            """);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }


    public async Task<TaskItem> CreateAsync(PostTaskRequest request)
    {
        await using var command = _dataSource.CreateCommand(
            """
            INSERT into public.tasks (title) 
            VALUES (@title) 
            RETURNING id, title, is_completed, created_at, updated_at
            """);
        command.Parameters.AddWithValue("title", request.Title);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.ReadTaskItem();
    }

    public async Task<TaskItem?> UpdateAsync(int id, PutTaskRequest request)
    {
        await using var command = _dataSource.CreateCommand(
          """
            UPDATE public.tasks 
            SET title = @title, is_completed = @isCompleted, updated_at = @updated_at
            WHERE id = @id 
            RETURNING id, title, is_completed, created_at, updated_at 
            """);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("title", request.Title);
        command.Parameters.AddWithValue("isCompleted", request.IsCompleted);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader.ReadTaskItem();
        }
        return null;
    }
    
    public async Task<TaskItem?> PatchAsync(int id, PatchTaskRequest request)
    {
        var sets = new List<string>();
        var command = _dataSource.CreateCommand();

        if (request.Title is not null)
        {
            sets.Add("title = @title");
            command.Parameters.AddWithValue("title", request.Title);
        }

        if (request.IsCompleted is not null)
        {
            sets.Add("is_completed = @is_completed");
            command.Parameters.AddWithValue("is_completed", request.IsCompleted);
        }

        if (sets.Count == 0) return null;

        command.CommandText = $"""
            UPDATE public.tasks 
            SET {string.Join(", ", sets)}, updated_at = @updated_at
            WHERE id = @id 
            RETURNING id, title, is_completed, created_at, updated_at 
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return reader.ReadTaskItem();
        }
        return null;

    }
}