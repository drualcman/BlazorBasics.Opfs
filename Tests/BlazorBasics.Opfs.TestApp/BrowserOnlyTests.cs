namespace BlazorBasics.Opfs.TestApp;

/// <summary>What only a real browser can show, including where the engines differ.</summary>
public sealed class BrowserOnlyTests : OpfsContractTestBase<BrowserContractTarget>
{
    [Fact]
    public async Task Moving_through_the_copy_route_replaces_the_destination_and_removes_the_source()
    {
        await WriteAsync("a.txt", "new");
        await WriteAsync("other/b.txt", "older and longer");
        TestHooks.DisableMove();
        try
        {
            await FileSystem.MoveFileAsync(In("a.txt"), In("other/b.txt"));
        }
        finally
        {
            TestHooks.EnableMove();
        }

        Assert.False(await ExistsAsync("a.txt"));
        Assert.Equal("new", await ReadAsync("other/b.txt"));
    }

    [Fact]
    public async Task A_folder_moves_through_the_copy_route()
    {
        await WriteAsync("source/deep/a.txt", "1");
        TestHooks.DisableMove();
        try
        {
            await FileSystem.MoveFolderAsync(Target.Folder, "source", "moved", new OpfsFolderMoveOptions { LockPrefix = $"copy-route:{Target.Folder}" });
        }
        finally
        {
            TestHooks.EnableMove();
        }

        Assert.Equal("1", await ReadAsync("moved/deep/a.txt"));
    }

    [Fact]
    public async Task A_move_with_an_open_writer_refuses_or_moves_but_never_loses_a_file()
    {
        await WriteAsync("a.txt", "source");
        await WriteAsync("b.txt", "destination");
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("b.txt"));
        bool refused = false;
        try
        {
            await FileSystem.MoveFileAsync(In("a.txt"), In("b.txt"));
        }
        catch (OpfsFileBusyException)
        {
            refused = true;
        }
        await writer.AbortAsync();

        Assert.Equal(refused ? "destination" : "source", await ReadAsync("b.txt"));
        Assert.Equal(refused, await ExistsAsync("a.txt"));
    }

    [Fact]
    public async Task A_snapshot_read_after_a_write_fails_or_gives_the_new_content()
    {
        await WriteAsync("a.txt", "old");
        using IOpfsFileSnapshot? snapshot = await FileSystem.OpenSnapshotOrNullAsync(In("a.txt"));
        await WriteAsync("a.txt", "new");

        string? read = null;
        try
        {
            read = Text(await snapshot!.ReadRangeAsync(0, 3));
        }
        catch (OpfsException failure)
        {
            Assert.Contains(failure.BrowserErrorName, new[] { "NotReadableError", "AbortError" });
        }

        Assert.True(read is null or "new", $"Read '{read}'.");
    }

    [Fact]
    public async Task A_snapshot_reads_its_file_while_a_write_is_open()
    {
        await WriteAsync("a.txt", "old");
        using IOpfsFileSnapshot? snapshot = await FileSystem.OpenSnapshotOrNullAsync(In("a.txt"));
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt"));
        await writer.WriteAsync(Bytes("new"));

        Assert.Equal("old", Text(await snapshot!.ReadRangeAsync(0, 3)));
    }

    [Fact]
    public async Task An_object_url_serves_the_file()
    {
        await WriteAsync("a.txt", "from a blob");

        string url = await FileSystem.CreateObjectUrlAsync(In("a.txt"));

        Assert.Equal("from a blob", await TestHooks.FetchTextAsync(url));
        FileSystem.RevokeObjectUrl(url);
    }

    [Fact]
    public async Task Two_instances_share_locks()
    {
        string name = $"instances:{Target.Folder}";
        OpfsLocks first = new();
        OpfsLocks second = new();

        IOpfsLock? held = await first.TryAcquireAsync(name, OpfsLockMode.Exclusive);
        Assert.Null(await second.TryAcquireAsync(name, OpfsLockMode.Exclusive));
        held!.Dispose();

        using IOpfsLock? again = await second.TryAcquireAsync(name, OpfsLockMode.Exclusive);
        Assert.NotNull(again);
    }

    [Fact]
    public async Task The_storage_answers()
    {
        OpfsStorageInfo storage = new();

        OpfsStorageEstimate? estimate = await storage.EstimateAsync();
        bool isPersisted = await storage.IsPersistedAsync();

        Assert.NotNull(estimate);
        Assert.True(estimate.Quota > 0);
        Assert.True(isPersisted || !isPersisted);
    }

    [Fact]
    public async Task Cancelling_a_copy_leaves_the_destination_as_it_was()
    {
        await FileSystem.WriteBytesAsync(In("big.bin"), new byte[32 * 1024 * 1024]);
        await WriteAsync("b.txt", "previous");
        using CancellationTokenSource cancellation = new();

        Task copy = FileSystem.CopyAsync(In("big.bin"), In("b.txt"), cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => copy);
        Assert.Equal("previous", await ReadAsync("b.txt"));
    }
}
