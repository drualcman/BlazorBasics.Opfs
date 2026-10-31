using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// A client component of a real Blazor Web App, against a server in its own process. The HTML of the navigation is what
/// prerendering produced on the server, and the page afterwards is what the interactive phase made of it.
/// </summary>
[Collection(BrowserCollection.NAME)]
public sealed class WebAppTests(BrowserFixture fixture, WebAppHosts hosts) : IClassFixture<WebAppHosts>
{
    private const string NOT_SUPPORTED = "OpfsNotSupportedException";
    private const string WRITTEN_TEXT = "written in the interactive phase";
    private const float INTERACTIVE_TIMEOUT_MILLISECONDS = 60_000;
    private static readonly TimeSpan RuntimeDownloadTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RevisitDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ServerSettleDelay = TimeSpan.FromSeconds(1);

    private const string READ_FROM_THE_BROWSER = """
        async () => {
            const root = await navigator.storage.getDirectory();
            const folder = await root.getDirectoryHandle('prerender');
            const file = await (await folder.getFileHandle('after-render.txt')).getFile();
            return await file.text();
        }
        """;

    public static TheoryData<string, string> EnginesAndHosts()
    {
        TheoryData<string, string> cases = [];
        foreach (string engine in BrowserEngines.Names)
        {
            cases.Add(engine, WebAppHosts.BUILT);
            cases.Add(engine, WebAppHosts.PUBLISHED);
        }
        return cases;
    }

    [Theory]
    [MemberData(nameof(EnginesAndHosts))]
    public async Task Prerendering_runs_on_the_server_and_the_package_answers_clearly(string engine, string host)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await context.NewPageAsync();
        PageProblems problems = new(page);

        IResponse response = (await page.GotoAsync($"{hosts.Of(host).Address}webassembly"))!;
        string prerendered = await response.TextAsync();

        Assert.Equal(200, response.Status);
        Assert.Equal("server", ElementText(prerendered, "host"));
        Assert.True(int.Parse(ElementText(prerendered, "package-types")) > 0);
        Assert.Equal("False", ElementText(prerendered, "probe-available"));
        Assert.Equal(NOT_SUPPORTED, ElementText(prerendered, "write-failure-type"));
        Assert.Equal(NOT_SUPPORTED, ElementText(prerendered, "lock-failure-type"));
        Assert.Equal(NOT_SUPPORTED, ElementText(prerendered, "estimate-failure-type"));
        string message = ElementText(prerendered, "write-failure-message");
        Assert.Contains("prerendering", message);
        Assert.Contains("InteractiveWebAssembly", message);
        Assert.Contains("OnAfterRenderAsync", message);

        await TextOfAsync(page, "after-render-text", WRITTEN_TEXT);
        await AssertNoProblemsAsync(context, problems, host);
    }

    [Theory]
    [MemberData(nameof(EnginesAndHosts))]
    public async Task The_interactive_phase_of_a_prerendered_component_works(string engine, string host)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await context.NewPageAsync();
        PageProblems problems = new(page);

        await page.GotoAsync($"{hosts.Of(host).Address}webassembly");

        await TextOfAsync(page, "after-render-text", WRITTEN_TEXT);
        await TextOfAsync(page, "host", "browser");
        await TextOfAsync(page, "probe-available", "True");
        await TextOfAsync(page, "after-render-listing", "after-render.txt,on-initialized.txt");
        await TextOfAsync(page, "after-render-failure", string.Empty);
        await TextOfAsync(page, "write-failure-type", string.Empty);
        Assert.Equal(WRITTEN_TEXT, await page.EvaluateAsync<string>(READ_FROM_THE_BROWSER));
        await AssertNoProblemsAsync(context, problems, host);
    }

    [Theory]
    [MemberData(nameof(EnginesAndHosts))]
    public async Task The_first_visit_of_InteractiveAuto_gets_an_answer_and_keeps_the_circuit(string engine, string host)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await context.NewPageAsync();
        PageProblems problems = new(page);

        await page.GotoAsync($"{hosts.Of(host).Address}auto");

        await TextOfAsync(page, "probe-answered", "True");
        await TextOfAsync(page, "host", "server");
        await TextOfAsync(page, "probe-available", "False");
        await TextOfAsync(page, "probe-reason", "This is not running in a browser.");
        await TextOfAsync(page, "probe-failure-type", NOT_SUPPORTED);
        await page.ClickAsync("#count");
        await TextOfAsync(page, "clicks", "1");
        await page.ClickAsync("#count");
        await TextOfAsync(page, "clicks", "2");

        await AssertNoProblemsAsync(context, problems, host);
    }

    [Theory]
    [MemberData(nameof(EnginesAndHosts))]
    public async Task A_later_visit_of_InteractiveAuto_runs_in_the_browser(string engine, string host)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await context.NewPageAsync();
        PageProblems problems = new(page);

        await page.GotoAsync($"{hosts.Of(host).Address}auto");
        await TextOfAsync(page, "probe-answered", "True");
        string runsOn = await page.TextContentAsync("#host") ?? string.Empty;
        DateTime giveUpAt = DateTime.UtcNow + RuntimeDownloadTimeout;
        while (runsOn != "browser" && DateTime.UtcNow < giveUpAt)
        {
            await Task.Delay(RevisitDelay);
            await page.ReloadAsync();
            await TextOfAsync(page, "probe-answered", "True");
            runsOn = await page.TextContentAsync("#host") ?? string.Empty;
        }

        Assert.Equal("browser", runsOn);
        await TextOfAsync(page, "probe-available", "True");
        await AssertNoProblemsAsync(context, problems, host);
    }

    private async Task AssertNoProblemsAsync(IBrowserContext context, PageProblems problems, string host)
    {
        Assert.Empty(problems.Seen);
        await context.CloseAsync();
        await Task.Delay(ServerSettleDelay);
        Assert.Empty(hosts.Of(host).Failures);
    }

    private static Task TextOfAsync(IPage page, string id, string text) =>
        Assertions.Expect(page.Locator($"#{id}")).ToHaveTextAsync(text, new LocatorAssertionsToHaveTextOptions { Timeout = INTERACTIVE_TIMEOUT_MILLISECONDS });

    private static string ElementText(string html, string id)
    {
        Match element = Regex.Match(html, $"<p id=\"{id}\">(.*?)</p>", RegexOptions.Singleline);
        Assert.True(element.Success, $"The prerendered page has no element {id}.");
        return WebUtility.HtmlDecode(element.Groups[1].Value);
    }
}
