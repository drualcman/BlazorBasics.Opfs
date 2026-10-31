namespace BlazorBasics.Opfs.ContractTests;

public abstract class ReadContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    [Fact]
    public async Task Probe_says_it_is_available()
    {
        OpfsProbeResult result = await FileSystem.ProbeAsync();

        Assert.True(result.IsAvailable, result.Reason);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task Info_describes_a_file()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow.AddMinutes(-1);
        await WriteAsync("a.txt", "hello");

        OpfsFileInfo? info = await FileSystem.GetInfoOrNullAsync(In("a.txt"));

        Assert.NotNull(info);
        Assert.Equal("a.txt", info.Name);
        Assert.Equal(5, info.Length);
        Assert.True(info.ModifiedUtc > before);
        Assert.Null(info.Text);
    }

    [Fact]
    public async Task Info_is_null_for_a_missing_file_and_a_missing_folder()
    {
        Assert.Null(await FileSystem.GetInfoOrNullAsync(In("none.txt")));
        Assert.Null(await FileSystem.GetInfoOrNullAsync(In("none/none.txt")));
    }

    [Fact]
    public async Task Info_on_a_folder_throws_type_mismatch()
    {
        await WriteAsync("folder/a.txt", "x");

        await Assert.ThrowsAsync<OpfsTypeMismatchException>(() => FileSystem.GetInfoOrNullAsync(In("folder")));
    }

    [Fact]
    public async Task A_snapshot_is_null_for_a_missing_file()
    {
        Assert.Null(await FileSystem.OpenSnapshotOrNullAsync(In("none.txt")));
    }

    [Fact]
    public async Task A_snapshot_reads_ranges_and_cuts_past_the_end()
    {
        await WriteAsync("a.txt", "0123456789");

        using IOpfsFileSnapshot? snapshot = await FileSystem.OpenSnapshotOrNullAsync(In("a.txt"));

        Assert.NotNull(snapshot);
        Assert.Equal(10, snapshot.Length);
        Assert.Equal("234", Text(await snapshot.ReadRangeAsync(2, 3)));
        Assert.Equal("89", Text(await snapshot.ReadRangeAsync(8, 10)));
    }

    [Fact]
    public async Task A_snapshot_stream_reads_the_whole_file()
    {
        await WriteAsync("a.txt", "0123456789");
        IOpfsFileSnapshot? snapshot = await FileSystem.OpenSnapshotOrNullAsync(In("a.txt"));

        await using Stream stream = snapshot!.OpenRead();
        using MemoryStream copy = new();
        await stream.CopyToAsync(copy);

        Assert.Equal("0123456789", Text(copy.ToArray()));
    }

    [Fact]
    public async Task List_gives_files_and_folders_in_ordinal_order_with_the_text_of_the_suffix()
    {
        await WriteAsync("b.meta", "metadata");
        await WriteAsync("a.bin", "x");
        await WriteAsync("Z/inner.txt", "y");
        await WriteAsync("c/inner.txt", "y");

        OpfsFolderContent content = await FileSystem.ListAsync(Target.Folder, ".meta");

        Assert.Equal(["a.bin", "b.meta"], content.Files.Select(file => file.Name));
        Assert.Null(content.Files[0].Text);
        Assert.Equal("metadata", content.Files[1].Text);
        Assert.Equal(["Z", "c"], content.Folders);
    }

    [Fact]
    public async Task List_of_a_missing_folder_throws_not_found()
    {
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.ListAsync(In("none")));
    }

    [Fact]
    public async Task List_of_a_file_throws_type_mismatch()
    {
        await WriteAsync("a.txt", "x");

        await Assert.ThrowsAsync<OpfsTypeMismatchException>(() => FileSystem.ListAsync(In("a.txt")));
    }

    [Fact]
    public async Task List_tree_gives_relative_paths_at_any_depth()
    {
        await WriteAsync("one.txt", "1");
        await WriteAsync("a/two.meta", "2");
        await WriteAsync("a/b/three.txt", "3");

        IReadOnlyList<OpfsFileInfo> files = await FileSystem.ListTreeAsync(Target.Folder, ".meta");

        Assert.Equal(["a/b/three.txt", "a/two.meta", "one.txt"], files.Select(file => file.Name));
        Assert.Equal("2", files[1].Text);
    }

    [Fact]
    public async Task Reading_a_missing_file_throws_not_found()
    {
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.ReadTextAsync(In("none.txt")));
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.ReadBytesAsync(In("none/none.txt")));
    }

    [Fact]
    public async Task Reading_through_a_file_as_a_folder_throws_type_mismatch()
    {
        await WriteAsync("a.txt", "x");

        await Assert.ThrowsAsync<OpfsTypeMismatchException>(() => FileSystem.ReadTextAsync(In("a.txt/inner.txt")));
    }

    [Fact]
    public async Task Reading_a_folder_as_a_file_throws_type_mismatch()
    {
        await WriteAsync("folder/a.txt", "x");

        await Assert.ThrowsAsync<OpfsTypeMismatchException>(() => FileSystem.ReadTextAsync(In("folder")));
    }

    [Fact]
    public async Task Reading_the_root_as_a_file_is_refused()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => FileSystem.ReadTextAsync(OpfsPath.Root));
    }

    [Fact]
    public async Task A_range_past_the_end_comes_back_shorter()
    {
        await WriteAsync("a.txt", "0123456789");

        Assert.Equal("789", Text(await FileSystem.ReadRangeAsync(In("a.txt"), 7, 100)));
        Assert.Empty(await FileSystem.ReadRangeAsync(In("a.txt"), 50, 10));
    }

    [Fact]
    public async Task A_lazy_stream_reads_by_slices_seeks_and_reports_its_path()
    {
        await WriteAsync("a.txt", "0123456789");

        await using Stream stream = FileSystem.OpenLazyRead(In("a.txt"), 10);
        stream.Seek(4, SeekOrigin.Begin);
        byte[] buffer = new byte[3];
        int read = await stream.ReadAsync(buffer);

        Assert.Equal(3, read);
        Assert.Equal("456", Text(buffer));
        Assert.Equal(In("a.txt"), ((BlazorBasics.Opfs.Streams.OpfsLazyReadStream)stream).Path);
    }

    [Fact]
    public async Task A_lazy_stream_refuses_a_synchronous_read()
    {
        await WriteAsync("a.txt", "x");

        await using Stream stream = FileSystem.OpenLazyRead(In("a.txt"), 1);

        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
    }

    [Fact]
    public async Task A_lazy_stream_over_a_removed_file_throws_not_found()
    {
        await using Stream stream = FileSystem.OpenLazyRead(In("none.txt"), 10);

        await Assert.ThrowsAsync<OpfsNotFoundException>(async () => await stream.ReadExactlyAsync(new byte[4]));
    }
}
