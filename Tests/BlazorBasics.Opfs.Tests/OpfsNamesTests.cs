namespace BlazorBasics.Opfs.Tests;

public sealed class OpfsNamesTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a/b", "a%2Fb")]
    [InlineData("a\\b", "a%5Cb")]
    [InlineData("100%", "100%25")]
    [InlineData("%2F", "%252F")]
    [InlineData("D:\\", "D:%5C")]
    public void Names_encode_and_come_back(string name, string stored)
    {
        Assert.Equal(stored, OpfsNames.Encode(name));
        Assert.Equal(name, OpfsNames.Decode(stored));
    }
}
