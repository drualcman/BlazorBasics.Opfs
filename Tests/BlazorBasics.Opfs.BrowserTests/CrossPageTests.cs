using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>What takes more than one page, driven through the public JavaScript of the package.</summary>
[Collection(BrowserCollection.NAME)]
public sealed class CrossPageTests(BrowserFixture fixture)
{
    private const string MODULE = "await import('/_content/BlazorBasics.Opfs/opfs.js')";

    public static TheoryData<string> Engines() => new(BrowserEngines.Names);

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task Two_pages_see_each_others_files(string engine)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage first = await OpenBlankPageAsync(context);
        IPage second = await OpenBlankPageAsync(context);

        await first.EvaluateAsync($"async () => {{ const opfs = {MODULE}; await opfs.writeText(['pages', 'a.txt'], 'from the first'); }}");
        string read = await second.EvaluateAsync<string>($"async () => {{ const opfs = {MODULE}; return await opfs.readText(['pages', 'a.txt']); }}");

        Assert.Equal("from the first", read);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task Closing_a_page_mid_write_leaves_the_previous_content_and_no_leftovers(string engine)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage writing = await OpenBlankPageAsync(context);
        await writing.EvaluateAsync($$"""
            async () => {
                const opfs = {{MODULE}};
                await opfs.writeText(['crash', 'a.txt'], 'previous');
                globalThis.open = await opfs.openWriter(['crash', 'a.txt'], false);
                await globalThis.open.writable.write(new Uint8Array(8 * 1024 * 1024));
            }
            """);

        await writing.CloseAsync();
        IPage next = await OpenBlankPageAsync(context);
        string listing = await next.EvaluateAsync<string>($"async () => {{ const opfs = {MODULE}; return await opfs.listFolder(['crash'], null); }}");
        string read = await next.EvaluateAsync<string>($"async () => {{ const opfs = {MODULE}; return await opfs.readText(['crash', 'a.txt']); }}");

        Assert.Equal("previous", read);
        Assert.Contains("\"name\":\"a.txt\"", listing);
        Assert.Equal(1, listing.Split("\"name\"").Length - 1);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task A_lock_clashes_between_pages_and_a_closed_page_frees_it(string engine)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage holding = await OpenBlankPageAsync(context);
        IPage other = await OpenBlankPageAsync(context);
        string tryAcquire = $"async () => {{ const opfs = {MODULE}; return (await opfs.tryAcquireLock('pages', 'exclusive')) !== null; }}";

        Assert.True(await holding.EvaluateAsync<bool>(tryAcquire));
        Assert.False(await other.EvaluateAsync<bool>(tryAcquire));

        await holding.CloseAsync();
        await other.WaitForFunctionAsync(tryAcquire);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task A_lock_released_through_one_import_url_is_free_at_once_through_another(string engine)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await OpenBlankPageAsync(context);

        bool taken = await page.EvaluateAsync<bool>("""
            async () => {
                const first = await import('/_content/BlazorBasics.Opfs/opfs.js');
                const second = await import('/_content/BlazorBasics.Opfs/opfs.js?v=2');
                first.releaseLock(await first.tryAcquireLock('urls', 'exclusive'));
                return (await second.tryAcquireLock('urls', 'exclusive')) !== null;
            }
            """);

        Assert.True(taken);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task A_file_downloads_with_its_suggested_name(string engine)
    {
        await using IBrowserContext context = await fixture.NewContextAsync(engine);
        IPage page = await OpenBlankPageAsync(context);
        await page.EvaluateAsync($"async () => {{ const opfs = {MODULE}; await opfs.writeText(['a.txt'], 'downloaded'); }}");

        IDownload download = await page.RunAndWaitForDownloadAsync(() =>
            page.EvaluateAsync($"async () => {{ const opfs = {MODULE}; await opfs.downloadFile(['a.txt'], 'suggested.txt'); }}"));

        Assert.Equal("suggested.txt", download.SuggestedFilename);
        Assert.Equal("downloaded", await File.ReadAllTextAsync((await download.PathAsync())!));
    }

    private async Task<IPage> OpenBlankPageAsync(IBrowserContext context)
    {
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.Address.TrimEnd('/')}/blank.html");
        return page;
    }
}
