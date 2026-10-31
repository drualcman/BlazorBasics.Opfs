using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>What the pages of the back/forward cache send here, since no automation can ask them.</summary>
internal sealed class BackForwardReports
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> Received = new();

    public void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/fixture/back-forward/{run}", async (string run, HttpContext context) =>
        {
            using StreamReader reader = new(context.Request.Body);
            SourceOf(run).TrySetResult(await reader.ReadToEndAsync());
        });
    }

    public Task<string> WaitForAsync(string run) => SourceOf(run).Task;

    private TaskCompletionSource<string> SourceOf(string run) =>
        Received.GetOrAdd(run, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
}
