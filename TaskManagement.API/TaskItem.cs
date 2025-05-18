using MediatR;

namespace TaskManagement.API;

public record TaskCreatedEvent(TaskItem Task) : INotification;
public record TaskUpdatedEvent(Guid TaskId, TaskItemStatus NewStatus) : INotification;
public record TaskDeletedEvent(Guid Id) : INotification;

public class TaskItem
{
    private List<INotification> _events = [];

    public IReadOnlyList<INotification> Events => _events;
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TaskItemStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; set; }

    public void UpdateStatus(TaskItemStatus status, DateTime date)
    {
        Status = status;
        UpdatedAt = date;
        _events.Add(new TaskUpdatedEvent(Id, Status));
    }

    public void Delete()
    {
        _events.Add(new TaskDeletedEvent(Id));
    }

    public TaskItem(string title, string description, DateTime createdAt)
    {
        Title = !string.IsNullOrEmpty(title) ? title : throw new ArgumentException("Title can't be empty", nameof(title));
        Description = description ?? string.Empty;
        Status = TaskItemStatus.New;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        _events.Add(new TaskCreatedEvent(this));
    }

    private TaskItem() { } // For EF
}

public enum TaskItemStatus
{
    New,
    InProgress,
    Completed
}
