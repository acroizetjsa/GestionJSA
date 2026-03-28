using System.Net;
using System.Text.Json;

namespace Gmao.Api.Common.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            await WriteProblemAsync(context, HttpStatusCode.Forbidden, "Accès refusé", exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "Ressource introuvable", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Opération invalide", exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erreur non gérée.");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Erreur serveur", "Une erreur inattendue s'est produite.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        var payload = new
        {
            type = "about:blank",
            title,
            status = (int)statusCode,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
