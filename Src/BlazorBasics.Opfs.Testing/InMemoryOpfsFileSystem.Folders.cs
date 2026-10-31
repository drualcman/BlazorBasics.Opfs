using System.Text.Json;

namespace BlazorBasics.Opfs.Testing;

public sealed partial class InMemoryOpfsFileSystem
{
    /// <inheritdoc />
    public Task DeleteAsync(OpfsPath fileOrFolder, CancellationToken cancellationToken = default)
    {
        Store.FolderExists(fileOrFolder.Parent);
        if (fileOrFolder.IsRoot || Store.Folders.Contains(fileOrFolder))
        {
            RemoveUnder(fileOrFolder);
            if (!fileOrFolder.IsRoot)
            {
                Store.Folders.Remove(fileOrFolder);
            }
        }
        else if (Store.Files.TryGetValue(fileOrFolder, out InMemoryFile? file))
        {
            EnsureFree(fileOrFolder, file);
            Store.Files.Remove(fileOrFolder);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearFolderAsync(OpfsPath folder, CancellationToken cancellationToken = default)
    {
        if (Store.Folders.Contains(folder))
        {
            RemoveUnder(folder);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task MoveFolderAsync(OpfsPath root, OpfsPath source, OpfsPath destination, OpfsFolderMoveOptions options, CancellationToken cancellationToken = default)
    {
        await FinishPendingFolderMovesAsync(root, options, cancellationToken);
        if (Store.Folders.Contains(Under(root, source)))
        {
            Store.EnsureFolder(root.Combine(options.NotesFolderName), true);
            await MoveContentAsync(Under(root, source), Under(root, destination), cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task FinishPendingFolderMovesAsync(OpfsPath root, OpfsFolderMoveOptions options, CancellationToken cancellationToken = default)
    {
        OpfsPath notes = root.Combine(options.NotesFolderName);
        List<OpfsPath> pending = Store.Files.Keys.Where(file => file.Parent == notes).ToList();
        foreach (OpfsPath note in pending)
        {
            using JsonDocument move = JsonDocument.Parse(Store.Files[note].Content);
            await MoveContentAsync(PathOf(move.RootElement.GetProperty("source")), PathOf(move.RootElement.GetProperty("destination")), cancellationToken);
            Store.Files.Remove(note);
        }
    }

    private static OpfsPath Under(OpfsPath root, OpfsPath relative)
    {
        OpfsPath combined = root;
        foreach (string segment in relative.Segments)
        {
            combined = combined.Combine(segment);
        }
        return combined;
    }

    private static OpfsPath PathOf(JsonElement segments)
    {
        OpfsPath path = OpfsPath.Root;
        foreach (JsonElement segment in segments.EnumerateArray())
        {
            path = path.Combine(segment.GetString()!);
        }
        return path;
    }

    private async Task MoveContentAsync(OpfsPath source, OpfsPath destination, CancellationToken cancellationToken)
    {
        if (Store.Folders.Contains(source))
        {
            Store.EnsureFolder(destination, true);
            string prefix = $"{source}/";
            foreach (OpfsPath file in Store.Files.Keys.Where(file => Store.IsUnder(file, source)).ToList())
            {
                await MoveFileAsync(file, $"{destination}/{file.ToString()[prefix.Length..]}", cancellationToken);
            }
            foreach (OpfsPath folder in Store.Folders.Where(folder => Store.IsUnder(folder, source)).ToList())
            {
                Store.EnsureFolder($"{destination}/{folder.ToString()[prefix.Length..]}", true);
            }
            await DeleteAsync(source, cancellationToken);
        }
    }

    private void RemoveUnder(OpfsPath folder)
    {
        List<KeyValuePair<OpfsPath, InMemoryFile>> files = Store.Files.Where(file => Store.IsUnder(file.Key, folder)).ToList();
        files.ForEach(file => EnsureFree(file.Key, file.Value));
        files.ForEach(file => Store.Files.Remove(file.Key));
        Store.Folders.RemoveWhere(found => Store.IsUnder(found, folder));
    }

    private static void EnsureFree(OpfsPath path, InMemoryFile file)
    {
        if (file.OpenWriters > 0)
        {
            throw new OpfsFileBusyException($"{path} has an open writer.", "NoModificationAllowedError");
        }
    }
}
