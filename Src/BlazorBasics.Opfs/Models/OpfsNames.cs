using System.Text;

namespace BlazorBasics.Opfs.Models;

/// <summary>
/// Turns any name into one the file system stores, which never holds <c>/</c> or <c>\</c>: those two and <c>%</c> are
/// written as <c>%2F</c>, <c>%5C</c> and <c>%25</c>, so every name comes back as it was.
/// </summary>
public static class OpfsNames
{
    /// <summary>The stored form of a name.</summary>
    public static string Encode(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        StringBuilder encoded = new(name.Length);
        foreach (char character in name)
        {
            encoded.Append(character switch
            {
                '%' => "%25",
                '/' => "%2F",
                '\\' => "%5C",
                _ => character.ToString()
            });
        }
        return encoded.ToString();
    }

    /// <summary>The name a stored name was encoded from.</summary>
    public static string Decode(string storedName)
    {
        ArgumentNullException.ThrowIfNull(storedName);
        return storedName.Replace("%2F", "/").Replace("%5C", "\\").Replace("%25", "%");
    }

    internal static void EnsureStorable(string segment)
    {
        if (segment.Length == 0 || segment == "." || segment == ".." || segment.Contains('\\'))
        {
            throw new ArgumentException($"'{segment}' cannot be a name in the file system.", nameof(segment));
        }
    }
}
