using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // O Postgres pode demorar alguns segundos para aceitar conexões no docker-compose.
        for (var attempt = 1; ; attempt++)
        {
            try { await db.Database.EnsureCreatedAsync(); break; }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning("Banco indisponível (tentativa {Attempt}/10): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        // No modo Firebase os usuários vêm do Firebase; os dados de demonstração locais não se aplicam.
        if (string.Equals(config["Auth:Provider"], "Firebase", StringComparison.OrdinalIgnoreCase)) return;
        if (!config.GetValue("SeedData:Enabled", true)) return;

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Admin, Roles.User })
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await EnsureUserAsync(users, "admin@empresa.com", "Ana Administradora", "Admin@123", Roles.Admin);
        var user = await EnsureUserAsync(users, "usuario@empresa.com", "Carlos Usuário", "User@123", Roles.User);

        if (await db.Tasks.AnyAsync()) return;

        var now = DateTime.UtcNow;
        TaskItem T(string owner, string title, string? desc, TaskItemStatus s, TaskPriority p, int? dueInDays) => new()
        {
            UserId = owner, Title = title, Description = desc, Status = s, Priority = p,
            DueDate = dueInDays is null ? null : now.Date.AddDays(dueInDays.Value),
            CreatedAt = now.AddDays(-Random.Shared.Next(1, 20)), UpdatedAt = now
        };

        db.Tasks.AddRange(
            T(user.Id, "Revisar contrato do fornecedor", "Conferir cláusulas de reajuste e multa rescisória.", TaskItemStatus.InProgress, TaskPriority.High, 2),
            T(user.Id, "Atualizar relatório mensal de vendas", "Incluir comparativo com o mesmo mês do ano anterior.", TaskItemStatus.Pending, TaskPriority.Medium, 5),
            T(user.Id, "Agendar reunião de alinhamento com o time", null, TaskItemStatus.Pending, TaskPriority.Low, 7),
            T(user.Id, "Homologar nova versão do sistema financeiro", "Executar roteiro de testes e registrar evidências.", TaskItemStatus.InProgress, TaskPriority.High, 1),
            T(user.Id, "Responder chamados em aberto do suporte", "Priorizar os chamados com SLA próximo do vencimento.", TaskItemStatus.Pending, TaskPriority.High, -1),
            T(user.Id, "Preparar apresentação para a diretoria", "Resumo executivo dos resultados do trimestre.", TaskItemStatus.Completed, TaskPriority.High, -3),
            T(user.Id, "Solicitar acesso à VPN para novo colaborador", null, TaskItemStatus.Completed, TaskPriority.Low, -6),
            T(user.Id, "Revisar documentação da API interna", "Atualizar exemplos de requisição no Swagger.", TaskItemStatus.Pending, TaskPriority.Medium, 10),
            T(user.Id, "Planejar treinamento de segurança da informação", null, TaskItemStatus.Pending, TaskPriority.Medium, 14),
            T(user.Id, "Conciliar notas fiscais do mês", "Cruzar com o extrato do ERP.", TaskItemStatus.InProgress, TaskPriority.Medium, 3),
            T(admin.Id, "Aprovar orçamento de infraestrutura", "Validar proposta de ampliação do cluster.", TaskItemStatus.Pending, TaskPriority.High, 4),
            T(admin.Id, "Revisar permissões dos usuários do sistema", "Remover acessos de colaboradores desligados.", TaskItemStatus.InProgress, TaskPriority.Medium, 6),
            T(admin.Id, "Publicar política de uso de dispositivos", null, TaskItemStatus.Completed, TaskPriority.Low, -10));

        await db.SaveChangesAsync();
        logger.LogInformation("Dados de exemplo criados.");
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> users, string email, string name, string password, string role)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
        return user;
    }
}
