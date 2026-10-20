using TaskManager.Api.Models;

namespace TaskManager.Api.Dtos;

public record TaskRequest(string Title, string? Description, TaskItemStatus Status, TaskPriority Priority, DateTime? DueDate);

public record UpdateStatusRequest(TaskItemStatus Status);

public record TaskResponse(
    Guid Id, string Title, string? Description, TaskItemStatus Status, TaskPriority Priority,
    DateTime? DueDate, DateTime CreatedAt, DateTime UpdatedAt, string OwnerName);

public record TaskSummary(int Total, int Pending, int InProgress, int Completed);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public class TaskFilter
{
    public string? Search { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
