using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddHostedService<EventsListener>();

var app = builder.Build();

app.Run();


public sealed class EventsListener : BackgroundService
{
    private readonly ILogger<EventsListener> _log;
    private IChannel _channel = default!;
    private readonly string _queue;
    private readonly string _exchange;
    private readonly ConnectionFactory _factory;

    public EventsListener(IConfiguration cfg, ILogger<EventsListener> log)
    {
        _log = log;

        var rabbit = cfg.GetRequiredSection("RabbitMQ");

        var factory = new ConnectionFactory
        {
            HostName = rabbit["Host"] ?? Throw("Host"),
            Port = int.Parse(rabbit["Port"] ?? Throw("Port")),
            UserName = rabbit["User"] ?? Throw("User"),
            Password = rabbit["Pass"] ?? Throw("Pass")
        };
        _exchange = rabbit["Exchange"] ?? Throw("Exchange");

        _queue = rabbit["Queue"] ?? Throw("Queue");
        _factory = factory;

        static string Throw(string cfgPath)
        {
            throw new InvalidOperationException($"RabbitMQ configuration missing: {cfgPath}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (true)
        {
            try
            {
                if (_channel == null)
                {
                    var connection = await _factory.CreateConnectionAsync(ct);
                    _channel = await connection.CreateChannelAsync(null, ct);

                    await _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

                    await _channel.QueueDeclareAsync(_queue, durable: true,
                                                     exclusive: false,
                                                     autoDelete: false, cancellationToken: ct);
                    await _channel.QueueBindAsync(_queue, _exchange, routingKey: "task.*", cancellationToken: ct);
                }

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleMessageAsync;

                await _channel.BasicConsumeAsync(_queue, autoAck: false, consumer, ct);
                _log.LogInformation("Listening on queue '{Queue}' ...", _queue);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to connect to rabbit");
            }
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var ev = JsonSerializer.Deserialize<TaskEvent>(json)!;

            _log.LogInformation("TaskEvent: {Action}, Id: {Id}, Task Status: {Status}", ev.Action, ev.Id, ev.Status);

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Couldn't handle message");
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.CloseAsync();
        base.Dispose();
    }
}

public record TaskEvent
{
    public required Guid Id { get; init; }
    public required string Action { get; init; } // created|updated|deleted
    public required string Status { get; init; }
}

