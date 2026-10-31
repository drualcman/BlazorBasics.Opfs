using System.Text;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>What a snapshot gives when read after its file was written, with the same length and with a longer one.</summary>
internal sealed class SnapshotMeasurement
{
    private const string FILE = "opfs-device-checks/snapshot.txt";

    private readonly OpfsFileSystem FileSystem = new();

    public async Task<IReadOnlyList<SnapshotReadAnswer>> RunAsync()
    {
        List<SnapshotReadAnswer> answers =
        [
            await ReadAfterWriteAsync("same-length", "new"),
            await ReadAfterWriteAsync("longer", "newer and longer")
        ];
        await FileSystem.DeleteAsync(FILE);
        return answers;
    }

    private async Task<SnapshotReadAnswer> ReadAfterWriteAsync(string name, string written)
    {
        await FileSystem.WriteTextAsync(FILE, "old");
        using IOpfsFileSnapshot snapshot = (await FileSystem.OpenSnapshotOrNullAsync(FILE))!;
        await FileSystem.WriteTextAsync(FILE, written);

        string? read = null;
        string? browserError = null;
        try
        {
            read = Encoding.UTF8.GetString(await snapshot.ReadRangeAsync(0, written.Length));
        }
        catch (OpfsException failure)
        {
            browserError = failure.BrowserErrorName ?? failure.GetType().Name;
        }
        return new SnapshotReadAnswer(name, snapshot.Length, read, browserError);
    }
}
