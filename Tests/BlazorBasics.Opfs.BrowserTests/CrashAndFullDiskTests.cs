using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>A browser killed in the middle of a write, and a disk that fills up, which is not the same as a full quota.</summary>
[Collection(BrowserCollection.NAME)]
public sealed class CrashAndFullDiskTests(BrowserFixture fixture)
{
    private const string BLANK_PAGE = "blank.html";
    private const string FOLDER = "crash";
    private const string PLAYWRIGHT_BROWSERS_FOLDER = "ms-playwright";
    private const string WEBKIT_SWAP_PATTERN = "FileSystemWritableStream-*";
    private const long MEBIBYTE = 1024 * 1024;
    private const int WRITE_MEBIBYTES = 60;
    private const long BYTES_LEFT_ON_THE_DISK = 40 * MEBIBYTE;

    public static TheoryData<string> Engines() => new(BrowserEngines.Names);

    [ContainerTheory]
    [MemberData(nameof(Engines))]
    public async Task A_killed_browser_keeps_the_previous_content_and_its_swap_counts_except_in_WebKit(string engine)
    {
        int limit = StorageLimits.MebibytesOf(engine);
        string profile = fixture.NewLimitedProfile(engine);
        string[] swapsBefore = Directory.GetFiles(Path.GetTempPath(), WEBKIT_SWAP_PATTERN);
        IBrowserContext context = await fixture.OpenProfileAsync(engine, profile);
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

        KillBrowsers();
        await Task.WhenAny(context.CloseAsync(), Task.Delay(3000));
        long orphanedBytes = Directory.GetFiles(Path.GetTempPath(), WEBKIT_SWAP_PATTERN).Except(swapsBefore).Sum(file => new FileInfo(file).Length);
        await using IBrowserContext restarted = await fixture.OpenProfileAsync(engine, profile);
        IPage next = await fixture.OpenLimitedPageAsync(restarted, engine, BLANK_PAGE);

        Assert.Equal("previous", await QuotaPageScripts.ReadTextAsync(next, FOLDER, "a.txt"));
        Assert.Equal(1, (await QuotaPageScripts.ListedNamesAsync(next, FOLDER)).Split("\"name\"").Length - 1);
        Assert.Equal(engine == BrowserEngines.WEBKIT ? "written" : "write QuotaExceededError", await QuotaPageScripts.WriteAsync(next, FOLDER, "b.bin", limit * 8 / 10));
        Assert.Equal(engine == BrowserEngines.WEBKIT, orphanedBytes >= limit * 6 / 10 * MEBIBYTE);
        await QuotaPageScripts.RemoveFileAsync(next, FOLDER, "a.txt");
        Assert.DoesNotContain(await QuotaPageScripts.NamesAsync(next, FOLDER), name => name.StartsWith("a.txt"));
        Assert.Equal("written", await QuotaPageScripts.WriteAsync(next, FOLDER, "b.bin", limit * 8 / 10));
    }

    [ContainerTheory]
    [MemberData(nameof(Engines))]
    public async Task A_write_larger_than_a_full_disk_fails_at_write_and_keeps_the_previous_content(string engine)
    {
        string disk = ContainerTheoryAttribute.DiskFolder!;
        await using IBrowserContext context = await fixture.OpenProfileAsync(engine, fixture.NewProfile(disk), TemporaryFolderOn(disk));
        IPage page = await OpenBlankPageAsync(context);
        await QuotaPageScripts.WriteTextAsync(page, FOLDER, "a.txt", "previous");
        await QuotaPageScripts.UsageAsync(page);
        string filler = FillTheDisk(disk);
        string result;
        string read;
        string[] names;
        string libraryFailure;
        try
        {
            result = await QuotaPageScripts.WriteAsync(page, FOLDER, "a.txt", WRITE_MEBIBYTES);
            read = await QuotaPageScripts.ReadTextAsync(page, FOLDER, "a.txt");
            names = await QuotaPageScripts.NamesAsync(page, FOLDER);
            libraryFailure = await QuotaPageScripts.WriteBlobAsync(page, FOLDER, "a.txt", WRITE_MEBIBYTES);
        }
        finally
        {
            File.Delete(filler);
        }

        Assert.Equal(engine == BrowserEngines.WEBKIT ? "write UnknownError" : "write QuotaExceededError", result);
        Assert.Equal("previous", read);
        Assert.Equal(["a.txt"], names);
        Assert.StartsWith(ExpectedLibraryFailure(engine), libraryFailure);
        Assert.Equal("previous", await QuotaPageScripts.ReadTextAsync(page, FOLDER, "a.txt"));
        await QuotaPageScripts.WriteTextAsync(page, FOLDER, "a.txt", "recovered");
        Assert.Equal("recovered", await QuotaPageScripts.ReadTextAsync(page, FOLDER, "a.txt"));
    }

    [ContainerFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_cuts_the_file_with_no_error_when_the_disk_has_no_room_for_a_second_copy()
    {
        string disk = ContainerTheoryAttribute.DiskFolder!;

        long length = await LengthAfterWritingOnAFullDiskAsync(TemporaryFolderOn(disk), 30);

        Assert.InRange(length, 1, 30 * MEBIBYTE - 1);
    }

    [ContainerFact(BrowserEngines.WEBKIT)]
    public async Task WebKit_cuts_the_file_with_no_error_when_the_temporary_folder_is_on_another_disk()
    {
        long length = await LengthAfterWritingOnAFullDiskAsync(null, WRITE_MEBIBYTES);

        Assert.InRange(length, 1, WRITE_MEBIBYTES * MEBIBYTE - 1);
    }

    [ContainerTheory(BrowserEngines.WEBKIT)]
    [InlineData("blob", true, 30)]
    [InlineData("writer", true, 30)]
    [InlineData("append", true, 30)]
    [InlineData("blob", false, WRITE_MEBIBYTES)]
    [InlineData("writer", false, WRITE_MEBIBYTES)]
    [InlineData("append", false, WRITE_MEBIBYTES)]
    public async Task In_WebKit_the_package_keeps_the_previous_content_where_the_browser_would_cut_the_file(string way, bool temporaryOnTheDisk, int mebibytes)
    {
        string disk = ContainerTheoryAttribute.DiskFolder!;
        string? temporaryFolder = temporaryOnTheDisk ? TemporaryFolderOn(disk) : null;
        await using IBrowserContext context = await fixture.OpenProfileAsync(BrowserEngines.WEBKIT, fixture.NewProfile(disk), temporaryFolder);
        IPage page = await OpenBlankPageAsync(context);
        await QuotaPageScripts.WriteTextAsync(page, FOLDER, "a.txt", "previous");
        string filler = FillTheDisk(disk);
        string failure;
        try
        {
            failure = way switch
            {
                "blob" => await QuotaPageScripts.WriteBlobAsync(page, FOLDER, "a.txt", mebibytes),
                "writer" => await QuotaPageScripts.WriteThroughWriterAsync(page, FOLDER, "a.txt", mebibytes),
                _ => await QuotaPageScripts.AppendAsync(page, FOLDER, "a.txt", mebibytes)
            };
        }
        finally
        {
            File.Delete(filler);
        }

        Assert.StartsWith("QuotaExceededError:", failure);
        Assert.Equal("previous", await QuotaPageScripts.ReadTextAsync(page, FOLDER, "a.txt"));
        Assert.Equal(["a.txt"], await QuotaPageScripts.NamesAsync(page, FOLDER));
        await QuotaPageScripts.WriteTextAsync(page, FOLDER, "a.txt", "recovered");
        Assert.Equal("recovered", await QuotaPageScripts.ReadTextAsync(page, FOLDER, "a.txt"));
    }

    private async Task<long> LengthAfterWritingOnAFullDiskAsync(string? temporaryFolder, int mebibytes)
    {
        string disk = ContainerTheoryAttribute.DiskFolder!;
        await using IBrowserContext context = await fixture.OpenProfileAsync(BrowserEngines.WEBKIT, fixture.NewProfile(disk), temporaryFolder);
        IPage page = await OpenBlankPageAsync(context);
        await QuotaPageScripts.WriteTextAsync(page, FOLDER, "a.txt", "previous");
        string filler = FillTheDisk(disk);
        long length;
        try
        {
            Assert.Equal("written", await QuotaPageScripts.WriteAsync(page, FOLDER, "a.txt", mebibytes));
            length = await page.EvaluateAsync<long>($"async () => {{ const opfs = {QuotaPageScripts.MODULE}; return (await opfs.requireFile(['{FOLDER}', 'a.txt'])).size; }}");
        }
        finally
        {
            File.Delete(filler);
        }
        return length;
    }

    private static string TemporaryFolderOn(string disk) => Directory.CreateDirectory(Path.Combine(disk, "temporary")).FullName;

    private static string ExpectedLibraryFailure(string engine) => engine switch
    {
        BrowserEngines.CHROMIUM => "QuotaExceededError:",
        BrowserEngines.FIREFOX => "UnknownError:",
        _ => "QuotaExceededError:"
    };

    private static string FillTheDisk(string disk)
    {
        string filler = Path.Combine(disk, "filler.bin");
        byte[] chunk = new byte[MEBIBYTE];
        long remaining = new DriveInfo(disk).AvailableFreeSpace - BYTES_LEFT_ON_THE_DISK;
        using FileStream stream = new(filler, FileMode.Create);
        while (remaining > 0)
        {
            stream.Write(chunk);
            remaining -= chunk.Length;
        }
        return filler;
    }

    private static void KillBrowsers()
    {
        foreach (Process process in Process.GetProcesses().Where(IsPlaywrightBrowser))
        {
            process.Kill();
        }
    }

    private static bool IsPlaywrightBrowser(Process process)
    {
        bool isBrowser = false;
        try
        {
            isBrowser = process.MainModule?.FileName.Contains(PLAYWRIGHT_BROWSERS_FOLDER) == true;
        }
        catch (Exception failure) when (failure is Win32Exception or InvalidOperationException)
        {
            isBrowser = false;
        }
        return isBrowser;
    }

    private async Task<IPage> OpenBlankPageAsync(IBrowserContext context)
    {
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.Address.TrimEnd('/')}/{BLANK_PAGE}");
        return page;
    }
}
