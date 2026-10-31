using System.Collections.Concurrent;
using System.Diagnostics;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// The test Blazor Web App in a process of its own, as it runs for real, with its console kept so a test can check that
/// the server had nothing to complain about.
/// </summary>
internal sealed class WebAppProcess : IAsyncDisposable
{
    private const string ASSEMBLY = "BlazorBasics.Opfs.WebApp.dll";
    private const string LISTENING_ON = "Now listening on: ";
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(60);
    private static readonly string[] FailureMarkers = ["fail:", "crit:"];

    private readonly Process Server;
    private readonly ConcurrentQueue<string> Output = new();
    private readonly ConcurrentQueue<string> Errors = new();
    private readonly TaskCompletionSource<string> Listening = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private WebAppProcess(Process server)
    {
        Server = server;
    }

    public string Address => Listening.Task.Result.TrimEnd('/') + "/";

    public IReadOnlyList<string> Failures =>
        [.. Errors, .. Output.Where(line => FailureMarkers.Any(marker => line.StartsWith(marker, StringComparison.Ordinal)))];

    public static async Task<WebAppProcess> StartAsync(string folder, string environment)
    {
        ProcessStartInfo start = new("dotnet")
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(Path.Combine(folder, ASSEMBLY));
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add("http://127.0.0.1:0");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;

        WebAppProcess application = new(new Process { StartInfo = start });
        await application.RunAsync();
        return application;
    }

    private async Task RunAsync()
    {
        Server.OutputDataReceived += (_, line) => KeepOutput(line.Data);
        Server.ErrorDataReceived += (_, line) => KeepError(line.Data);
        Server.Start();
        Server.BeginOutputReadLine();
        Server.BeginErrorReadLine();
        Task finished = await Task.WhenAny(Listening.Task, Server.WaitForExitAsync()).WaitAsync(StartTimeout);
        if (finished != Listening.Task)
        {
            throw new InvalidOperationException($"The web app did not start: {string.Join(Environment.NewLine, [.. Output, .. Errors])}");
        }
    }

    private void KeepOutput(string? line)
    {
        if (line is not null)
        {
            Output.Enqueue(line);
            int marker = line.IndexOf(LISTENING_ON, StringComparison.Ordinal);
            if (marker >= 0)
            {
                Listening.TrySetResult(line[(marker + LISTENING_ON.Length)..].Trim());
            }
        }
    }

    private void KeepError(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            Errors.Enqueue(line);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!Server.HasExited)
        {
            Server.Kill(true);
            await Server.WaitForExitAsync();
        }
        Server.Dispose();
    }
}
