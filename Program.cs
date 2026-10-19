using TaskManagementApi.Models;
using Npgsql;
using TaskManagementApi.Validators;

int stringLength = 200;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
app.MapGet("/tasks", async (NpgsqlDataSource dataSource) =>
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
    return tasks;
}).WithName("GetTasks").WithOpenApi();
app.MapGet("/tasks/{id:int}", async (NpgsqlDataSource dataSource, int id) =>
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
        return Results.Ok(new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2)));
    }
    return Results.NotFound();
}).WithName("GetTasksID").WithOpenApi();

app.MapDelete("/tasks/{id:int}", async (NpgsqlDataSource dataSource, int id) =>
{
    await using var command = dataSource.CreateCommand(
        """
        DELETE FROM public.tasks 
        WHERE id = @id;
        """);
    command.Parameters.AddWithValue("id", id);
    if (await command.ExecuteNonQueryAsync() == 0)
    {
        return Results.NotFound();
    }
    else
    {
        return Results.NoContent();
    }

}).WithName("DeleteTasks").WithOpenApi();

app.MapPost("/tasks", async (NpgsqlDataSource dataSource, CreateTaskRequest title) =>
{

    if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > stringLength) 
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

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

        return Results.Created($"/tasks/{task.Id}", task);
    }
    return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "InternalServerError", detail: "Не удалось создать задачу.");

}).WithName("PostTasks").WithOpenApi();

app.MapPut("/tasks/{id:int}", async (int id, NpgsqlDataSource dataSource, ChangeTaskRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > stringLength) 
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

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

        return Results.Ok(task);
    }
    return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");

}).WithName("PutTasksUpdate").WithOpenApi();

app.MapPatch("/tasks/{id:int}/rename", async (NpgsqlDataSource dataSource, int id, RenameTaskRequest title) =>
{

    if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > stringLength) 
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Некорректная строка");

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
        return Results.Ok(task);
    }
    return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");


}).WithName("PutTasksReneme").WithOpenApi();

app.MapPatch("/tasks/{id:int}/completion", async (NpgsqlDataSource dataSource, int id, ChangeTaskCompletionRequest isCompleted) =>
{
    if (isCompleted.IsCompleted is null)
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Поле isCompleted обязательно для передачи");


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
        return Results.Ok(task);
    }
    return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "NotFound", detail: "Не удалось найти задачу.");
}).WithName("ChangeTaskCompletion").WithOpenApi();

app.Run();
