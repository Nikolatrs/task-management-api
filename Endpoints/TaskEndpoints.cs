using Microsoft.AspNetCore.Http.HttpResults;
using TaskManagementApi.Models;
using TaskManagementApi.Repositories;


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

        tasks.MapPost("/", Create)
            .WithName("PostTasks")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        tasks.MapPut("/{id:int}", Update)
            .WithName("PutTaskUpdate")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tasks.MapPatch("/{id:int}", PatchTask)
            .WithName("PatchTask")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<List<TaskItem>>> GetAllTask(ITaskRepository repository)
    {
        var tasks = await repository.GetAllAsync();
        return TypedResults.Ok(tasks);
    }

    private static async Task<Results<Ok<TaskItem>, NotFound>> GetTaskID(ITaskRepository repository, int id)
    {
        var task = await repository.GetByIDAsync(id);
        return task is null ? TypedResults.NotFound(): TypedResults.Ok(task);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteTaskID(ITaskRepository repository, int id)
    {
        var isDeleted = await repository.DeleteAsync(id);
        return isDeleted ? TypedResults.NoContent(): TypedResults.NotFound();
    }

    private static async Task<Results<Created<TaskItem>, ProblemHttpResult>> Create(ITaskRepository repository, PostTaskRequest title)
    {

        if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > _stringLength)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

        var createdTask = await repository.CreateAsync(title);
        return TypedResults.Created($"/tasks/{createdTask.Id}", createdTask);
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> Update(ITaskRepository repository, int id, PutTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > _stringLength)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Некорректная строка");

        var task = await repository.UpdateAsync(id, request);
        return task is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.")
            : TypedResults.Ok(task);
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> PatchTask(ITaskRepository repository, int id, PatchTaskRequest data)
    {
        if (data.Title is null && data.IsCompleted is null)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Не передано ни одного поля для обновления.");

        if (data.Title is not null && string.IsNullOrWhiteSpace(data.Title))
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Title не может быть пустым.");

        var task = await repository.PatchAsync(id, data);

        return task is null
           ? TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.")
           : TypedResults.Ok(task);
    }
}