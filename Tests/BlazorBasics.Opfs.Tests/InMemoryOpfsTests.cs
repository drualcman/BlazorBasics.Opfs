using BlazorBasics.Opfs.Testing;

namespace BlazorBasics.Opfs.Tests;

public sealed class InMemoryOpfsTests
{
    [Fact]
    public async Task A_stale_snapshot_fails_as_in_chromium_and_firefox()
    {
        InMemoryOpfsFileSystem fileSystem = new();
        await fileSystem.WriteTextAsync("a.txt", "old");
        using IOpfsFileSnapshot? snapshot = await fileSystem.OpenSnapshotOrNullAsync("a.txt");

        await fileSystem.WriteTextAsync("a.txt", "new");

        OpfsException failure = await Assert.ThrowsAsync<OpfsException>(() => snapshot!.ReadRangeAsync(0, 3));
        Assert.Equal("NotReadableError", failure.BrowserErrorName);
    }

    [Fact]
    public async Task A_download_with_no_fetcher_fails_as_a_network_failure()
    {
        OpfsFetchException failure = await Assert.ThrowsAsync<OpfsFetchException>(() => new InMemoryOpfsFileSystem().FetchToAsync(new Uri("http://a/b"), "a.bin"));

        Assert.Equal(0, failure.StatusCode);
    }

    [Fact]
    public async Task The_probe_says_what_the_test_sets()
    {
        InMemoryOpfsFileSystem fileSystem = new() { ProbeResult = new OpfsProbeResult(false, "No OPFS here.") };

        Assert.False((await fileSystem.ProbeAsync()).IsAvailable);
    }

    [Fact]
    public async Task Storage_info_answers_what_the_test_sets()
    {
        InMemoryOpfsStorageInfo storage = new() { Estimate = new OpfsStorageEstimate(1, 2) };

        Assert.Equal(new OpfsStorageEstimate(1, 2), await storage.EstimateAsync());
        Assert.False(await storage.IsPersistedAsync());
        Assert.True(await storage.RequestPersistenceAsync());
        Assert.True(await storage.IsPersistedAsync());
    }
}
