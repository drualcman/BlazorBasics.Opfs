using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>What each engine does when the quota of the origin runs out, with a browser of its own per test.</summary>
[Collection(BrowserCollection.NAME)]
public sealed class QuotaTests(BrowserFixture fixture)
{
    private const string BLANK_PAGE = "blank.html";
    private const string FOLDER = "quota";
    private const double MEBIBYTE = 1024 * 1024;

    private static readonly string[] Routes = ["A_writer_over_an_existing_file", "A_writer_for_a_new_file", "Writing_bytes", "A_copy", "A_download"];

    public static TheoryData<string> Engines() => new(BrowserEngines.Names);

    public static TheoryData<string, string> EnginesAndRoutes()
    {
        TheoryData<string, string> cases = [];
        foreach (string engine in BrowserEngines.Names)
        {
            foreach (string route in Routes)
            {
                cases.Add(engine, route);
            }
        }
        return cases;
    }

    [Theory]
    [MemberData(nameof(EnginesAndRoutes))]
    public async Task A_way_of_writing_throws_the_typed_exception_in_dotnet_and_leaves_no_trace(string engine, string route)
    {
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        string address = $"?quota={route}&limit={StorageLimits.MebibytesOf(engine)}";

        IPage page = await fixture.OpenLimitedPageAsync(context, engine, address);
        IReadOnlyDictionary<string, BrowserTestOutcome> outcomes = await fixture.OutcomesOnAsync(page);

        BrowserTestOutcome outcome = Assert.Single(outcomes.Values);
        Assert.True(outcome.Passed, outcome.Failure);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task Exhaustion_surfaces_at_write_and_abort_leaves_the_file_as_it_was(string engine)
    {
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        await QuotaPageScripts.WriteTextAsync(page, FOLDER, "a.txt", "previous");

        string result = await QuotaPageScripts.WriteAsync(page, FOLDER, "a.txt", StorageLimits.MebibytesOf(engine) * 3 / 2);

        Assert.Equal("write QuotaExceededError", result);
        Assert.Equal("previous", await QuotaPageScripts.ReadTextAsync(page, FOLDER, "a.txt"));
        Assert.Equal(["a.txt"], await QuotaPageScripts.NamesAsync(page, FOLDER));
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task A_blob_in_memory_reports_the_quota_through_the_public_javascript(string engine)
    {
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        await QuotaPageScripts.WriteTextAsync(page, FOLDER, "a.txt", "previous");

        string failure = await QuotaPageScripts.WriteBlobAsync(page, FOLDER, "a.txt", StorageLimits.MebibytesOf(engine) * 3 / 2);

        Assert.StartsWith("QuotaExceededError:", failure);
        Assert.Equal("previous", await QuotaPageScripts.ReadTextAsync(page, FOLDER, "a.txt"));
        Assert.Equal(["a.txt"], await QuotaPageScripts.NamesAsync(page, FOLDER));
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task Closing_a_page_mid_write_gives_the_space_back(string engine)
    {
        int limit = StorageLimits.MebibytesOf(engine);
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage writing = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        await QuotaPageScripts.WriteTextAsync(writing, FOLDER, "a.txt", "previous");
        await writing.EvaluateAsync($$"""
            async mebibytes => {
                const opfs = {{QuotaPageScripts.MODULE}};
                globalThis.open = await opfs.openWriter(['{{FOLDER}}', 'a.txt'], false);
                for (let written = 0; written < mebibytes; written++) {
                    await globalThis.open.writable.write(new Uint8Array(1024 * 1024));
                }
            }
            """, limit * 6 / 10);

        await writing.CloseAsync();
        IPage next = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        string result = await WriteWhenFreedAsync(next, limit * 8 / 10);

        Assert.Equal("written", result);
        Assert.Equal("previous", await QuotaPageScripts.ReadTextAsync(next, FOLDER, "a.txt"));
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task Replacing_a_file_over_half_the_quota_fails_unless_the_swap_stays_out_of_it(string engine)
    {
        int size = StorageLimits.MebibytesOf(engine) * 6 / 10;
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        Assert.Equal("written", await QuotaPageScripts.WriteAsync(page, FOLDER, "big.bin", size));

        string result = await QuotaPageScripts.WriteAsync(page, FOLDER, "big.bin", size);

        Assert.Equal(engine == BrowserEngines.WEBKIT ? "written" : "write QuotaExceededError", result);
        Assert.Equal(["big.bin"], await QuotaPageScripts.NamesAsync(page, FOLDER));
    }

    [EngineFact(BrowserEngines.CHROMIUM)]
    public async Task Chromium_refuses_keeping_existing_data_at_createWritable_and_the_package_removes_the_swap_it_leaves()
    {
        string engine = BrowserEngines.CHROMIUM;
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        await QuotaPageScripts.WriteAsync(page, FOLDER, "big.bin", 9);

        string rawResult = await QuotaPageScripts.WriteAsync(page, FOLDER, "big.bin", 1, true);
        string[] namesAfterTheBrowser = await QuotaPageScripts.NamesAsync(page, FOLDER);
        await QuotaPageScripts.RemoveFileAsync(page, FOLDER, "big.bin.crswap");
        await QuotaPageScripts.WriteAsync(page, FOLDER, "big.bin", 9);
        string failure = await QuotaPageScripts.AppendAsync(page, FOLDER, "big.bin", 1);

        Assert.Equal("createWritable QuotaExceededError", rawResult);
        Assert.Equal(["big.bin", "big.bin.crswap"], namesAfterTheBrowser);
        Assert.StartsWith("QuotaExceededError:", failure);
        Assert.Equal(["big.bin"], await QuotaPageScripts.NamesAsync(page, FOLDER));
    }

    [EngineFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_refuses_replacing_a_file_over_half_the_quota_through_the_package_and_keeps_it()
    {
        string engine = BrowserEngines.WEBKIT;
        int size = StorageLimits.MebibytesOf(engine) * 6 / 10;
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        Assert.Equal("no failure", await QuotaPageScripts.WriteBlobAsync(page, FOLDER, "big.bin", size));

        string failure = await QuotaPageScripts.WriteBlobAsync(page, FOLDER, "big.bin", size);

        Assert.StartsWith("QuotaExceededError:", failure);
        Assert.Equal(["big.bin"], await QuotaPageScripts.NamesAsync(page, FOLDER));
        Assert.Equal(size * MEBIBYTE, await page.EvaluateAsync<double>($"async () => {{ const opfs = {QuotaPageScripts.MODULE}; return (await opfs.requireFile(['{FOLDER}', 'big.bin'])).size; }}"));
    }

    [EngineFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_refuses_appending_to_a_file_over_half_the_quota_through_the_package_and_keeps_it()
    {
        string engine = BrowserEngines.WEBKIT;
        int size = StorageLimits.MebibytesOf(engine) * 6 / 10;
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        Assert.Equal("no failure", await QuotaPageScripts.WriteBlobAsync(page, FOLDER, "big.bin", size));

        string failure = await QuotaPageScripts.AppendAsync(page, FOLDER, "big.bin", 1);

        Assert.StartsWith("QuotaExceededError:", failure);
        Assert.Equal(["big.bin"], await QuotaPageScripts.NamesAsync(page, FOLDER));
    }

    [EngineFact(BrowserEngines.FIREFOX)]
    public async Task Firefox_stops_counting_a_file_kept_by_keeping_existing_data()
    {
        string engine = BrowserEngines.FIREFOX;
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        await QuotaPageScripts.WriteAsync(page, FOLDER, "big.bin", 7);
        double usageBefore = await QuotaPageScripts.UsageAsync(page);

        string result = await QuotaPageScripts.WriteAsync(page, FOLDER, "big.bin", 1, true);

        Assert.Equal("written", result);
        Assert.True(await QuotaPageScripts.UsageAsync(page) < usageBefore - 5 * MEBIBYTE, "The estimate kept counting the file.");
    }

    [EngineFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_drops_the_quota_to_5_MiB_after_hitting_it_until_the_browser_restarts()
    {
        string engine = BrowserEngines.WEBKIT;
        string profile = fixture.NewLimitedProfile(engine);
        IBrowserContext context = await fixture.OpenProfileAsync(engine, profile);
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);

        await QuotaPageScripts.WriteAsync(page, FOLDER, "a.bin", 1100);
        double quotaAfterHittingIt = await QuotaPageScripts.QuotaAsync(page);
        await page.ReloadAsync();
        double quotaAfterReloading = await QuotaPageScripts.QuotaAsync(page);
        await context.CloseAsync();

        await using IBrowserContext restarted = await fixture.OpenProfileAsync(engine, profile);
        IPage next = await fixture.OpenLimitedPageAsync(restarted, engine, BLANK_PAGE);
        Assert.Equal(5 * MEBIBYTE, quotaAfterHittingIt);
        Assert.Equal(5 * MEBIBYTE, quotaAfterReloading);
        Assert.Equal(1000 * MEBIBYTE, await QuotaPageScripts.QuotaAsync(next));
    }

    [EngineFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_estimates_space_that_blocks_nothing()
    {
        string engine = BrowserEngines.WEBKIT;
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewLimitedProfile(engine));
        IPage page = await fixture.OpenLimitedPageAsync(context, engine, BLANK_PAGE);
        for (int cycle = 0; cycle < 4; cycle++)
        {
            await QuotaPageScripts.WriteAsync(page, FOLDER, "a.bin", 100);
            await QuotaPageScripts.RemoveAsync(page, FOLDER);
        }
        double estimated = await QuotaPageScripts.UsageAsync(page);

        string result = await QuotaPageScripts.WriteAsync(page, FOLDER, "a.bin", 950);

        Assert.True(estimated > 500 * MEBIBYTE, $"The estimate was {estimated}.");
        Assert.Equal("written", result);
    }

    private static async Task<string> WriteWhenFreedAsync(IPage page, int mebibytes)
    {
        string result = await QuotaPageScripts.WriteAsync(page, FOLDER, "b.bin", mebibytes);
        int attempt = 0;
        while (result != "written" && attempt < 20)
        {
            await page.WaitForTimeoutAsync(250);
            result = await QuotaPageScripts.WriteAsync(page, FOLDER, "b.bin", mebibytes);
            attempt++;
        }
        return result;
    }
}
