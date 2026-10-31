using BlazorBasics.Opfs.Services;

namespace BlazorBasics.Opfs.Tests;

public sealed class OpfsErrorTranslatorTests
{
    [Theory]
    [InlineData("NotFoundError", typeof(OpfsNotFoundException))]
    [InlineData("TypeMismatchError", typeof(OpfsTypeMismatchException))]
    [InlineData("QuotaExceededError", typeof(OpfsQuotaExceededException))]
    [InlineData("NoModificationAllowedError", typeof(OpfsFileBusyException))]
    [InlineData("SecurityError", typeof(OpfsAccessDeniedException))]
    [InlineData("NotAllowedError", typeof(OpfsAccessDeniedException))]
    [InlineData("NotSupportedError", typeof(OpfsNotSupportedException))]
    [InlineData("NotReadableError", typeof(OpfsException))]
    [InlineData("UnknownError", typeof(OpfsException))]
    public void A_browser_error_becomes_its_exception(string name, Type expected)
    {
        Exception original = new($"{name}: what happened");

        OpfsException translated = OpfsErrorTranslator.Translate(original);

        Assert.IsType(expected, translated);
        Assert.Equal(name, translated.BrowserErrorName);
        Assert.Equal("what happened", translated.Message);
        Assert.Same(original, translated.InnerException);
    }

    [Fact]
    public void The_error_prefix_of_the_runtime_is_ignored()
    {
        Assert.IsType<OpfsNotFoundException>(OpfsErrorTranslator.Translate(new Exception("Error: NotFoundError: gone")));
    }

    [Fact]
    public void A_message_that_is_not_ours_stays_whole()
    {
        OpfsException translated = OpfsErrorTranslator.Translate(new Exception("something odd"));

        Assert.IsType<OpfsException>(translated);
        Assert.Equal("something odd", translated.Message);
    }

    [Theory]
    [InlineData("OpfsFetchError: 404|Not Found", 404)]
    [InlineData("OpfsFetchError: 0|Failed to fetch", 0)]
    public void A_fetch_failure_keeps_its_status(string message, int status)
    {
        OpfsFetchException translated = Assert.IsType<OpfsFetchException>(OpfsErrorTranslator.Translate(new Exception(message)));

        Assert.Equal(status, translated.StatusCode);
    }

    [Fact]
    public void Every_exception_is_an_io_exception()
    {
        Assert.IsAssignableFrom<IOException>(OpfsErrorTranslator.Translate(new Exception("NotFoundError: gone")));
    }
}
