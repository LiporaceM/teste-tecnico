using Microsoft.AspNetCore.Identity;

namespace TaskManager.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
