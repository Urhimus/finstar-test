using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;

namespace TaskManagement.API.Controllers;

public record CreateTaskItemDTO(string Title, string Description);
public record GetTaskItemDTO(Guid Id, string Title, string Description, TaskItemStatus Status);

[ApiController]
[Route("api/[controller]")]
public class TasksController(
    ApplicationDbContext context,
    ILogger<TasksController> logger,
    DateTimeProvider dateTimeProvider,
    IMediator mediator) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<TasksController> _logger = logger;
    private readonly DateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IMediator _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<GetTaskItemDTO>> GetTask(Guid id)
    {
        _logger.LogDebug("Retrieving task with ID: {Id}", id);

        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
        {
            _logger.LogInformation("Task with ID: {Id} not found", id);
            return NotFound();
        }

        return new GetTaskItemDTO(task.Id, task.Title, task.Description, task.Status);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateTask([FromBody] CreateTaskItemDTO taskDto, CancellationToken ct)
    {
        _logger.LogDebug("Creating new task");

        var task = new TaskItem(
            taskDto.Title,
            taskDto.Description,
            createdAt: _dateTimeProvider.UtcNow());

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Task created with ID: {Id}", task.Id);

        await PublishEvents(task.Events, ct);

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTaskStatus(Guid id, [FromQuery] TaskItemStatus status, CancellationToken ct)
    {
        _logger.LogDebug("Updating task with ID: {Id}", id);

        var existingTask = await _context.Tasks.FindAsync([id], ct);
        if (existingTask == null)
        {
            _logger.LogDebug("Task with ID: {Id} not found", id);
            return NotFound();
        }

        existingTask.UpdateStatus(status, _dateTimeProvider.UtcNow());

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Task with ID: {Id} updated successfully", id);

        await PublishEvents(existingTask.Events, ct);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        _logger.LogDebug("Deleting task with ID: {Id}", id);

        var task = await _context.Tasks.FindAsync([id], ct);
        if (task == null)
        {
            _logger.LogDebug("Task with ID: {Id} not found", id);
            return NotFound();
        }

        task.Delete();
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Task with ID: {Id} deleted successfully", id);

        await PublishEvents(task.Events, ct);

        return NoContent();
    }

    private async Task PublishEvents(IEnumerable<INotification> events, CancellationToken ct)
    {

        foreach (var evt in events)
        {
            try
            {
                await _mediator.Publish(evt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while publishing event: {Event} {Error}", evt, ex);
            }
        }
    }
}
