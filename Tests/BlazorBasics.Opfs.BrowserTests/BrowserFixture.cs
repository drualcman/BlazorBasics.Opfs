using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// The server and Playwright for all the tests, and one run of the test app per engine. Profiles are persistent because
/// WebKit has no Origin Private File System in an ephemeral context.
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    private const float RUN_TIMEOUT_MILLISECONDS = 600_000;

    private readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyDictionary<string, BrowserTestOutcome>>>> Runs = new();
    private readonly List<string> ProfileFolders = [];
    private BrowserTestAppServer Server = null!;
    private IPlaywright Playwright = null!;

    public string Address => Server.Address;

    public async Task InitializeAsync()
    {
        Server = await BrowserTestAppServer.StartAsync();
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
    }

    public Task<IReadOnlyDictionary<string, BrowserTestOutcome>> OutcomesOfAsync(string engine) =>
        Runs.GetOrAdd(engine, name => new Lazy<Task<IReadOnlyDictionary<string, BrowserTestOutcome>>>(() => RunAsync(name))).Value;

    public async Task<IBrowserContext> NewContextAsync(string engine)
    {
        string profile = Path.Combine(Path.GetTempPath(), $"opfs-tests-{Guid.NewGuid():N}");
        ProfileFolders.Add(profile);
        return await Playwright[engine].LaunchPersistentContextAsync(profile, new BrowserTypeLaunchPersistentContextOptions { Headless = true, AcceptDownloads = true });
    }

    private async Task<IReadOnlyDictionary<string, BrowserTestOutcome>> RunAsync(string engine)
    {
        await using IBrowserContext context = await NewContextAsync(engine);
        IPage page = await context.NewPageAsync();
        await page.GotoAsync(Address);
        await page.WaitForFunctionAsync("() => globalThis.opfsContractResults !== undefined || globalThis.opfsFailed !== undefined",
            null, new PageWaitForFunctionOptions { Timeout = RUN_TIMEOUT_MILLISECONDS });
        string? json = await page.EvaluateAsync<string?>("() => globalThis.opfsContractResults ?? null");
        if (json is null)
        {
            throw new InvalidOperationException($"The test app failed: {await page.EvaluateAsync<string>("() => globalThis.opfsFailed")}");
        }
        return JsonSerializer.Deserialize<List<BrowserTestOutcome>>(json)!.ToDictionary(outcome => outcome.Name);
    }

    public async Task DisposeAsync()
    {
        Playwright.Dispose();
        await Server.DisposeAsync();
        foreach (string profile in ProfileFolders.Where(Directory.Exists))
        {
            try
            {
                Directory.Delete(profile, true);
            }
            catch (IOException)
            {
                Console.WriteLine($"Could not remove the test profile {profile}: a browser may still hold it.");
            }
        }
    }
}
