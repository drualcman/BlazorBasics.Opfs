namespace BlazorBasics.Opfs.TestApp;

/// <summary>The promise of the package: a file of any size costs the same memory, because its bytes stay in JavaScript.</summary>
public sealed class LargeFileTests : OpfsContractTestBase<BrowserContractTarget>
{
    private const int MEBIBYTE = 1024 * 1024;
    private const int LARGE_FILE_MEBIBYTES = 128;
    private const long LARGE_FILE_LENGTH = LARGE_FILE_MEBIBYTES * (long)MEBIBYTE;
    private const double ALLOWED_HEAP_GROWTH = 32.0 * MEBIBYTE;

    [Fact]
    public async Task A_large_file_written_copied_moved_and_fetched_does_not_grow_the_memory()
    {
        await WriteLargeFileAsync();
        double heapBefore = TestHooks.HeapBytes();

        long copied = await FileSystem.CopyAsync(In("large.bin"), In("copy.bin"));
        await FileSystem.MoveFileAsync(In("copy.bin"), In("moved.bin"));
        OpfsFetchResult fetched = await FileSystem.FetchToAsync(Target.FixtureUrl($"bytes/{LARGE_FILE_LENGTH}"), In("fetched.bin"));

        Assert.Equal(LARGE_FILE_LENGTH, copied);
        Assert.Equal(new OpfsFetchResult(LARGE_FILE_LENGTH, false), fetched);
        Assert.Equal(LARGE_FILE_LENGTH, (await FileSystem.GetInfoOrNullAsync(In("moved.bin")))!.Length);
        Assert.True(TestHooks.HeapBytes() - heapBefore < ALLOWED_HEAP_GROWTH, $"The heap grew from {heapBefore} to {TestHooks.HeapBytes()}.");
    }

    [Fact]
    public async Task A_large_file_reads_through_a_stream_by_slices()
    {
        await WriteLargeFileAsync();

        await using Stream stream = FileSystem.OpenLazyRead(In("large.bin"), LARGE_FILE_LENGTH);
        byte[] buffer = new byte[MEBIBYTE];
        long total = 0;
        int read = await stream.ReadAsync(buffer);
        while (read > 0)
        {
            total += read;
            read = await stream.ReadAsync(buffer);
        }

        Assert.Equal(LARGE_FILE_LENGTH, total);
    }

    private async Task WriteLargeFileAsync()
    {
        byte[] chunk = new byte[MEBIBYTE];
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("large.bin"));
        for (int written = 0; written < LARGE_FILE_MEBIBYTES; written++)
        {
            await writer.WriteAsync(chunk);
        }
        await writer.CompleteAsync();
    }
}
