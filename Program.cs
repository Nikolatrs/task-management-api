using TaskManagementApi.Models;
using Npgsql;
using TaskManagementApi.Validators;
using Microsoft.AspNetCore.Http.HttpResults;
using TaskManagementApi.Extensions;
using TaskManagementApi.Endpoints;
using TaskManagementApi.Repositories;

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
builder.Services.AddScoped<ITaskRepository, NpgsqlTaskRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapGet("/", () => "Hy, it's task management!!!");
app.MapTaskEndpoints();

app.Run();
