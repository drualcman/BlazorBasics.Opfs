namespace BlazorBasics.Opfs.Services;

/// <summary>What every browser operation goes through: the host check, the module import and the translation of failures.</summary>
internal static class OpfsCall
{
    public static void EnsureBrowser()
    {
        if (!OperatingSystem.IsBrowser())
        {
            throw new OpfsNotSupportedException();
        }
    }

    [SupportedOSPlatform("browser")]
    public static async Task RunAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        await RunAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    [SupportedOSPlatform("browser")]
    public static async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken)
    {
        EnsureBrowser();
        cancellationToken.ThrowIfCancellationRequested();
        await OpfsModule.ImportedAsync().WaitAsync(cancellationToken);
        TResult result;
        try
        {
            result = await operation();
        }
        catch (JSException failure) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(failure.Message, failure, cancellationToken);
        }
        catch (JSException failure)
        {
            throw OpfsErrorTranslator.Translate(failure);
        }
        return result;
    }
}
