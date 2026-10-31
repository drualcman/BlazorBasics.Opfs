using System.Runtime.InteropServices.JavaScript;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>The real file system as the target of the battery: each test gets its own folder under <see cref="TESTS_FOLDER"/>.</summary>
public sealed class BrowserContractTarget : IOpfsContractTarget
{
    public const string TESTS_FOLDER = "opfs-contract";

    public IOpfsFileSystem FileSystem { get; } = new OpfsFileSystem();

    public IOpfsLocks Locks { get; } = new OpfsLocks();

    public OpfsPath Folder { get; } = $"{TESTS_FOLDER}/{Guid.NewGuid():N}";

    public Uri FixtureUrl(string route)
    {
        using JSObject location = JSHost.GlobalThis.GetPropertyAsJSObject("location")!;
        return new Uri($"{location.GetPropertyAsString("origin")}/fixture/{route}");
    }
}
