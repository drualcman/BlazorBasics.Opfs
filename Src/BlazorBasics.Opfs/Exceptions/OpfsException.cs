namespace BlazorBasics.Opfs.Exceptions;

/// <summary>A failure of the file system. The original browser failure is the <see cref="Exception.InnerException"/>.</summary>
public class OpfsException : IOException
{
    /// <summary>Creates the exception.</summary>
    public OpfsException(string message, string? browserErrorName = null, Exception? innerException = null)
        : base(message, innerException)
    {
        BrowserErrorName = browserErrorName;
    }

    /// <summary>The name of the browser error, like <c>NotFoundError</c>, when it came from one.</summary>
    public string? BrowserErrorName { get; }
}
