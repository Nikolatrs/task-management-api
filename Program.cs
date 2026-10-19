using TaskManagementApi.Models;
using Npgsql;
using TaskManagementApi.Validators;
using Microsoft.AspNetCore.Http.HttpResults;

int stringLength = 200;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is empty or missing.");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapGet("/", () => "Hy, it's task management!!!");
app.MapGet("/tasks", async Task<Ok<List<TaskItem>>> (NpgsqlDataSource dataSource) =>
{
    List<TaskItem> tasks = new List<TaskItem>();
    await using var command = dataSource.CreateCommand(
        """
        SELECT id, title, is_completed 
        FROM public.tasks
        ORDER BY id ASC
        """);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        tasks.Add(new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2)));
    }
    return TypedResults.Ok(tasks);

}).WithName("GetTasks");

app.MapGet("/tasks/{id:int}", async Task<Results<Ok<TaskItem>, NotFound>> (NpgsqlDataSource dataSource, int id) =>
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
        return TypedResults.Ok(new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2)));
    }
    return TypedResults.NotFound();
}).WithName("GetTasksID");

app.MapDelete("/tasks/{id:int}", async Task<Results<NoContent, NotFound>> (NpgsqlDataSource dataSource, int id) =>
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

}).WithName("DeleteTasks");

app.MapPost("/tasks", async Task<Results<Created<TaskItem>, ProblemHttpResult>> (NpgsqlDataSource dataSource, CreateTaskRequest title) =>
{

    if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > stringLength)
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
        var task = new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2));

        return TypedResults.Created($"/tasks/{task.Id}", task);
    }
    return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "InternalServerError", detail: "Не удалось создать задачу.");

}).WithName("PostTasks")
.ProducesProblem(StatusCodes.Status500InternalServerError)
.ProducesProblem(StatusCodes.Status400BadRequest);

app.MapPut("/tasks/{id:int}", async Task<Results<Ok<TaskItem>, ProblemHttpResult>> (int id, NpgsqlDataSource dataSource, ChangeTaskRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > stringLength)
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
        var task = new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2));

        return TypedResults.Ok(task);
    }
    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");

}).WithName("PutTasksUpdate").ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status400BadRequest);

app.MapPatch("/tasks/{id:int}/rename", async Task<Results<Ok<TaskItem>, ProblemHttpResult>> (NpgsqlDataSource dataSource, int id, RenameTaskRequest title) =>
{

    if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > stringLength)
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
        var task = new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2));
        return TypedResults.Ok(task);
    }
    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");


}).WithName("PutTasksReneme").ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status400BadRequest);

app.MapPatch("/tasks/{id:int}/completion", async Task<Results<Ok<TaskItem>, ProblemHttpResult>> (NpgsqlDataSource dataSource, int id, ChangeTaskCompletionRequest isCompleted) =>
{
    if (isCompleted.IsCompleted is null)
        return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Поле isCompleted обязательно для передачи");


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
        var task = new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2));
        return TypedResults.Ok(task);
    }
    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");
}).WithName("ChangeTaskCompletion").ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status400BadRequest);

app.Run();
