namespace BlazorBasics.Opfs.ContractTests;

public abstract class WriterContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    [Fact]
    public async Task Chunks_become_visible_all_at_once_when_it_completes()
    {
        await WriteAsync("a.txt", "previous");

        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt"));
        await writer.WriteAsync(Bytes("one"));
        await writer.WriteAsync(Bytes("-two"));
        Assert.Equal("previous", await ReadAsync("a.txt"));
        await writer.CompleteAsync();

        Assert.Equal("one-two", await ReadAsync("a.txt"));
    }

    [Fact]
    public async Task Keeping_existing_data_continues_at_the_end()
    {
        await WriteAsync("a.txt", "one");

        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt"), keepExistingData: true);
        await writer.WriteAsync(Bytes("-two"));
        await writer.CompleteAsync();

        Assert.Equal("one-two", await ReadAsync("a.txt"));
    }

    [Fact]
    public async Task Aborting_leaves_the_file_as_it_was()
    {
        await WriteAsync("a.txt", "previous");

        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt"));
        await writer.WriteAsync(Bytes("new"));
        await writer.AbortAsync();

        Assert.Equal("previous", await ReadAsync("a.txt"));
    }

    [Fact]
    public async Task Aborting_a_new_file_leaves_nothing()
    {
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt"));
        await writer.WriteAsync(Bytes("new"));
        await writer.AbortAsync();

        Assert.False(await ExistsAsync("a.txt"));
    }

    [Fact]
    public async Task Disposing_without_completing_aborts()
    {
        await WriteAsync("a.txt", "previous");

        await using (IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt")))
        {
            await writer.WriteAsync(Bytes("new"));
        }

        Assert.Equal("previous", await ReadAsync("a.txt"));
    }

    [Fact]
    public async Task Disposing_a_writer_on_a_new_file_leaves_nothing()
    {
        await using (IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt")))
        {
            await writer.WriteAsync(Bytes("new"));
        }

        Assert.False(await ExistsAsync("a.txt"));
    }

    [Fact]
    public async Task A_disposed_writer_frees_the_file()
    {
        await using (IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt")))
        {
            await writer.WriteAsync(Bytes("new"));
        }

        await WriteAsync("a.txt", "again");
        await FileSystem.DeleteAsync(In("a.txt"));

        Assert.False(await ExistsAsync("a.txt"));
    }

    [Fact]
    public async Task Using_a_finished_writer_is_refused()
    {
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt"));
        await writer.CompleteAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync(Bytes("x")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.CompleteAsync());
    }

    [Fact]
    public async Task Removing_a_file_with_an_open_writer_throws_busy()
    {
        await WriteAsync("a.txt", "previous");
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(In("a.txt"));

        await Assert.ThrowsAsync<OpfsFileBusyException>(() => FileSystem.DeleteAsync(In("a.txt")));

        await writer.AbortAsync();
        Assert.Equal("previous", await ReadAsync("a.txt"));
    }
}
