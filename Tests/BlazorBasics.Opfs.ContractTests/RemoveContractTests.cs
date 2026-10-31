namespace BlazorBasics.Opfs.ContractTests;

public abstract class RemoveContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    [Fact]
    public async Task Removing_a_file_or_a_folder_takes_everything_under_it()
    {
        await WriteAsync("a.txt", "x");
        await WriteAsync("folder/b/c.txt", "y");

        await FileSystem.DeleteAsync(In("a.txt"));
        await FileSystem.DeleteAsync(In("folder"));

        OpfsFolderContent content = await FileSystem.ListAsync(Target.Folder);
        Assert.Empty(content.Files);
        Assert.Empty(content.Folders);
    }

    [Fact]
    public async Task Removing_what_is_not_there_is_success()
    {
        await WriteAsync("a.txt", "x");

        await FileSystem.DeleteAsync(In("none.txt"));
        await FileSystem.DeleteAsync(In("none/none/none.txt"));
    }

    [Fact]
    public async Task Removing_through_a_file_as_a_folder_throws_type_mismatch()
    {
        await WriteAsync("a.txt", "x");

        await Assert.ThrowsAsync<OpfsTypeMismatchException>(() => FileSystem.DeleteAsync(In("a.txt/inner.txt")));
    }

    [Fact]
    public async Task Clearing_keeps_the_folder_and_removes_its_content()
    {
        await WriteAsync("folder/a.txt", "x");
        await WriteAsync("folder/b/c.txt", "y");

        await FileSystem.ClearFolderAsync(In("folder"));

        OpfsFolderContent content = await FileSystem.ListAsync(In("folder"));
        Assert.Empty(content.Files);
        Assert.Empty(content.Folders);
    }

    [Fact]
    public async Task Clearing_a_missing_folder_is_success()
    {
        await FileSystem.ClearFolderAsync(In("none"));
    }
}
