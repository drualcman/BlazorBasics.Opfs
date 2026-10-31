namespace BlazorBasics.Opfs.Testing;

/// <summary>The files and folders, with the lookups that fail the way the browser does.</summary>
internal sealed class InMemoryStore
{
    public Dictionary<OpfsPath, InMemoryFile> Files { get; } = [];

    public HashSet<OpfsPath> Folders { get; } = [OpfsPath.Root];

    public void EnsureFolder(OpfsPath folder, bool create)
    {
        OpfsPath current = OpfsPath.Root;
        foreach (string segment in folder.Segments)
        {
            current = current.Combine(segment);
            if (Files.ContainsKey(current))
            {
                throw new OpfsTypeMismatchException($"{current} is a file.", "TypeMismatchError");
            }
            if (!Folders.Contains(current))
            {
                if (!create)
                {
                    throw new OpfsNotFoundException($"{current} is not there.", "NotFoundError");
                }
                Folders.Add(current);
            }
        }
    }

    public InMemoryFile? FileOrNull(OpfsPath file)
    {
        EnsureIsFile(file);
        InMemoryFile? found = null;
        if (FolderExists(file.Parent))
        {
            Files.TryGetValue(file, out found);
        }
        return found;
    }

    public InMemoryFile? FileOrNullCreatingFolders(OpfsPath file)
    {
        EnsureIsFile(file);
        EnsureFolder(file.Parent, true);
        return FileOrNull(file);
    }

    public InMemoryFile RequireFile(OpfsPath file)
    {
        EnsureIsFile(file);
        EnsureFolder(file.Parent, false);
        return FileOrNull(file) ?? throw new OpfsNotFoundException($"{file} is not there.", "NotFoundError");
    }

    public bool IsUnder(OpfsPath path, OpfsPath folder) =>
        folder.IsRoot ? !path.IsRoot : path.ToString().StartsWith($"{folder}/", StringComparison.Ordinal);

    public bool FolderExists(OpfsPath folder)
    {
        bool exists = true;
        try
        {
            EnsureFolder(folder, false);
        }
        catch (OpfsNotFoundException)
        {
            exists = false;
        }
        return exists;
    }

    private void EnsureIsFile(OpfsPath file)
    {
        if (file.IsRoot)
        {
            throw new ArgumentException("The root is a folder, not a file.", nameof(file));
        }
        if (Folders.Contains(file))
        {
            throw new OpfsTypeMismatchException($"{file} is a folder.", "TypeMismatchError");
        }
    }
}
