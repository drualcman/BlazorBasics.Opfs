namespace BlazorBasics.Opfs.ContractTests;

public abstract class FolderMoveContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    private static readonly OpfsFolderMoveOptions Options = new()
    {
        NotesFolderName = ".test-moves",
        LockPrefix = "BlazorBasics.Opfs.Tests-moves:"
    };

    private OpfsPath Root => Target.Folder;

    [Fact]
    public async Task A_folder_moves_with_everything_under_it()
    {
        await WriteAsync("source/a.txt", "1");
        await WriteAsync("source/deep/b.txt", "2");

        await FileSystem.MoveFolderAsync(Root, "source", "moved/here", Options);

        Assert.Equal("1", await ReadAsync("moved/here/a.txt"));
        Assert.Equal("2", await ReadAsync("moved/here/deep/b.txt"));
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.ListAsync(In("source")));
        Assert.Empty((await FileSystem.ListAsync(In(Options.NotesFolderName))).Files);
    }

    [Fact]
    public async Task A_move_replaces_files_already_in_the_destination_and_keeps_the_others()
    {
        await WriteAsync("source/a.txt", "new");
        await WriteAsync("destination/a.txt", "old");
        await WriteAsync("destination/kept.txt", "kept");

        await FileSystem.MoveFolderAsync(Root, "source", "destination", Options);

        Assert.Equal("new", await ReadAsync("destination/a.txt"));
        Assert.Equal("kept", await ReadAsync("destination/kept.txt"));
    }

    [Fact]
    public async Task Moving_a_missing_folder_does_nothing()
    {
        await WriteAsync("kept.txt", "kept");

        await FileSystem.MoveFolderAsync(Root, "none", "destination", Options);

        Assert.DoesNotContain("destination", (await FileSystem.ListAsync(Root)).Folders);
    }

    [Fact]
    public async Task A_move_a_closed_tab_left_halfway_is_finished_by_the_next_call()
    {
        await WriteAsync("source/b.txt", "second");
        await WriteAsync("destination/a.txt", "first, already moved");
        await WriteNoteAsync("source", "destination");

        await FileSystem.FinishPendingFolderMovesAsync(Root, Options);

        Assert.Equal("first, already moved", await ReadAsync("destination/a.txt"));
        Assert.Equal("second", await ReadAsync("destination/b.txt"));
        Assert.Empty((await FileSystem.ListAsync(In(Options.NotesFolderName))).Files);
        await Assert.ThrowsAsync<OpfsNotFoundException>(() => FileSystem.ListAsync(In("source")));
    }

    [Fact]
    public async Task A_new_move_finishes_the_pending_one_first()
    {
        await WriteAsync("pending/a.txt", "pending");
        await WriteNoteAsync("pending", "finished");
        await WriteAsync("source/b.txt", "new");

        await FileSystem.MoveFolderAsync(Root, "source", "moved", Options);

        Assert.Equal("pending", await ReadAsync("finished/a.txt"));
        Assert.Equal("new", await ReadAsync("moved/b.txt"));
    }

    [Fact]
    public async Task Finishing_with_no_notes_does_nothing()
    {
        await FileSystem.FinishPendingFolderMovesAsync(In("none"), Options);
    }

    private Task WriteNoteAsync(string source, string destination)
    {
        string SegmentsOf(string relative) => string.Join(",", In(relative).Segments.Select(segment => $"\"{segment}\""));
        return WriteAsync($"{Options.NotesFolderName}/note.json", $"{{\"source\":[{SegmentsOf(source)}],\"destination\":[{SegmentsOf(destination)}]}}");
    }
}
