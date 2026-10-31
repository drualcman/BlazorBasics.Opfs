using System.Runtime.InteropServices;

namespace BlazorBasics.Opfs.Services;

/// <summary>Gives bytes the array form the interop takes, with no copy when they already live in one.</summary>
internal static class OpfsContent
{
    public static ArraySegment<byte> SegmentOf(ReadOnlyMemory<byte> content)
    {
        ArraySegment<byte> segment;
        if (content.IsEmpty)
        {
            segment = new ArraySegment<byte>([]);
        }
        else if (!MemoryMarshal.TryGetArray(content, out segment))
        {
            segment = new ArraySegment<byte>(content.ToArray());
        }
        return segment;
    }

    public static string[] NamesOf(OpfsPath file)
    {
        if (file.IsRoot)
        {
            throw new ArgumentException("The root is a folder, not a file.", nameof(file));
        }
        return [.. file.Segments];
    }

    public static string[] FolderNamesOf(OpfsPath folder) => [.. folder.Segments];
}
