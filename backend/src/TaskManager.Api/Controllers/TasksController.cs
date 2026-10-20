using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Dtos;
using TaskManager.Api.Extensions;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class TasksController(ITaskService service) : ControllerBase
{
    private string UserId => User.GetUserId();
    private bool IsAdmin => User.IsAdmin();

    /// <summary>Lista tarefas com busca, filtros e paginação.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskResponse>>> List([FromQuery] TaskFilter filter) =>
        Ok(await service.ListAsync(UserId, IsAdmin, filter));

    /// <summary>Totais de tarefas por status.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<TaskSummary>> Summary() =>
        Ok(await service.SummaryAsync(UserId, IsAdmin));

    /// <summary>Busca uma tarefa pelo id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id) =>
        Ok(await service.GetAsync(id, UserId, IsAdmin));

    /// <summary>Cria uma tarefa para o usuário autenticado.</summary>
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(TaskRequest request)
    {
        var created = await service.CreateAsync(request, UserId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Atualiza todos os campos de uma tarefa.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, TaskRequest request) =>
        Ok(await service.UpdateAsync(id, request, UserId, IsAdmin));

    /// <summary>Altera apenas o status de uma tarefa.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(Guid id, UpdateStatusRequest request) =>
        Ok(await service.UpdateStatusAsync(id, request, UserId, IsAdmin));

    /// <summary>Remove uma tarefa.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id, UserId, IsAdmin);
        return NoContent();
    }
}
