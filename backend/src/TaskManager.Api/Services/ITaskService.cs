using TaskManager.Api.Dtos;

namespace TaskManager.Api.Services;

public interface ITaskService
{
    Task<PagedResult<TaskResponse>> ListAsync(string userId, bool isAdmin, TaskFilter filter);
    Task<TaskSummary> SummaryAsync(string userId, bool isAdmin);
    Task<TaskResponse> GetAsync(Guid id, string userId, bool isAdmin);
    Task<TaskResponse> CreateAsync(TaskRequest request, string userId);
    Task<TaskResponse> UpdateAsync(Guid id, TaskRequest request, string userId, bool isAdmin);
    Task<TaskResponse> UpdateStatusAsync(Guid id, UpdateStatusRequest request, string userId, bool isAdmin);
    Task DeleteAsync(Guid id, string userId, bool isAdmin);
}
