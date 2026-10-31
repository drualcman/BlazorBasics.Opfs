using BlazorBasics.Opfs.ContractTests;
using BlazorBasics.Opfs.Testing;

namespace BlazorBasics.Opfs.Tests;

/// <summary>The in-memory double as the target of the battery, with the fixture routes answered in memory.</summary>
public sealed class InMemoryContractTarget : IOpfsContractTarget
{
    private readonly InMemoryOpfsFileSystem InMemoryFileSystem = new() { Fetcher = FetchFixture.AnswerAsync };

    public IOpfsFileSystem FileSystem => InMemoryFileSystem;

    public IOpfsLocks Locks { get; } = new InMemoryOpfsLocks();

    public OpfsPath Folder { get; } = "contract";

    public Uri FixtureUrl(string route) => new($"http://fixture/{route}");
}
