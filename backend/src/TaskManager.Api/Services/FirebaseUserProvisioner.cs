using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

/// <summary>
/// Modo Firebase: após validar o ID token, (1) define o perfil (Admin se o e-mail estiver na lista
/// configurada, senão User) e (2) cria o registro local do usuário no primeiro acesso, pois as
/// tarefas têm chave estrangeira para a tabela de usuários.
/// </summary>
public static class FirebaseUserProvisioner
{
    public static async Task OnTokenValidatedAsync(TokenValidatedContext ctx, ISet<string> adminEmails)
    {
        var principal = ctx.Principal!;
        var uid = principal.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid))
        {
            ctx.Fail("Token sem identificação de usuário.");
            return;
        }

        var email = principal.FindFirstValue("email") ?? string.Empty;
        var name = principal.FindFirstValue("name");
        var fullName = !string.IsNullOrWhiteSpace(name) ? name : (email.Length > 0 ? email : uid);
        var role = adminEmails.Contains(email.ToLowerInvariant()) ? Roles.Admin : Roles.User;

        principal.Identities.First().AddClaim(new Claim("role", role));

        var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        if (await db.Users.AnyAsync(u => u.Id == uid)) return;

        var userName = email.Length > 0 ? email : uid;
        db.Users.Add(new ApplicationUser
        {
            Id = uid,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FullName = fullName,
            SecurityStamp = Guid.NewGuid().ToString()
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Requisições simultâneas no primeiro acesso: outra já criou o usuário.
            db.ChangeTracker.Clear();
        }
    }
}
