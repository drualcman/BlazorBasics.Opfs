namespace BlazorBasics.Opfs.Tests;

public sealed class OpfsPathTests
{
    [Fact]
    public void Parse_splits_and_drops_empty_segments()
    {
        OpfsPath path = OpfsPath.Parse("/a//b/c/");

        Assert.Equal(["a", "b", "c"], path.Segments);
        Assert.Equal("a/b/c", path.ToString());
    }

    [Fact]
    public void Parse_does_not_encode()
    {
        Assert.Equal(["100%25"], OpfsPath.Parse("100%25").Segments);
    }

    [Theory]
    [InlineData("a/../b")]
    [InlineData("./a")]
    [InlineData("a\\b")]
    public void Parse_refuses_what_is_not_a_name(string path)
    {
        Assert.Throws<ArgumentException>(() => OpfsPath.Parse(path));
    }

    [Fact]
    public void From_segments_keeps_each_name_as_one_segment()
    {
        OpfsPath path = OpfsPath.FromSegments(["a/b", "c\\d", "50%"]);

        Assert.Equal(["a%2Fb", "c%5Cd", "50%25"], path.Segments);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    public void From_segments_refuses_what_cannot_be_a_name(string name)
    {
        Assert.Throws<ArgumentException>(() => OpfsPath.FromSegments([name]));
    }

    [Fact]
    public void Parent_and_name_split_the_last_segment()
    {
        OpfsPath path = "a/b/c.txt";

        Assert.Equal("a/b", path.Parent.ToString());
        Assert.Equal("c.txt", path.Name);
        Assert.Equal("a", ((OpfsPath)"a/b").Parent.ToString());
    }

    [Fact]
    public void The_root_is_its_own_parent_and_has_no_name()
    {
        Assert.Equal(OpfsPath.Root, OpfsPath.Root.Parent);
        Assert.Equal(OpfsPath.Root, ((OpfsPath)"a").Parent);
        Assert.Equal(string.Empty, OpfsPath.Root.Name);
        Assert.Empty(OpfsPath.Root.Segments);
        Assert.True(OpfsPath.Root.IsRoot);
    }

    [Fact]
    public void The_default_value_is_the_root()
    {
        OpfsPath path = default;

        Assert.Equal(OpfsPath.Root, path);
        Assert.Equal(OpfsPath.Root.GetHashCode(), path.GetHashCode());
        Assert.Empty(path.Segments);
    }

    [Fact]
    public void Combine_adds_one_stored_segment()
    {
        Assert.Equal("a/b", OpfsPath.Parse("a").Combine("b").ToString());
        Assert.Equal("b", OpfsPath.Root.Combine("b").ToString());
        Assert.Equal("a/x%2Fy", OpfsPath.Parse("a").Combine("x%2Fy").ToString());
    }

    [Fact]
    public void Combine_refuses_more_than_one_segment()
    {
        Assert.Throws<ArgumentException>(() => OpfsPath.Parse("a").Combine("b/c"));
    }

    [Fact]
    public void Equal_paths_are_equal_however_they_were_built()
    {
        Assert.Equal(OpfsPath.Parse("/a/b"), OpfsPath.FromSegments(["a", "b"]));
        Assert.NotEqual(OpfsPath.Parse("a/b"), OpfsPath.Parse("a/B"));
    }
}
