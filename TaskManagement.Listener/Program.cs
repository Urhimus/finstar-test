using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Events");

app.MapPost("/taskCreated", (TaskEventDto taskEvent) =>
{
    logger.LogInformation("Task Created Event: {Evt}", JsonSerializer.Serialize(taskEvent));
    return Results.Ok();
});

app.MapPost("/taskStatusUpdated", (TaskUpdatedEventDto taskEvent) =>
{
    logger.LogInformation("Task Updated Event: TaskId: {TaskId}, Status: {Status}",
        taskEvent.Id, taskEvent.NewStatus);
    return Results.Ok();
});

app.MapPost("/taskDeleted", ([FromBody] Guid taskId) =>
{
    logger.LogInformation("Task Deleted Event: TaskId: {TaskId}", taskId);
    return Results.Ok();
});

app.Run();

public class TaskEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TaskUpdatedEventDto
{
    public Guid Id { get; set; }
    public string NewStatus { get; set; } = default!;
}