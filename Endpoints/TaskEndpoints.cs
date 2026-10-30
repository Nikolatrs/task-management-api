using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using TaskManagementApi.Models;
using TaskManagementApi.Repositories;


namespace TaskManagementApi.Endpoints;
public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var tasks = app.MapGroup("/tasks").WithTags("Tasks");

        tasks.MapGet("/", GetAllTasks).WithName("GetAllTasks");
        tasks.MapGet("/{id:int}", GetTaskById).WithName("GetTaskById");
        tasks.MapDelete("/{id:int}", DeleteTaskID).WithName("DeleteTaskID");

        tasks.MapPost("/", CreateTask)
            .WithName("CreateTask")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        tasks.MapPut("/{id:int}", UpdateTask)
            .WithName("UpdateTask")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tasks.MapPatch("/{id:int}", PatchTask)
            .WithName("PatchTask")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<List<TaskItem>>> GetAllTasks(ITaskRepository repository)
    {
        var tasks = await repository.GetAllAsync();
        return TypedResults.Ok(tasks);
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult>> GetTaskById(ITaskRepository repository, int id)
    {
        var task = await repository.GetByIdAsync(id);
        return task is null ? TaskNotFound(id) : TypedResults.Ok(task);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteTaskID(ITaskRepository repository, int id)
    {
        var isDeleted = await repository.DeleteAsync(id);
        return isDeleted ? TypedResults.NoContent(): TaskNotFound(id);
    }

    private static async Task<Results<Created<TaskItem>, ValidationProblem>> CreateTask(ITaskRepository repository, PostTaskRequest request, IValidator<PostTaskRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var createdTask = await repository.CreateAsync(request);
        return TypedResults.Created($"/tasks/{createdTask.Id}", createdTask);
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult, ValidationProblem>> UpdateTask(ITaskRepository repository, int id, PutTaskRequest request, IValidator<PutTaskRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var task = await repository.UpdateAsync(id, request);
        return task is null
            ? TaskNotFound(id)
            : TypedResults.Ok(task);
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult, ValidationProblem>> PatchTask(ITaskRepository repository, int id, PatchTaskRequest data, IValidator<PatchTaskRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var task = await repository.PatchAsync(id, data);

        return task is null
           ? TaskNotFound(id)
           : TypedResults.Ok(task);
    }

    private static ProblemHttpResult TaskNotFound(int id) =>
    TypedResults.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Task not found",
        detail: $"Задача с id {id} не найдена.");
}