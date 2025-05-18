using MediatR;
using System.Net.Http;
using System.Threading.Tasks;
using static TaskManagement.API.EventHandlers.TaskEventNotificationHttpClientExstension;

namespace TaskManagement.API.EventHandlers;

public static class TaskEventNotificationHttpClientExstension
{
    public const string ClientName = "TaskEventNotificationClient";

    public static IServiceCollection AddTaskEventNotificationHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        string configUriName = "SYNC_LISTENER_HOST";
        string? listenerBaseUrl = configuration[configUriName];
        if (string.IsNullOrEmpty(listenerBaseUrl))
        {
            string errorMessage = $"{configUriName} is not present in configuration";
            throw new InvalidOperationException(errorMessage);
        }

        services.AddHttpClient(ClientName, (client) =>
        {
            client.BaseAddress = new Uri(listenerBaseUrl);
        });

        return services;
    }
}

public class TaskUpdatedEventHandler(
    ILogger<TaskUpdatedEventHandler> logger,
    IHttpClientFactory httpClientFactory
    ) : INotificationHandler<TaskUpdatedEvent>
{
    public async Task Handle(TaskUpdatedEvent evt, CancellationToken ct)
    {
        try
        {
            var dto = new TaskUpdatedEventDto()
            {
                Id = evt.TaskId,
                NewStatus = evt.NewStatus.ToString()
            };

            var client = httpClientFactory.CreateClient(ClientName);
            var response = await client.PostAsJsonAsync("taskStatusUpdated", dto, ct);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Task updated event notification sent successfully for TaskId: {TaskId}", evt.TaskId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send task updated event notification for TaskId: {TaskId}", evt.TaskId);
        }
    }

    public class TaskUpdatedEventDto
    {
        public Guid Id { get; set; }
        public string NewStatus { get; set; } = default!;
    }
}

public class TaskCreatedEventHandler(
    ILogger<TaskCreatedEventHandler> logger,
    IHttpClientFactory httpClientFactory
    ) : INotificationHandler<TaskCreatedEvent>
{
    public async Task Handle(TaskCreatedEvent evt, CancellationToken ct)
    {
        try
        {
            var dto = new TaskCreatedEventDto
            {
                Id = evt.Task.Id,
                Title = evt.Task.Title,
                Description = evt.Task.Description,
                Status = evt.Task.Status.ToString(),
                CreatedAt = evt.Task.CreatedAt,
                UpdatedAt = evt.Task.UpdatedAt
            };

            var client = httpClientFactory.CreateClient(ClientName);
            var response = await client.PostAsJsonAsync("taskCreated", dto, ct);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Task created event notification sent successfully for TaskId: {TaskId}", evt.Task.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send task created event notification for TaskId: {TaskId}", evt.Task.Id);
        }
    }

    public class TaskCreatedEventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

public class TaskDeletedEventHandler(
    ILogger<TaskDeletedEventHandler> logger,
    IHttpClientFactory httpClientFactory
    ) : INotificationHandler<TaskDeletedEvent>
{
    public async Task Handle(TaskDeletedEvent evt, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            var response = await client.PostAsJsonAsync("taskDeleted", evt.Id, ct);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Task deleted event notification sent successfully for TaskId: {TaskId}", evt.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send task deleted event notification for TaskId: {TaskId}", evt.Id);
        }
    }
}