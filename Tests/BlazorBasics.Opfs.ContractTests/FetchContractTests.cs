namespace BlazorBasics.Opfs.ContractTests;

/// <summary>
/// The target serves these routes: <c>bytes/{n}</c> declares its length, <c>chunked/{n}</c> does not, <c>status/{code}</c>
/// answers that status, <c>cut/{n}</c> breaks after n bytes, and <c>stall/{n}</c> never ends after n bytes.
/// </summary>
public abstract class FetchContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    [Theory]
    [InlineData("bytes/2048")]
    [InlineData("chunked/2048")]
    public async Task A_download_lands_in_the_file(string route)
    {
        OpfsFetchResult result = await FileSystem.FetchToAsync(Target.FixtureUrl(route), In("deep/a.bin"));

        Assert.Equal(new OpfsFetchResult(2048, false), result);
        Assert.Equal(2048, (await FileSystem.GetInfoOrNullAsync(In("deep/a.bin")))!.Length);
    }

    [Fact]
    public async Task An_empty_download_makes_an_empty_file()
    {
        OpfsFetchResult result = await FileSystem.FetchToAsync(Target.FixtureUrl("bytes/0"), In("a.bin"));

        Assert.Equal(new OpfsFetchResult(0, false), result);
        Assert.True(await ExistsAsync("a.bin"));
    }

    [Theory]
    [InlineData("bytes/4096")]
    [InlineData("chunked/4096")]
    public async Task Over_the_limit_is_a_result_and_leaves_the_file_as_it_was(string route)
    {
        await WriteAsync("a.bin", "previous");
        await FileSystem.FetchToAsync(Target.FixtureUrl(route), In("new.bin"), new OpfsFetchOptions { MaxBytes = 1024 });

        OpfsFetchResult result = await FileSystem.FetchToAsync(Target.FixtureUrl(route), In("a.bin"), new OpfsFetchOptions { MaxBytes = 1024 });

        Assert.Equal(new OpfsFetchResult(0, true), result);
        Assert.Equal("previous", await ReadAsync("a.bin"));
        Assert.False(await ExistsAsync("new.bin"));
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    public async Task An_http_error_throws_with_the_status_and_creates_nothing(int status)
    {
        OpfsFetchException failure = await Assert.ThrowsAsync<OpfsFetchException>(() => FileSystem.FetchToAsync(Target.FixtureUrl($"status/{status}"), In("a.bin")));

        Assert.Equal(status, failure.StatusCode);
        Assert.False(await ExistsAsync("a.bin"));
    }

    [Fact]
    public async Task A_cut_download_throws_and_leaves_the_file_as_it_was()
    {
        await WriteAsync("a.bin", "previous");

        await Assert.ThrowsAsync<OpfsFetchException>(() => FileSystem.FetchToAsync(Target.FixtureUrl("cut/2048"), In("a.bin")));

        Assert.Equal("previous", await ReadAsync("a.bin"));
    }

    [Fact]
    public async Task A_cut_download_of_a_new_file_leaves_nothing()
    {
        await Assert.ThrowsAsync<OpfsFetchException>(() => FileSystem.FetchToAsync(Target.FixtureUrl("cut/2048"), In("a.bin")));

        Assert.False(await ExistsAsync("a.bin"));
    }

    [Fact]
    public async Task Cancelling_stops_the_download_and_leaves_the_file_as_it_was()
    {
        await WriteAsync("a.bin", "previous");
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileSystem.FetchToAsync(Target.FixtureUrl("stall/1024"), In("a.bin"), null, cancellation.Token));

        Assert.Equal("previous", await ReadAsync("a.bin"));
    }
}
