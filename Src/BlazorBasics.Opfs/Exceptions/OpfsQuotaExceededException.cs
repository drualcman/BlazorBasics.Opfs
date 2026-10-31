namespace BlazorBasics.Opfs.Exceptions;

/// <summary>The storage of the site is full (<c>QuotaExceededError</c>).</summary>
public sealed class OpfsQuotaExceededException : OpfsException
{
    /// <summary>Creates the exception.</summary>
    public OpfsQuotaExceededException(string message, string? browserErrorName = null, Exception? innerException = null)
        : base(message, browserErrorName, innerException)
    {
    }
}
