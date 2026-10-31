namespace BlazorBasics.Opfs.TestApp;

/// <summary>
/// Each way of writing, run alone in a browser whose quota the test host has limited, because hitting the quota can change
/// the quota itself. The host names the test and the limit in the address.
/// </summary>
public sealed class QuotaExhaustionTests : OpfsContractTestBase<BrowserContractTarget>
{
    public const string TEST_QUERY = "quota";
    public const string LIMIT_QUERY = "limit";

    private const int MEBIBYTE = 1024 * 1024;
    private const string QUOTA_ERROR_NAME = "QuotaExceededError";

    private static int LimitMebibytes => int.Parse(TestHooks.QueryValue(LIMIT_QUERY)!);

    private static int FillerMebibytes => LimitMebibytes * 85 / 100;

    private static int PayloadMebibytes => LimitMebibytes * 30 / 100;

    [Fact]
    public async Task A_writer_over_an_existing_file()
    {
        await WriteAsync("a.txt", "previous");

        OpfsQuotaExceededException failure = await Assert.ThrowsAsync<OpfsQuotaExceededException>(() => WriteChunksAsync("a.txt", LimitMebibytes * 3 / 2));

        AssertIsTheBrowserFailure(failure);
        Assert.Equal("previous", await ReadAsync("a.txt"));
        Assert.Equal(["a.txt"], await VisibleNamesAsync());
    }

    [Fact]
    public async Task A_writer_for_a_new_file()
    {
        OpfsQuotaExceededException failure = await Assert.ThrowsAsync<OpfsQuotaExceededException>(() => WriteChunksAsync("new.bin", LimitMebibytes * 3 / 2));

        AssertIsTheBrowserFailure(failure);
        Assert.Empty(await VisibleNamesAsync());
    }

    [Fact]
    public async Task Writing_bytes()
    {
        await WriteAsync("a.txt", "previous");
        await WriteChunksAsync("filler.bin", FillerMebibytes);

        OpfsQuotaExceededException failure = await Assert.ThrowsAsync<OpfsQuotaExceededException>(() =>
            FileSystem.WriteBytesAsync(In("a.txt"), new byte[PayloadMebibytes * MEBIBYTE]));

        AssertIsTheBrowserFailure(failure);
        Assert.Equal("previous", await ReadAsync("a.txt"));
        Assert.Equal(["a.txt", "filler.bin"], await VisibleNamesAsync());
    }

    [Fact]
    public async Task A_copy()
    {
        await WriteAsync("a.txt", "previous");
        await WriteChunksAsync("filler.bin", FillerMebibytes);

        OpfsQuotaExceededException failure = await Assert.ThrowsAsync<OpfsQuotaExceededException>(() =>
            FileSystem.CopyAsync(In("filler.bin"), In("a.txt")));

        AssertIsTheBrowserFailure(failure);
        Assert.Equal("previous", await ReadAsync("a.txt"));
        Assert.Equal(["a.txt", "filler.bin"], await VisibleNamesAsync());
    }

    [Fact]
    public async Task A_download()
    {
        await WriteAsync("a.txt", "previous");
        await WriteChunksAsync("filler.bin", FillerMebibytes);
        Uri url = Target.FixtureUrl($"bytes/{(long)PayloadMebibytes * MEBIBYTE}");

        OpfsQuotaExceededException failure = await Assert.ThrowsAsync<OpfsQuotaExceededException>(() =>
            FileSystem.FetchToAsync(url, In("a.txt")));

        AssertIsTheBrowserFailure(failure);
        Assert.Equal("previous", await ReadAsync("a.txt"));
        Assert.Equal(["a.txt", "filler.bin"], await VisibleNamesAsync());
    }

    private static void AssertIsTheBrowserFailure(OpfsQuotaExceededException failure)
    {
        Assert.Equal(QUOTA_ERROR_NAME, failure.BrowserErrorName);
        Assert.NotNull(failure.InnerException);
    }

    private async Task WriteChunksAsync(string relativePath, int mebibytes)
    {
        byte[] chunk = new byte[MEBIBYTE];
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In(relativePath));
        for (int written = 0; written < mebibytes; written++)
        {
            await writer.WriteAsync(chunk);
        }
        await writer.CompleteAsync();
    }

    private async Task<List<string>> VisibleNamesAsync()
    {
        OpfsFolderContent content = await FileSystem.ListAsync(Target.Folder);
        return content.Files.Select(file => file.Name).Concat(content.Folders).Order(StringComparer.Ordinal).ToList();
    }
}
