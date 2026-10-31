using System.Diagnostics;
using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// The page of the manual checks, so a trip with a phone is not wasted on a broken page. What it measures here must match the
/// table of differences in the README. A real device comes back with its own numbers.
/// </summary>
[Collection(BrowserCollection.NAME)]
public sealed class DeviceChecksTests(BrowserFixture fixture)
{
    private const float LIMIT_MILLISECONDS = 600_000;
    private const string DEVICE_PAGE = "device.html";

    private static readonly TimeSpan ReportLimit = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan ReportPoll = TimeSpan.FromMilliseconds(500);

    public static TheoryData<string, string, string> Expected()
    {
        Dictionary<string, (string Snapshot, string Move)> engines = new()
        {
            [BrowserEngines.CHROMIUM] = ("same-length: NotReadableError", "on the destination: refused"),
            [BrowserEngines.FIREFOX] = ("same-length: AbortError", "on the destination: refused"),
            [BrowserEngines.WEBKIT] = ("same-length: read \"new\"", "on the destination: moved")
        };
        TheoryData<string, string, string> cases = [];
        foreach (string engine in BrowserEngines.Names)
        {
            cases.Add(engine, engines[engine].Snapshot, engines[engine].Move);
        }
        return cases;
    }

    public static TheoryData<string> Engines() => new(BrowserEngines.Names);

    [Theory]
    [MemberData(nameof(Expected))]
    public async Task The_measurements_show_what_the_engine_does(string engine, string snapshot, string move)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await OpenAsync(context, "measured device");

        await RunAsync(page, "#run-measurements");
        string summary = await page.TextContentAsync("#summary") ?? string.Empty;

        Assert.Contains("Probe: available", summary);
        Assert.Contains(snapshot, summary);
        Assert.Contains(move, summary);
        Assert.Contains(engine == BrowserEngines.WEBKIT ? "Estimate after removing: stayed-up" : "Estimate after removing: went-down", summary);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task The_battery_runs_from_the_page_and_the_report_reaches_the_server(string engine)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        string device = $"reported on {engine}";
        IPage page = await OpenAsync(context, device);

        await RunAsync(page, "#run-battery");
        string summary = await page.TextContentAsync("#summary") ?? string.Empty;

        Assert.Matches(@"Battery: ([1-9]\d*)/\1 passed", summary);
        Assert.Contains(fixture.DeviceReports(), report => report.Contains($"Device: {device}") && report.Contains("Battery: "));
    }

    [EngineFact(BrowserEngines.FIREFOX)]
    public async Task Firefox_in_private_mode_answers_with_a_reason()
    {
        await using IBrowserContext context = await fixture.OpenFirefoxPrivateAsync();
        IPage page = await OpenAsync(context, "Firefox in private mode");

        await RunAsync(page, "#run-measurements");
        string summary = await page.TextContentAsync("#summary") ?? string.Empty;
        Console.WriteLine(summary);

        Assert.Matches(@"Probe: (available|NOT available: \S)", summary);
    }

    [EngineFact(BrowserEngines.CHROMIUM)]
    public async Task The_back_forward_trips_come_back_to_the_page_in_order()
    {
        await using IBrowserContext context = await fixture.OpenChromiumWithBackForwardCacheAsync();
        IPage page = await OpenAsync(context, "trips");

        await page.ClickAsync("#run-back-forward");
        await page.WaitForFunctionAsync("() => document.getElementById('status')?.textContent === 'Finished.'", null,
            new PageWaitForFunctionOptions { Timeout = LIMIT_MILLISECONDS });
        string summary = await page.TextContentAsync("#summary") ?? string.Empty;

        Assert.DoesNotContain("INCONCLUSIVE", summary);
        Assert.Contains("control, no lock: restored, holder=false", summary);
        Assert.Contains("held: restored, holder=true, held=true, lost=false, name free=false", summary);
        Assert.Contains("held, name taken away: loaded again", summary);
    }

    [ContainerFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_comes_back_to_the_page_and_reports_the_lost_lock()
    {
        using Process browser = UnautomatedWebKit.Open($"{fixture.Address.TrimEnd('/')}/{DEVICE_PAGE}?start=back-forward");
        try
        {
            string? report = await WaitForReportAsync("held, name taken away: restored");

            Assert.NotNull(report);
            Assert.DoesNotContain("INCONCLUSIVE", report);
            Assert.Contains("control, no lock: restored, holder=false", report);
            Assert.Contains("held: restored, holder=true, held=false, lost=true, name free=true", report);
            Assert.Contains("held, name taken away: restored, holder=true, held=false, lost=true, name free=true", report);
        }
        finally
        {
            browser.Kill(entireProcessTree: true);
        }
    }

    private async Task<string?> WaitForReportAsync(string expected)
    {
        Stopwatch waited = Stopwatch.StartNew();
        string? report = null;
        while (report is null && waited.Elapsed < ReportLimit)
        {
            await Task.Delay(ReportPoll);
            report = fixture.DeviceReports().FirstOrDefault(candidate => candidate.Contains(expected));
        }
        return report;
    }

    private async Task<IPage> OpenAsync(IBrowserContext context, string device)
    {
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.Address.TrimEnd('/')}/{DEVICE_PAGE}");
        await page.FillAsync("#device", device);
        await page.WaitForFunctionAsync("() => globalThis.opfsDeviceReady === true", null, new PageWaitForFunctionOptions { Timeout = LIMIT_MILLISECONDS });
        return page;
    }

    private static async Task RunAsync(IPage page, string button)
    {
        await page.ClickAsync(button);
        await page.WaitForFunctionAsync("() => document.getElementById('status').textContent === 'Finished.'", null,
            new PageWaitForFunctionOptions { Timeout = LIMIT_MILLISECONDS });
    }
}
