namespace BlazorBasics.Opfs.ContractTests;

/// <summary>What the battery runs against. A new target is made for every test, with its own empty folder.</summary>
public interface IOpfsContractTarget
{
    IOpfsFileSystem FileSystem { get; }

    IOpfsLocks Locks { get; }

    OpfsPath Folder { get; }

    /// <summary>The address of a fixture route, like <c>bytes/100</c>. See <see cref="FetchContractTests{TTarget}"/>.</summary>
    Uri FixtureUrl(string route);
}
