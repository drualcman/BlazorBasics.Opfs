namespace BlazorBasics.Opfs.Abstractions;

/// <summary>
/// One file as it was when taken, for several reads with no extra lookups. It is only valid until the file is next written.
/// After that Chromium and Firefox fail the read with an <see cref="OpfsException"/>, while WebKit gives the new content
/// with no error, so a caller that needs consistent reads must not let a write overlap them.
/// </summary>
public interface IOpfsFileSnapshot : IDisposable
{
    /// <summary>The length of the file when the snapshot was taken.</summary>
    long Length { get; }

    /// <summary>Reads part of the file. A range past the end comes back shorter.</summary>
    Task<byte[]> ReadRangeAsync(long start, long length, CancellationToken cancellationToken = default);

    /// <summary>Gives a stream over the snapshot, which then owns it: dispose the stream instead.</summary>
    Stream OpenRead();
}
