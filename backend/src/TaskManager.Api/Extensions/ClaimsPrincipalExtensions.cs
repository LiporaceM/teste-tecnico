using System.Security.Claims;
using TaskManager.Api.Models;

namespace TaskManager.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub") ?? throw new UnauthorizedAccessException("Token sem identificação de usuário.");

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
