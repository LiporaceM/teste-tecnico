using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos;
using TaskManager.Api.Exceptions;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public class TaskService(
    AppDbContext db,
    IValidator<TaskRequest> taskValidator,
    IValidator<UpdateStatusRequest> statusValidator) : ITaskService
{
    public async Task<PagedResult<TaskResponse>> ListAsync(string userId, bool isAdmin, TaskFilter filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = Scoped(userId, isAdmin);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term)
                || (t.Description != null && t.Description.ToLower().Contains(term)));
        }
        if (filter.Status is not null) query = query.Where(t => t.Status == filter.Status);
        if (filter.Priority is not null) query = query.Where(t => t.Priority == filter.Priority);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskResponse(
                t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.CreatedAt, t.UpdatedAt,
                t.Owner!.FullName))
            .ToListAsync();

        return new PagedResult<TaskResponse>(items, page, pageSize, total);
    }

    public async Task<TaskSummary> SummaryAsync(string userId, bool isAdmin)
    {
        var counts = await Scoped(userId, isAdmin)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int Get(TaskItemStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;
        return new TaskSummary(counts.Sum(c => c.Count), Get(TaskItemStatus.Pending), Get(TaskItemStatus.InProgress), Get(TaskItemStatus.Completed));
    }

    public async Task<TaskResponse> GetAsync(Guid id, string userId, bool isAdmin)
    {
        var task = await FindAsync(id, userId, isAdmin);
        return ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(TaskRequest request, string userId)
    {
        await taskValidator.ValidateAndThrowAsync(request);

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status,
            Priority = request.Priority,
            DueDate = ToUtc(request.DueDate),
            UserId = userId
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return ToResponse(await FindAsync(task.Id, userId, isAdmin: false));
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, TaskRequest request, string userId, bool isAdmin)
    {
        await taskValidator.ValidateAndThrowAsync(request);
        var task = await FindAsync(id, userId, isAdmin, track: true);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = ToUtc(request.DueDate);
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateStatusAsync(Guid id, UpdateStatusRequest request, string userId, bool isAdmin)
    {
        await statusValidator.ValidateAndThrowAsync(request);
        var task = await FindAsync(id, userId, isAdmin, track: true);

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ToResponse(task);
    }

    public async Task DeleteAsync(Guid id, string userId, bool isAdmin)
    {
        var task = await FindAsync(id, userId, isAdmin, track: true);
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }

    // Usuário comum enxerga só as próprias tarefas; Admin enxerga todas.
    private IQueryable<TaskItem> Scoped(string userId, bool isAdmin) =>
        isAdmin ? db.Tasks.AsNoTracking() : db.Tasks.AsNoTracking().Where(t => t.UserId == userId);

    // Tarefa de outro usuário responde 404 (e não 403) para não revelar que ela existe.
    private async Task<TaskItem> FindAsync(Guid id, string userId, bool isAdmin, bool track = false)
    {
        var query = db.Tasks.Include(t => t.Owner).Where(t => t.Id == id);
        if (!isAdmin) query = query.Where(t => t.UserId == userId);
        if (!track) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync() ?? throw new NotFoundException("Tarefa não encontrada.");
    }

    private static TaskResponse ToResponse(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.CreatedAt, t.UpdatedAt,
        t.Owner != null ? t.Owner.FullName : string.Empty);

    private static DateTime? ToUtc(DateTime? date)
    {
        if (date is null) return null;
        var d = date.Value;
        return d.Kind switch
        {
            DateTimeKind.Utc => d,
            DateTimeKind.Local => d.ToUniversalTime(),
            _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
        };
    }
}
