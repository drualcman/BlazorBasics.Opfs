namespace BlazorBasics.Opfs.Exceptions;

/// <summary>The browser refuses the storage, as some private modes do (<c>SecurityError</c>, <c>NotAllowedError</c>).</summary>
public sealed class OpfsAccessDeniedException : OpfsException
{
    /// <summary>Creates the exception.</summary>
    public OpfsAccessDeniedException(string message, string? browserErrorName = null, Exception? innerException = null)
        : base(message, browserErrorName, innerException)
    {
    }
}
