namespace BlazorBasics.Opfs.ContractTests;

public abstract class CopyMoveContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    [Fact]
    public async Task Copying_returns_the_length_and_keeps_the_source()
    {
        await WriteAsync("a.txt", "hello");

        long copied = await FileSystem.CopyAsync(In("a.txt"), In("deep/er/b.txt"));

        Assert.Equal(5, copied);
        Assert.Equal("hello", await ReadAsync("a.txt"));
        Assert.Equal("hello", await ReadAsync("deep/er/b.txt"));
    }

    [Fact]
    public async Task Copying_replaces_the_destination()
    {
        await WriteAsync("a.txt", "new");
        await WriteAsync("b.txt", "older and longer");

        await FileSystem.CopyAsync(In("a.txt"), In("b.txt"));

        Assert.Equal("new", await ReadAsync("b.txt"));
    }

    [Fact]
    public async Task Copying_a_missing_file_throws_not_found_and_creates_nothing()
    {
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.CopyAsync(In("none.txt"), In("b.txt")));

        Assert.False(await ExistsAsync("b.txt"));
    }

    [Fact]
    public async Task Moving_renames_and_creates_the_folders()
    {
        await WriteAsync("a.txt", "hello");

        await FileSystem.MoveFileAsync(In("a.txt"), In("deep/er/b.txt"));

        Assert.False(await ExistsAsync("a.txt"));
        Assert.Equal("hello", await ReadAsync("deep/er/b.txt"));
    }

    [Fact]
    public async Task Moving_replaces_a_destination_in_the_same_folder()
    {
        await WriteAsync("a.txt", "new");
        await WriteAsync("b.txt", "older and longer");

        await FileSystem.MoveFileAsync(In("a.txt"), In("b.txt"));

        Assert.False(await ExistsAsync("a.txt"));
        Assert.Equal("new", await ReadAsync("b.txt"));
    }

    [Fact]
    public async Task Moving_replaces_a_destination_in_another_folder()
    {
        await WriteAsync("a.txt", "new");
        await WriteAsync("other/b.txt", "older and longer");

        await FileSystem.MoveFileAsync(In("a.txt"), In("other/b.txt"));

        Assert.False(await ExistsAsync("a.txt"));
        Assert.Equal("new", await ReadAsync("other/b.txt"));
    }

    [Fact]
    public async Task Moving_a_file_onto_itself_changes_nothing()
    {
        await WriteAsync("a.txt", "hello");

        await FileSystem.MoveFileAsync(In("a.txt"), In("a.txt"));

        Assert.Equal("hello", await ReadAsync("a.txt"));
    }

    [Fact]
    public async Task Moving_a_missing_file_throws_not_found_and_keeps_the_destination()
    {
        await WriteAsync("b.txt", "kept");

        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.MoveFileAsync(In("none.txt"), In("b.txt")));

        Assert.Equal("kept", await ReadAsync("b.txt"));
    }

    [Fact]
    public async Task Object_urls_need_the_file()
    {
        await WriteAsync("a.txt", "x");

        string url = await FileSystem.CreateObjectUrlAsync(In("a.txt"));

        Assert.StartsWith("blob:", url);
        FileSystem.RevokeObjectUrl(url);
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.CreateObjectUrlAsync(In("none.txt")));
    }

    [Fact]
    public async Task Downloading_a_missing_file_throws_not_found()
    {
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.DownloadAsync(In("none.txt"), "name.txt"));
    }
}
