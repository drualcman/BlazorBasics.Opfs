using System.Text.Json;
using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary><c>opfs.js</c> alone in a JavaScript worker, with no .NET, as a module worker and as a classic one.</summary>
[Collection(BrowserCollection.NAME)]
public sealed class ScriptWorkerTests(BrowserFixture fixture)
{
    private const string RUN_WORKER = """
        type => new Promise((resolve, reject) => {
            const worker = new Worker('/workers/opfs-script-worker.js', { type });
            worker.onmessage = ({ data }) => data.failure === undefined ? resolve(JSON.stringify(data.results)) : reject(new Error(data.failure));
            worker.onerror = event => reject(new Error(event.message || 'The worker did not start.'));
        })
        """;

    private const string FETCH_WORKER_URL = """
        () => new Promise((resolve, reject) => {
            const worker = new Worker('/workers/opfs-url-worker.js', { type: 'module' });
            worker.onmessage = async ({ data }) => resolve(await (await fetch(data)).text());
            worker.onerror = event => reject(new Error(event.message || 'The worker did not start.'));
        })
        """;

    public static TheoryData<string> Engines() => new(BrowserEngines.Names);

    public static TheoryData<string, string> EnginesAndWorkerTypes()
    {
        TheoryData<string, string> cases = [];
        foreach (string engine in BrowserEngines.Names)
        {
            cases.Add(engine, "module");
            cases.Add(engine, "classic");
        }
        return cases;
    }

    [Theory]
    [MemberData(nameof(EnginesAndWorkerTypes))]
    public async Task The_javascript_module_works_in_a_worker(string engine, string workerType)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await OpenBlankPageAsync(context);

        string json = await page.EvaluateAsync<string>(RUN_WORKER, workerType);
        List<BrowserTestOutcome> outcomes = JsonSerializer.Deserialize<List<BrowserTestOutcome>>(json)!;

        Assert.NotEmpty(outcomes);
        Assert.All(outcomes, outcome => Assert.True(outcome.Passed, $"{outcome.Name}: {outcome.Failure}"));
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task A_page_reads_an_object_url_made_in_a_worker(string engine)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await OpenBlankPageAsync(context);

        Assert.Equal("from the worker", await page.EvaluateAsync<string>(FETCH_WORKER_URL));
    }

    private async Task<IPage> OpenBlankPageAsync(IBrowserContext context)
    {
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.Address.TrimEnd('/')}/blank.html");
        return page;
    }
}
