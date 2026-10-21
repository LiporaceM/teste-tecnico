using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Dtos;
using TaskManager.Api.Exceptions;
using TaskManager.Api.Extensions;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    UserManager<ApplicationUser> users,
    ITokenService tokens,
    IValidator<RegisterRequest> registerValidator) : ControllerBase
{
    /// <summary>Cria uma conta com o perfil User e já retorna o token.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        await registerValidator.ValidateAndThrowAsync(request);

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(), Email = request.Email.Trim(), FullName = request.FullName.Trim()
        };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure(e.Code, e.Description)));

        await users.AddToRoleAsync(user, Roles.User);
        return Ok(BuildResponse(user, Roles.User));
    }

    /// <summary>Autentica o usuário e retorna o token JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email?.Trim() ?? string.Empty);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password ?? string.Empty))
            throw new InvalidCredentialsException();

        var roles = await users.GetRolesAsync(user);
        var role = roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User;
        return Ok(BuildResponse(user, role));
    }

    /// <summary>Retorna os dados e o perfil do usuário autenticado (funciona nos modos Local e Firebase).</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserInfo> Me() => Ok(new UserInfo(
        User.GetUserId(),
        User.FindFirstValue("email") ?? string.Empty,
        User.FindFirstValue("name") ?? User.FindFirstValue("email") ?? string.Empty,
        User.IsAdmin() ? Roles.Admin : Roles.User));

    private AuthResponse BuildResponse(ApplicationUser user, string role)
    {
        var (token, expires) = tokens.Create(user, role);
        return new AuthResponse(token, expires, new UserInfo(user.Id, user.Email ?? string.Empty, user.FullName, role));
    }
}
