namespace BlazorBasics.Opfs.Models;

/// <summary>A location in the file system, as the segments stored on disk. The default value is the root.</summary>
public readonly record struct OpfsPath
{
    private const char SEPARATOR = '/';

    private readonly string? Text;

    private OpfsPath(string text)
    {
        Text = text;
    }

    /// <summary>The root of the file system.</summary>
    public static OpfsPath Root => new(string.Empty);

    /// <summary>Takes a path like <c>a/b/c</c> as it is, with no encoding. Empty segments are dropped.</summary>
    /// <exception cref="ArgumentException">A segment holds <c>\</c>, or is <c>.</c> or <c>..</c>.</exception>
    public static OpfsPath Parse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        string[] segments = path.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
        foreach (string segment in segments)
        {
            OpfsNames.EnsureStorable(segment);
        }
        return new OpfsPath(string.Join(SEPARATOR, segments));
    }

    /// <summary>Builds a path from any names, encoding <c>/</c>, <c>\</c> and <c>%</c> so that each stays one segment.</summary>
    /// <exception cref="ArgumentException">A name is empty, <c>.</c> or <c>..</c>.</exception>
    public static OpfsPath FromSegments(IEnumerable<string> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        List<string> encoded = segments.Select(OpfsNames.Encode).ToList();
        encoded.ForEach(OpfsNames.EnsureStorable);
        return new OpfsPath(string.Join(SEPARATOR, encoded));
    }

    /// <summary>The segments as stored on disk.</summary>
    public IReadOnlyList<string> Segments => Value.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The folder that holds this path. The parent of the root is the root.</summary>
    public OpfsPath Parent => new(Value.Contains(SEPARATOR) ? Value[..Value.LastIndexOf(SEPARATOR)] : string.Empty);

    /// <summary>The last segment as stored on disk, or an empty name for the root.</summary>
    public string Name => Value[(Value.LastIndexOf(SEPARATOR) + 1)..];

    /// <summary>Whether this is the root.</summary>
    public bool IsRoot => Value.Length == 0;

    private string Value => Text ?? string.Empty;

    /// <summary>Adds one stored segment, with no encoding: a name that came from a listing goes back in as it is.</summary>
    public OpfsPath Combine(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (name.Contains(SEPARATOR))
        {
            throw new ArgumentException($"'{name}' is more than one segment.", nameof(name));
        }
        OpfsNames.EnsureStorable(name);
        return new OpfsPath(IsRoot ? name : $"{Value}{SEPARATOR}{name}");
    }

    /// <summary>The same as <see cref="Parse"/>.</summary>
    public static implicit operator OpfsPath(string path) => Parse(path);

    /// <summary>Whether both are the same location. The default value equals the root.</summary>
    public bool Equals(OpfsPath other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <summary>A hash that agrees with <see cref="Equals(OpfsPath)"/>.</summary>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <summary>The stored path, like <c>a/b/c</c>.</summary>
    public override string ToString() => Value;
}
