namespace BlazorBasics.Opfs.ContractTests;

public abstract class NameContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    [Theory]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("100%")]
    [InlineData("literal%2F")]
    [InlineData("D:\\")]
    [InlineData("/")]
    public async Task A_name_survives_the_round_trip(string name)
    {
        OpfsPath file = OpfsPath.FromSegments([.. Target.Folder.Segments, "names", name]);

        await FileSystem.WriteTextAsync(file, "content");
        OpfsFolderContent content = await FileSystem.ListAsync(In("names"));

        Assert.Equal("content", await FileSystem.ReadTextAsync(file));
        Assert.Equal(name, Assert.Single(content.Files).DecodedName);
    }

    [Fact]
    public async Task All_names_at_once_stay_apart()
    {
        string[] names = ["a/b", "a\\b", "a%2Fb", "a%5Cb", "a%b"];
        foreach (string name in names)
        {
            await FileSystem.WriteTextAsync(OpfsPath.FromSegments([.. Target.Folder.Segments, "names", name]), name);
        }

        OpfsFolderContent content = await FileSystem.ListAsync(In("names"));

        Assert.Equal(names.Order(StringComparer.Ordinal), content.Files.Select(file => file.DecodedName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task A_stored_name_from_a_listing_goes_back_into_a_path_as_it_is()
    {
        await FileSystem.WriteTextAsync(OpfsPath.FromSegments([.. Target.Folder.Segments, "names", "a/b"]), "content");

        OpfsFileInfo file = Assert.Single((await FileSystem.ListAsync(In("names"))).Files);

        Assert.Equal("content", await FileSystem.ReadTextAsync(In("names").Combine(file.Name)));
    }
}
