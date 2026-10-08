using TaskManagementApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/tasks", () =>
{
    TaskItem[] taskItem = { new TaskItem(1, "Изучить ASP.NET Core", false), new TaskItem(1, "Изучить ASP.NET Core", false) };

    return taskItem;
})
.WithName("GetTasks")
.WithOpenApi();

app.Run();