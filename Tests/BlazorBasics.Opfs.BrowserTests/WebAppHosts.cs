namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// The two ways the test web app runs: built, in Development, and published in Release, in Production, where the
/// WebAssembly side is trimmed and static assets come from the publish folder.
/// </summary>
public sealed class WebAppHosts : IAsyncLifetime
{
    public const string BUILT = "built";
    public const string PUBLISHED = "published";

    private const string PROJECT = "BlazorBasics.Opfs.WebApp";
    private const string PUBLISHED_FOLDER = "PublishedWebApp";

    private WebAppProcess Built = null!;
    private WebAppProcess Published = null!;

    internal WebAppProcess Of(string host) => host == PUBLISHED ? Published : Built;

    public async Task InitializeAsync()
    {
        Built = await WebAppProcess.StartAsync(BuildFolder(), "Development");
        Published = await WebAppProcess.StartAsync(Path.Combine(AppContext.BaseDirectory, PUBLISHED_FOLDER), "Production");
    }

    public async Task DisposeAsync()
    {
        await Built.DisposeAsync();
        await Published.DisposeAsync();
    }

    private static string BuildFolder()
    {
        DirectoryInfo output = new(AppContext.BaseDirectory);
        DirectoryInfo configuration = output.Parent!;
        DirectoryInfo testsFolder = configuration.Parent!.Parent!.Parent!;
        return Path.Combine(testsFolder.FullName, PROJECT, "bin", configuration.Name, output.Name);
    }
}
