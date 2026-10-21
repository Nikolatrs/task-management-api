using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using TaskManagementApi.Extensions;
using TaskManagementApi.Models;


namespace TaskManagementApi.Endpoints;
public static class TaskEndpoints
{
    private static int _stringLength = 200;

    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var tasks = app.MapGroup("/tasks").WithTags("Tasks");

        tasks.MapGet("/", GetAllTask).WithName("GetTasks");
        tasks.MapGet("/{id:int}", GetTaskID).WithName("GetTaskID");
        tasks.MapDelete("/{id:int}", DeleteTaskID).WithName("DeleteTask");

        tasks.MapPost("/", PostTask)
            .WithName("PostTasks")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        tasks.MapPut("/{id:int}", PutTaskUpdate)
            .WithName("PutTaskUpdate")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tasks.MapPatch("/{id:int}", PatchTask)
            .WithName("PatchTask")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<List<TaskItem>>> GetAllTask(NpgsqlDataSource dataSource)
    {
        List<TaskItem> tasks = new();
        await using var command = dataSource.CreateCommand(
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
        return TypedResults.Ok(tasks);
    }

    private static async Task<Results<Ok<TaskItem>, NotFound>> GetTaskID(NpgsqlDataSource dataSource, int id)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT id, title, is_completed, created_at, updated_at 
            FROM public.tasks 
            WHERE id = @id
            """);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return TypedResults.Ok(reader.ReadTaskItem());
        }
        return TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteTaskID(NpgsqlDataSource dataSource, int id)
    {
        await using var command = dataSource.CreateCommand(
            """
            DELETE FROM public.tasks 
            WHERE id = @id;
            """);
        command.Parameters.AddWithValue("id", id);
        if (await command.ExecuteNonQueryAsync() == 0)
        {
            return TypedResults.NotFound();
        }
        else
        {
            return TypedResults.NoContent();
        }
    }

    private static async Task<Results<Created<TaskItem>, ProblemHttpResult>> PostTask(NpgsqlDataSource dataSource, PostTaskRequest title)
    {

        if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > _stringLength)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

        await using var command = dataSource.CreateCommand(
           """
            INSERT into public.tasks (title) 
            VALUES (@title) 
            RETURNING id, title, is_completed, created_at, updated_at
            """);
        command.Parameters.AddWithValue("title", title.Title);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var task = reader.ReadTaskItem();
            return TypedResults.Created($"/tasks/{task.Id}", task);
        }
        return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError,
            title: "InternalServerError",
            detail: "Не удалось создать задачу.");
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> PutTaskUpdate(NpgsqlDataSource dataSource, int id, PutTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > _stringLength)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

        await using var command = dataSource.CreateCommand(
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
            var task = reader.ReadTaskItem();
            return TypedResults.Ok(task);
        }
        return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> PatchTask(NpgsqlDataSource dataSource, int id, PatchTaskRequest data)
    {
        if (data.Title is null && data.IsCompleted is null)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Не передано ни одного поля для обновления.");

        if (data.Title is not null && string.IsNullOrWhiteSpace(data.Title))
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Title не может быть пустым.");

        var sets = new List<string>();
        var command = dataSource.CreateCommand();

        if (data.Title is not null)
        {
            sets.Add("title = @title");
            command.Parameters.AddWithValue("title", data.Title);
        }

        if (data.IsCompleted is not null)
        {
            sets.Add("is_completed = @is_completed");
            command.Parameters.AddWithValue("is_completed", data.IsCompleted);
        }


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
            var task = reader.ReadTaskItem();
            return TypedResults.Ok(task);
        }
        return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");

    }

}