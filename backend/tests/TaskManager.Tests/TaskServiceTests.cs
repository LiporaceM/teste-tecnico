using Xunit;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos;
using TaskManager.Api.Exceptions;
using TaskManager.Api.Models;
using TaskManager.Api.Services;
using TaskManager.Api.Validators;

namespace TaskManager.Tests;

public class TaskServiceTests
{
    private const string Alice = "user-alice";
    private const string Bob = "user-bob";

    private static (TaskService Service, AppDbContext Db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Users.AddRange(
            new ApplicationUser { Id = Alice, UserName = "alice", FullName = "Alice" },
            new ApplicationUser { Id = Bob, UserName = "bob", FullName = "Bob" });
        db.SaveChanges();

        return (new TaskService(db, new TaskRequestValidator(), new UpdateStatusRequestValidator()), db);
    }

    private static TaskRequest Req(string title = "Tarefa", TaskItemStatus status = TaskItemStatus.Pending) =>
        new(title, "descrição", status, TaskPriority.Medium, null);

    [Fact]
    public async Task Create_AssignsOwnerAndTrimsTitle()
    {
        var (service, _) = Build();

        var created = await service.CreateAsync(Req("  Revisar contrato  "), Alice);

        Assert.Equal("Revisar contrato", created.Title);
        Assert.Equal("Alice", created.OwnerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithEmptyTitle_ThrowsValidation(string title)
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(Req(title), Alice));
    }

    [Fact]
    public async Task Create_WithTitleOver150Chars_ThrowsValidation()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(Req(new string('a', 151)), Alice));
    }

    [Fact]
    public async Task List_ForRegularUser_ReturnsOnlyOwnTasks()
    {
        var (service, _) = Build();
        await service.CreateAsync(Req("da Alice"), Alice);
        await service.CreateAsync(Req("do Bob"), Bob);

        var result = await service.ListAsync(Alice, isAdmin: false, new TaskFilter());

        Assert.Single(result.Items);
        Assert.Equal("da Alice", result.Items[0].Title);
    }

    [Fact]
    public async Task List_ForAdmin_ReturnsAllTasks()
    {
        var (service, _) = Build();
        await service.CreateAsync(Req("da Alice"), Alice);
        await service.CreateAsync(Req("do Bob"), Bob);

        var result = await service.ListAsync("admin", isAdmin: true, new TaskFilter());

        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public async Task List_FiltersByStatusAndSearch()
    {
        var (service, _) = Build();
        await service.CreateAsync(Req("Relatório mensal"), Alice);
        await service.CreateAsync(Req("Relatório anual", TaskItemStatus.Completed), Alice);
        await service.CreateAsync(Req("Reunião", TaskItemStatus.Completed), Alice);

        var result = await service.ListAsync(Alice, false,
            new TaskFilter { Search = "RELATÓRIO", Status = TaskItemStatus.Completed });

        Assert.Single(result.Items);
        Assert.Equal("Relatório anual", result.Items[0].Title);
    }

    [Fact]
    public async Task List_Paginates()
    {
        var (service, _) = Build();
        for (var i = 0; i < 5; i++) await service.CreateAsync(Req($"Tarefa {i}"), Alice);

        var page2 = await service.ListAsync(Alice, false, new TaskFilter { Page = 2, PageSize = 2 });

        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(5, page2.TotalItems);
        Assert.Equal(3, page2.TotalPages);
    }

    [Fact]
    public async Task Get_TaskOfAnotherUser_ThrowsNotFound()
    {
        var (service, _) = Build();
        var bobTask = await service.CreateAsync(Req(), Bob);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(bobTask.Id, Alice, isAdmin: false));
    }

    [Fact]
    public async Task UpdateStatus_ChangesStatus()
    {
        var (service, _) = Build();
        var task = await service.CreateAsync(Req(), Alice);

        var updated = await service.UpdateStatusAsync(task.Id, new UpdateStatusRequest(TaskItemStatus.Completed), Alice, false);

        Assert.Equal(TaskItemStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task Update_ByAnotherUser_ThrowsNotFound()
    {
        var (service, _) = Build();
        var task = await service.CreateAsync(Req(), Bob);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(task.Id, Req("hack"), Alice, false));
    }

    [Fact]
    public async Task Delete_RemovesTask_AndAdminCanDeleteAnyTask()
    {
        var (service, db) = Build();
        var task = await service.CreateAsync(Req(), Bob);

        await service.DeleteAsync(task.Id, "admin", isAdmin: true);

        Assert.Empty(await db.Tasks.ToListAsync());
    }

    [Fact]
    public async Task Summary_CountsByStatus()
    {
        var (service, _) = Build();
        await service.CreateAsync(Req(status: TaskItemStatus.Pending), Alice);
        await service.CreateAsync(Req(status: TaskItemStatus.Completed), Alice);
        await service.CreateAsync(Req(status: TaskItemStatus.Completed), Alice);

        var summary = await service.SummaryAsync(Alice, false);

        Assert.Equal(new TaskSummary(3, 1, 0, 2), summary);
    }
}
