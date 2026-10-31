namespace BlazorBasics.Opfs.TestApp;

/// <summary>What only a worker can show. The rest of the battery runs there too, through <c>worker.html</c>.</summary>
public sealed class WorkerOnlyTests : OpfsContractTestBase<BrowserContractTarget>
{
    [Fact]
    public void The_battery_really_runs_in_a_worker()
    {
        Assert.True(TestHooks.InWorker());
    }

    [Fact]
    public async Task Downloading_fails_naming_the_worker_and_the_fix()
    {
        await WriteAsync("a.txt", "content");

        OpfsNotSupportedException failure = await Assert.ThrowsAsync<OpfsNotSupportedException>(() => FileSystem.DownloadAsync(In("a.txt"), "a.txt"));

        Assert.Equal("NotSupportedError", failure.BrowserErrorName);
        Assert.Contains("worker", failure.Message);
        Assert.Contains("CreateObjectUrlAsync", failure.Message);
    }

    [Fact]
    public async Task Requesting_persistence_fails_naming_the_worker()
    {
        OpfsNotSupportedException failure = await Assert.ThrowsAsync<OpfsNotSupportedException>(() => new OpfsStorageInfo().RequestPersistenceAsync());

        Assert.Equal("NotSupportedError", failure.BrowserErrorName);
        Assert.Contains("worker", failure.Message);
    }
}
