namespace BlazorBasics.Opfs.Services;

/// <summary>Turns the <c>name: message</c> failures of <c>opfs.js</c> into typed exceptions, keeping the original inside.</summary>
internal static class OpfsErrorTranslator
{
    private const string ERROR_PREFIX = "Error: ";
    private const string FETCH_ERROR_NAME = "OpfsFetchError";

    public static OpfsException Translate(Exception failure)
    {
        (string name, string detail) = NameAndDetail(failure.Message);
        return name switch
        {
            "NotFoundError" => new OpfsNotFoundException(detail, name, failure),
            "TypeMismatchError" => new OpfsTypeMismatchException(detail, name, failure),
            "QuotaExceededError" => new OpfsQuotaExceededException(detail, name, failure),
            "NoModificationAllowedError" => new OpfsFileBusyException(detail, name, failure),
            "SecurityError" or "NotAllowedError" => new OpfsAccessDeniedException(detail, name, failure),
            FETCH_ERROR_NAME => FetchFailure(detail, failure),
            _ => new OpfsException(detail, name, failure)
        };
    }

    private static (string Name, string Detail) NameAndDetail(string message)
    {
        string text = message.StartsWith(ERROR_PREFIX, StringComparison.Ordinal) ? message[ERROR_PREFIX.Length..] : message;
        int separator = text.IndexOf(':');
        return separator < 0 ? (string.Empty, text) : (text[..separator], text[(separator + 1)..].Trim());
    }

    private static OpfsFetchException FetchFailure(string detail, Exception failure)
    {
        string[] parts = detail.Split('|', 2);
        int statusCode = int.TryParse(parts[0], out int parsed) ? parsed : 0;
        string text = parts.Length > 1 ? parts[1] : detail;
        return new OpfsFetchException($"The download failed with status {statusCode}: {text}", statusCode, failure);
    }
}
