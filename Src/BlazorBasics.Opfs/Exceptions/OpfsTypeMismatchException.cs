namespace BlazorBasics.Opfs.Exceptions;

/// <summary>A file was found where a folder was expected, or the reverse (<c>TypeMismatchError</c>).</summary>
public sealed class OpfsTypeMismatchException : OpfsException
{
    /// <summary>Creates the exception.</summary>
    public OpfsTypeMismatchException(string message, string? browserErrorName = null, Exception? innerException = null)
        : base(message, browserErrorName, innerException)
    {
    }
}
