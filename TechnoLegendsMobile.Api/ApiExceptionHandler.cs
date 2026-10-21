using System.Net.Sockets;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TechnoLegendsMobile.Api;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        log.LogError(ex, "Unhandled exception on {Path}", ctx.Request.Path);

        var (status, detail) = ex switch
        {
            _ when IsDbDown(ex) => (503, "The database is unavailable right now. Try again shortly."),
            DbUpdateException => (409, "That change couldn't be saved. It may conflict with existing data."),
            _ => (500, "Something went wrong on the server.")
        };

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = "Request failed", Detail = detail }, ct);
        return true;
    }

    private static bool IsDbDown(Exception? ex)
    {
        for (; ex is not null; ex = ex.InnerException)
            if (ex is NpgsqlException or SocketException or TimeoutException) return true;
        return false;
    }
}