using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>The routes of <c>FetchContractTests</c>.</summary>
internal static class FetchFixtureRoutes
{
    private const int CHUNK_LENGTH = 64 * 1024;

    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/fixture/status/{code:int}", (int code) => Results.StatusCode(code));
        routes.MapGet("/fixture/bytes/{length:long}", async (long length, HttpContext context) =>
        {
            context.Response.ContentLength = length;
            await SendAsync(context, length);
        });
        routes.MapGet("/fixture/chunked/{length:long}", (long length, HttpContext context) => SendAsync(context, length));
        routes.MapGet("/fixture/cut/{length:long}", async (long length, HttpContext context) =>
        {
            await SendAsync(context, length);
            context.Abort();
        });
        routes.MapGet("/fixture/stall/{length:long}", async (long length, HttpContext context) =>
        {
            await SendAsync(context, length);
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
        });
    }

    private static async Task SendAsync(HttpContext context, long length)
    {
        byte[] chunk = new byte[CHUNK_LENGTH];
        long remaining = length;
        while (remaining > 0)
        {
            int count = (int)Math.Min(remaining, chunk.Length);
            await context.Response.Body.WriteAsync(chunk.AsMemory(0, count), context.RequestAborted);
            remaining -= count;
        }
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }
}
