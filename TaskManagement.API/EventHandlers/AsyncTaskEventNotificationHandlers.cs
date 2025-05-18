using MediatR;
using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using System.Text.Json;

namespace TaskManagement.API.EventHandlers;

public record TaskEvent
{
    public required Guid Id { get; init; }
    public required string Action { get; init; } // created|updated|deleted
    public required string Status { get; init; }
}

public sealed class AsyncTaskEventsPublisher : IAsyncDisposable
{
    private readonly ILogger<AsyncTaskEventsPublisher> _logger;
    private IChannel _channel = default!;
    private readonly string _exchange;
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AsyncTaskEventsPublisher(IConfiguration cfg, ILogger<AsyncTaskEventsPublisher> logger)
    {
        var rabbit = cfg.GetRequiredSection("RabbitMQ");

        var factory = new ConnectionFactory
        {
            HostName = rabbit["Host"] ?? Throw("Host"),
            Port = int.Parse(rabbit["Port"] ?? Throw("Port")),
            UserName = rabbit["User"] ?? Throw("User"),
            Password = rabbit["Pass"] ?? Throw("Pass")
        };
        _exchange = rabbit["Exchange"] ?? Throw("Exchange");
        _factory = factory;

        static string Throw(string cfgPath)
        {
            throw new InvalidOperationException($"RabbitMQ configuration missing: {cfgPath}");
        }

        this._logger = logger;
    }

    public async Task Publish(TaskEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Publishing event {Evt}", evt);

        await _semaphore.WaitAsync();

        if (_channel == null)
        {
            var connection = await _factory.CreateConnectionAsync();
            _channel = await connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true);
        }

        _semaphore.Release();

        var body = JsonSerializer.SerializeToUtf8Bytes(evt);
        var props = new BasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = DeliveryModes.Persistent;

        var routingKey = $"task.{evt.Action}";
        await _channel.BasicPublishAsync(_exchange, routingKey, mandatory: true, props, body, ct);

        _logger.LogInformation("Published event {Evt}", evt);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
    }
}

public class TaskUpdatedAsyncEventHandler(AsyncTaskEventsPublisher rabbitPublisher) : INotificationHandler<TaskUpdatedEvent>
{
    public async Task Handle(TaskUpdatedEvent evt, CancellationToken ct)
    {
        await rabbitPublisher.Publish(new() { Id = evt.TaskId, Action = "Updated", Status = "Created" }, ct);
    }
}

public class TaskCreatedAsyncEventHandler(AsyncTaskEventsPublisher rabbitPublisher) : INotificationHandler<TaskCreatedEvent>
{
    public async Task Handle(TaskCreatedEvent evt, CancellationToken ct)
    {
        await rabbitPublisher.Publish(new() { Id = evt.Task.Id, Action = "Created", Status = evt.Task.Status.ToString() }, ct);
    }
}

public class TaskDeletedAsyncEventHandler(AsyncTaskEventsPublisher rabbitPublisher) : INotificationHandler<TaskDeletedEvent>
{
    public async Task Handle(TaskDeletedEvent evt, CancellationToken ct)
    {
        await rabbitPublisher.Publish(new() { Id = evt.Id, Action = "Deleted", Status = "Deleted" }, ct);
    }
}
