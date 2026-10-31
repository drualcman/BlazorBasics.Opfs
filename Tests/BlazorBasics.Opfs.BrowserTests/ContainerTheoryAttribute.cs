namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// A test that kills browsers or fills a disk, so it only runs in the container of <c>Tests/run-webkit-tests.ps1</c>,
/// which gives it <c>OPFS_DISK_FOLDER</c>, a small volume of its own. Naming an engine skips it in a run without that engine.
/// </summary>
public sealed class ContainerTheoryAttribute : TheoryAttribute
{
    private const string DISK_VARIABLE = "OPFS_DISK_FOLDER";

    public ContainerTheoryAttribute(string? engine = null)
    {
        if (DiskFolder is null || (engine is not null && !BrowserEngines.Names.Contains(engine)))
        {
            Skip = "This run is not in the container, or it has no such engine.";
        }
    }

    public static string? DiskFolder => Environment.GetEnvironmentVariable(DISK_VARIABLE);
}
