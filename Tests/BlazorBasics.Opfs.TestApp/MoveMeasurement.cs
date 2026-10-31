namespace BlazorBasics.Opfs.TestApp;

/// <summary>Whether <c>move()</c> respects an open writer, on the file that moves and on the one it replaces.</summary>
internal sealed class MoveMeasurement
{
    private const string FOLDER = "opfs-device-checks/move";
    private const string SOURCE = $"{FOLDER}/source.txt";
    private const string DESTINATION = $"{FOLDER}/destination.txt";

    private readonly OpfsFileSystem FileSystem = new();

    public async Task<IReadOnlyList<MoveAnswer>> RunAsync()
    {
        List<MoveAnswer> answers = [await MoveWithWriterOnAsync("source", SOURCE), await MoveWithWriterOnAsync("destination", DESTINATION)];
        await FileSystem.DeleteAsync(FOLDER);
        return answers;
    }

    private async Task<MoveAnswer> MoveWithWriterOnAsync(string name, OpfsPath writerFile)
    {
        await FileSystem.DeleteAsync(FOLDER);
        await FileSystem.WriteTextAsync(SOURCE, "source");
        await FileSystem.WriteTextAsync(DESTINATION, "destination");

        IOpfsWriter writer = await FileSystem.OpenWriterAsync(writerFile);
        string? moveError = await ErrorOfAsync(() => FileSystem.MoveFileAsync(SOURCE, DESTINATION));
        string? writerError = await ErrorOfAsync(async () =>
        {
            await writer.WriteAsync("written by the writer"u8.ToArray());
            await writer.CompleteAsync();
        });
        await writer.DisposeAsync();
        return new MoveAnswer(name, moveError, writerError, await TextOrNullAsync(SOURCE), await TextOrNullAsync(DESTINATION));
    }

    private static async Task<string?> ErrorOfAsync(Func<Task> action)
    {
        string? error = null;
        try
        {
            await action();
        }
        catch (OpfsException failure)
        {
            error = $"{failure.GetType().Name} ({failure.BrowserErrorName})";
        }
        return error;
    }

    private async Task<string?> TextOrNullAsync(OpfsPath file)
    {
        string? text = null;
        if (await FileSystem.GetInfoOrNullAsync(file) is not null)
        {
            text = await FileSystem.ReadTextAsync(file);
        }
        return text;
    }
}
