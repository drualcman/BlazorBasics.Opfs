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
    private const string WORKER_PAGE = "worker.html";

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

    public Task<IReadOnlyDictionary<string, BrowserTestOutcome>> OutcomesOfAsync(string engine) => RunOnceAsync(engine, string.Empty);

    public Task<IReadOnlyDictionary<string, BrowserTestOutcome>> WorkerOutcomesOfAsync(string engine) => RunOnceAsync(engine, WORKER_PAGE);

    public Task<IBrowserContext> NewContextAsync(string engine) => OpenProfileAsync(engine, NewProfile(Path.GetTempPath()));

    public string NewProfile(string parentFolder)
    {
        string profile = Path.Combine(parentFolder, $"opfs-tests-{Guid.NewGuid():N}");
        ProfileFolders.Add(profile);
        return profile;
    }

    public string NewLimitedProfile(string engine, string? parentFolder = null)
    {
        string profile = NewProfile(parentFolder ?? Path.GetTempPath());
        if (engine == BrowserEngines.FIREFOX)
        {
            Directory.CreateDirectory(profile);
            File.WriteAllText(Path.Combine(profile, StorageLimits.FIREFOX_USER_FILE), StorageLimits.FIREFOX_USER_PREFERENCE);
        }
        return profile;
    }

    public async Task<IBrowserContext> OpenProfileAsync(string engine, string profile, string? temporaryFolder = null)
    {
        BrowserTypeLaunchPersistentContextOptions options = new() { Headless = true, AcceptDownloads = true };
        if (temporaryFolder is not null)
        {
            options.Env = new Dictionary<string, string> { ["TMPDIR"] = temporaryFolder };
        }
        return await Playwright[engine].LaunchPersistentContextAsync(profile, options);
    }

    public Task<IBrowserContext> OpenChromiumWithBackForwardCacheAsync() =>
        Playwright[BrowserEngines.CHROMIUM].LaunchPersistentContextAsync(NewProfile(Path.GetTempPath()), new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = true,
            Channel = BrowserEngines.CHROMIUM,
            IgnoreDefaultArgs = ["--disable-back-forward-cache"]
        });

    public Task<IBrowserContext> OpenFirefoxPrivateAsync() =>
        Playwright[BrowserEngines.FIREFOX].LaunchPersistentContextAsync(NewProfile(Path.GetTempPath()), new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = true,
            FirefoxUserPrefs = new Dictionary<string, object> { ["browser.privatebrowsing.autostart"] = true }
        });

    internal Task<string> BackForwardReportAsync(string run) => Server.BackForward.WaitForAsync(run);

    public IEnumerable<string> DeviceReports() => Directory.GetFiles(Server.ReportsFolder).Select(File.ReadAllText);

    public async Task<IPage> OpenLimitedPageAsync(IBrowserContext context, string engine, string relativeAddress)
    {
        IPage page = await context.NewPageAsync();
        if (engine == BrowserEngines.CHROMIUM)
        {
            ICDPSession session = await context.NewCDPSessionAsync(page);
            await session.SendAsync("Storage.overrideQuotaForOrigin", new Dictionary<string, object>
            {
                ["origin"] = Address.TrimEnd('/'),
                ["quotaSize"] = StorageLimits.CHROMIUM_QUOTA_BYTES
            });
        }
        await page.GotoAsync($"{Address.TrimEnd('/')}/{relativeAddress}");
        return page;
    }

    public async Task<IReadOnlyDictionary<string, BrowserTestOutcome>> OutcomesOnAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => globalThis.opfsContractResults !== undefined || globalThis.opfsFailed !== undefined",
            null, new PageWaitForFunctionOptions { Timeout = RUN_TIMEOUT_MILLISECONDS });
        string? json = await page.EvaluateAsync<string?>("() => globalThis.opfsContractResults ?? null");
        if (json is null)
        {
            throw new InvalidOperationException($"The test app failed: {await page.EvaluateAsync<string>("() => globalThis.opfsFailed")}");
        }
        return JsonSerializer.Deserialize<List<BrowserTestOutcome>>(json)!.ToDictionary(outcome => outcome.Name);
    }

    private Task<IReadOnlyDictionary<string, BrowserTestOutcome>> RunOnceAsync(string engine, string relativeAddress) =>
        Runs.GetOrAdd($"{engine}/{relativeAddress}", _ => new Lazy<Task<IReadOnlyDictionary<string, BrowserTestOutcome>>>(() => RunAsync(engine, relativeAddress))).Value;

    private async Task<IReadOnlyDictionary<string, BrowserTestOutcome>> RunAsync(string engine, string relativeAddress)
    {
        await using IBrowserContext context = await NewContextAsync(engine);
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{Address.TrimEnd('/')}/{relativeAddress}");
        return await OutcomesOnAsync(page);
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
