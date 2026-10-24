using FluentValidation;
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

        tasks.MapPost("/", CreateTask)
            .WithName("PostTasks")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        tasks.MapPut("/{id:int}", UpdateTask)
            .WithName("PutTaskUpdate")
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

    private static async Task<Results<Created<TaskItem>, ValidationProblem>> CreateTask(ITaskRepository repository, PostTaskRequest title, IValidator<PostTaskRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(title);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var createdTask = await repository.CreateAsync(title);
        return TypedResults.Created($"/tasks/{createdTask.Id}", createdTask);
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult, ValidationProblem>> UpdateTask(ITaskRepository repository, int id, PutTaskRequest request, IValidator<PutTaskRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var task = await repository.UpdateAsync(id, request);
        return task is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.")
            : TypedResults.Ok(task);
    }

    private static async Task<Results<Ok<TaskItem>, ProblemHttpResult, ValidationProblem>> PatchTask(ITaskRepository repository, int id, PatchTaskRequest data, IValidator<PatchTaskRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var task = await repository.PatchAsync(id, data);

        return task is null
           ? TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.")
           : TypedResults.Ok(task);
    }
}