using System.Runtime.Versioning;
using BlazorBasics.Opfs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorBasics.Opfs.Tests;

[SupportedOSPlatform("browser")]
public sealed class OutsideBrowserTests
{
    private readonly IOpfsFileSystem FileSystem = new OpfsFileSystem();

    [Fact]
    public void Add_opfs_succeeds_on_a_server_and_resolves_every_service()
    {
        using ServiceProvider provider = new ServiceCollection().AddOpfs().BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IOpfsFileSystem>());
        Assert.NotNull(provider.GetRequiredService<IOpfsLocks>());
        Assert.NotNull(provider.GetRequiredService<IOpfsStorageInfo>());
    }

    [Fact]
    public async Task Add_opfs_keeps_a_registration_made_before()
    {
        BlazorBasics.Opfs.Testing.InMemoryOpfsFileSystem own = new();
        ServiceCollection services = new();
        services.AddSingleton<IOpfsFileSystem>(own);
        services.AddOpfs();

        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(own, provider.GetRequiredService<IOpfsFileSystem>());
    }

    [Fact]
    public async Task The_probe_answers_instead_of_throwing()
    {
        OpfsProbeResult result = await FileSystem.ProbeAsync();

        Assert.False(result.IsAvailable);
        Assert.IsType<OpfsNotSupportedException>(result.Failure);
    }

    [Fact]
    public async Task Every_kind_of_operation_throws_naming_the_cause()
    {
        OpfsNotSupportedException failure = await Assert.ThrowsAsync<OpfsNotSupportedException>(() => FileSystem.ReadTextAsync("a.txt"));
        Assert.Contains("prerendering", failure.Message);
        Assert.Contains("InteractiveWebAssembly", failure.Message);

        await Assert.ThrowsAsync<OpfsNotSupportedException>(() => FileSystem.WriteBytesAsync("a.txt", new byte[1]));
        await Assert.ThrowsAsync<OpfsNotSupportedException>(() => FileSystem.WriteStreamAsync("a.txt", new MemoryStream()));
        await Assert.ThrowsAsync<OpfsNotSupportedException>(() => FileSystem.ListAsync("a"));
        await Assert.ThrowsAsync<OpfsNotSupportedException>(() => FileSystem.CopyAsync("a", "b"));
        await Assert.ThrowsAsync<OpfsNotSupportedException>(() => FileSystem.DeleteAsync("a"));
        await Assert.ThrowsAsync<OpfsNotSupportedException>(() => new OpfsLocks().TryAcquireAsync("a", OpfsLockMode.Exclusive));
        await Assert.ThrowsAsync<OpfsNotSupportedException>(() => new OpfsStorageInfo().EstimateAsync());
        Assert.Throws<OpfsNotSupportedException>(() => FileSystem.RevokeObjectUrl("blob:a"));
    }
}
