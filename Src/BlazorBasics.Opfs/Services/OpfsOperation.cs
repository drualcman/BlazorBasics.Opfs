namespace BlazorBasics.Opfs.Services;

/// <summary>Ties a cancellation token to an operation running in JavaScript, which an abort really stops.</summary>
[SupportedOSPlatform("browser")]
internal static class OpfsOperation
{
    private static int LastOperationId;

    public static async Task<TResult> RunAsync<TResult>(Func<int, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        int operationId = Interlocked.Increment(ref LastOperationId);
        Task<TResult> running = operation(operationId);
        await using CancellationTokenRegistration registration = cancellationToken.Register(() => OpfsTransferInterop.AbortOperation(operationId));
        return await running;
    }
}
