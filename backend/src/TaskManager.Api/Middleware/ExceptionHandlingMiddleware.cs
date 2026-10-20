using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Exceptions;

namespace TaskManager.Api.Middleware;

/// <summary>Converte exceções em respostas ProblemDetails (RFC 7807) padronizadas.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Dados inválidos."),
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                InvalidCredentialsException => (StatusCodes.Status401Unauthorized, ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acesso negado."),
                _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Erro não tratado em {Path}", context.Request.Path);

            if (context.Response.HasStarted) throw;

            var problem = new ProblemDetails { Status = status, Title = title, Instance = context.Request.Path };

            if (ex is ValidationException validation)
                problem.Extensions["errors"] = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
