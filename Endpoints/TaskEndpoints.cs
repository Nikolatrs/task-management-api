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
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        tasks.MapPut("/{id:int}", PutTaskUpdate)
            .WithName("PutTaskUpdate")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        tasks.MapPatch("/{id:int}/Rename", PatchTaskRename)
            .WithName("PatchTaskRename")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        tasks.MapPatch("/{id:int}/Completion", PatchTaskCompletion)
            .WithName("PatchTaskCompletion")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<Ok<List<TaskItem>>> GetAllTask(NpgsqlDataSource dataSource)
    {
        List<TaskItem> tasks = new();
        await using var command = dataSource.CreateCommand(
            """
        SELECT id, title, is_completed 
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
            SELECT id, title, is_completed 
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

    private static async Task<Results<Created<TaskItem>, ProblemHttpResult>> PostTask(NpgsqlDataSource dataSource, CreateTaskRequest title)
    {

        if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > _stringLength)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

        await using var command = dataSource.CreateCommand(
           """
            INSERT into public.tasks (title) 
            VALUES (@title) 
            RETURNING id, title, is_completed
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

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> PutTaskUpdate(NpgsqlDataSource dataSource, int id, ChangeTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > _stringLength)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

        await using var command = dataSource.CreateCommand(
           """
            UPDATE public.tasks 
            SET title = @title, is_completed = @isCompleted
            WHERE id = @id 
            RETURNING id, title, is_completed 
            """);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("title", request.Title);
        command.Parameters.AddWithValue("isCompleted", request.IsCompleted);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var task = reader.ReadTaskItem();
            return TypedResults.Ok(task);
        }
        return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");
    }
    
    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> PatchTaskRename(NpgsqlDataSource dataSource, int id, RenameTaskRequest title)
    {
        if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > _stringLength)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

        await using var command = dataSource.CreateCommand(
           """
            UPDATE public.tasks 
            SET title = @title
            WHERE id = @id 
            RETURNING id, title, is_completed 
            """);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("title", title.Title);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var task = reader.ReadTaskItem();
            return TypedResults.Ok(task);
        }
        return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> PatchTaskCompletion(NpgsqlDataSource dataSource, int id, ChangeTaskCompletionRequest isCompleted)
    {
        if (isCompleted.IsCompleted is null)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, 
                title: "Bad Request", 
                detail: "Поле isCompleted обязательно для передачи");

        await using var command = dataSource.CreateCommand(
           """
            UPDATE public.tasks 
            SET is_completed = @is_completed
            WHERE id = @id 
            RETURNING id, title, is_completed 
            """);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("is_completed", isCompleted.IsCompleted.Value);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var task = reader.ReadTaskItem();
            return TypedResults.Ok(task);
        }
        return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");
    }
}