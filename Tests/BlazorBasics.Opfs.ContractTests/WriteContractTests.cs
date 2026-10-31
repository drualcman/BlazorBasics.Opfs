namespace BlazorBasics.Opfs.ContractTests;

public abstract class WriteContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    [Fact]
    public async Task Writing_creates_the_folders_and_replaces_the_content()
    {
        await WriteAsync("a/b/c.txt", "first, longer");
        await WriteAsync("a/b/c.txt", "second");

        Assert.Equal("second", await ReadAsync("a/b/c.txt"));
    }

    [Fact]
    public async Task Text_is_utf8()
    {
        await WriteAsync("a.txt", "ñandú €");

        Assert.Equal("ñandú €"u8.ToArray(), await FileSystem.ReadBytesAsync(In("a.txt")));
    }

    [Fact]
    public async Task Bytes_go_in_and_come_back_also_from_a_slice_of_an_array()
    {
        byte[] content = [9, 1, 2, 3, 9];

        await FileSystem.WriteBytesAsync(In("a.bin"), content.AsMemory(1, 3));

        Assert.Equal([1, 2, 3], await FileSystem.ReadBytesAsync(In("a.bin")));
    }

    [Fact]
    public async Task Empty_content_makes_an_empty_file()
    {
        await FileSystem.WriteBytesAsync(In("a.bin"), ReadOnlyMemory<byte>.Empty);

        Assert.Empty(await FileSystem.ReadBytesAsync(In("a.bin")));
    }

    [Fact]
    public async Task A_stream_writes_the_whole_file()
    {
        byte[] content = new byte[3 * 1024 * 1024 + 17];
        Random.Shared.NextBytes(content);

        await FileSystem.WriteStreamAsync(In("a.bin"), new MemoryStream(content));

        Assert.Equal(content, await FileSystem.ReadBytesAsync(In("a.bin")));
    }

    [Fact]
    public async Task A_failing_stream_leaves_the_previous_content()
    {
        await WriteAsync("a.txt", "previous");

        await Assert.ThrowsAsync<IOException>(() => FileSystem.WriteStreamAsync(In("a.txt"), new FailingStream()));

        Assert.Equal("previous", await ReadAsync("a.txt"));
    }

    [Fact]
    public async Task A_failing_stream_on_a_new_file_leaves_nothing()
    {
        await Assert.ThrowsAsync<IOException>(() => FileSystem.WriteStreamAsync(In("a.txt"), new FailingStream()));

        Assert.False(await ExistsAsync("a.txt"));
    }

    [Fact]
    public async Task Appending_adds_to_the_end_and_is_seen_at_once()
    {
        await FileSystem.AppendAsync(In("log"), Bytes("one"));
        await FileSystem.AppendAsync(In("log"), Bytes("-two"));

        Assert.Equal("one-two", await ReadAsync("log"));
    }

    [Fact]
    public async Task Truncating_cuts_and_never_extends()
    {
        await WriteAsync("a.txt", "0123456789");

        await FileSystem.TruncateAsync(In("a.txt"), 4);
        await FileSystem.TruncateAsync(In("a.txt"), 100);

        Assert.Equal("0123", await ReadAsync("a.txt"));
    }

    [Fact]
    public async Task Truncating_a_missing_file_throws_not_found()
    {
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.TruncateAsync(In("none.txt"), 1));
    }

    [Fact]
    public async Task Writing_through_a_file_as_a_folder_throws_type_mismatch()
    {
        await WriteAsync("a.txt", "x");

        await Assert.ThrowsAsync<OpfsTypeMismatchException>(() => WriteAsync("a.txt/b.txt", "y"));
    }

    [Fact]
    public async Task Writing_over_a_folder_throws_type_mismatch()
    {
        await WriteAsync("folder/a.txt", "x");

        await Assert.ThrowsAsync<OpfsTypeMismatchException>(() => WriteAsync("folder", "y"));
    }

    [Fact]
    public async Task A_cancelled_token_stops_a_write_before_it_starts()
    {
        await WriteAsync("a.txt", "previous");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileSystem.WriteTextAsync(In("a.txt"), "new", new CancellationToken(true)));

        Assert.Equal("previous", await ReadAsync("a.txt"));
    }
}
