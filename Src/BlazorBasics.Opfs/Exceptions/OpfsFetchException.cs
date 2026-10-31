namespace BlazorBasics.Opfs.Exceptions;

/// <summary>A download failed: the network, or an HTTP error status.</summary>
public sealed class OpfsFetchException : OpfsException
{
    /// <summary>Creates the exception.</summary>
    public OpfsFetchException(string message, int statusCode, Exception? innerException = null)
        : base(message, null, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>The HTTP status, or zero when no response arrived.</summary>
    public int StatusCode { get; }
}
