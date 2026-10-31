using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// A held lock through a real trip to the back/forward cache, with real .NET. Firefox is not here: it never enters the cache
/// with a lock held, and under Playwright not even without one.
/// </summary>
[Collection(BrowserCollection.NAME)]
public sealed class BackForwardCacheTests(BrowserFixture fixture)
{
    private static readonly TimeSpan ReportLimit = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions ReportJson = new(JsonSerializerDefaults.Web);

    [EngineFact(BrowserEngines.CHROMIUM)]
    public async Task Chromium_keeps_a_lock_nobody_asks_for()
    {
        await using IBrowserContext context = await fixture.OpenChromiumWithBackForwardCacheAsync();
        string run = NewRun();

        await (await context.NewPageAsync()).GotoAsync(AddressOf(run, compete: false));
        BackForwardLockReport report = await ReportOfAsync<BackForwardLockReport>(run);

        Assert.True(report.Restored);
        Assert.True(report.IsHeld);
        Assert.False(report.IsLost);
        Assert.False(report.CanTakeTheName);
    }

    [EngineFact(BrowserEngines.CHROMIUM)]
    public async Task Chromium_gives_the_lock_away_by_throwing_the_page_out()
    {
        await using IBrowserContext context = await fixture.OpenChromiumWithBackForwardCacheAsync();
        string run = NewRun();

        await (await context.NewPageAsync()).GotoAsync(AddressOf(run, compete: true));
        BackForwardAwayReport away = await ReportOfAsync<BackForwardAwayReport>($"{run}-away");
        BackForwardLockReport report = await ReportOfAsync<BackForwardLockReport>(run);

        Assert.True(away.Granted);
        Assert.False(report.Restored);
        Assert.False(report.HasHolder);
    }

    [ContainerFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_takes_the_lock_from_a_page_it_keeps_and_the_holder_finds_out()
    {
        string run = NewRun();

        using Process browser = UnautomatedWebKit.Open(AddressOf(run, compete: true));
        try
        {
            BackForwardAwayReport away = await ReportOfAsync<BackForwardAwayReport>($"{run}-away");
            BackForwardLockReport report = await ReportOfAsync<BackForwardLockReport>(run);

            Assert.True(away.Granted);
            Assert.True(report.Restored);
            Assert.True(report.HasHolder);
            Assert.False(report.IsHeld);
            Assert.True(report.IsLost);
            Assert.True(report.CanTakeTheName);
        }
        finally
        {
            browser.Kill(entireProcessTree: true);
        }
    }

    private static string NewRun() => Guid.NewGuid().ToString("N");

    private string AddressOf(string run, bool compete) =>
        $"{fixture.Address.TrimEnd('/')}/back-forward.html?run={run}&compete={compete.ToString().ToLowerInvariant()}";

    private async Task<TReport> ReportOfAsync<TReport>(string run)
    {
        string json = await fixture.BackForwardReportAsync(run).WaitAsync(ReportLimit);
        return JsonSerializer.Deserialize<TReport>(json, ReportJson)!;
    }
}
