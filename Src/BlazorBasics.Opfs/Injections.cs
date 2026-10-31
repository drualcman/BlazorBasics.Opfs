using BlazorBasics.Opfs.Abstractions;
using BlazorBasics.Opfs.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registration helpers for the browser file system.</summary>
public static class Injections
{
    /// <summary>
    /// Registers the file system, the locks and the storage information as singletons. It also succeeds on a server,
    /// where a client project's startup runs for prerendering: only using a service there throws.
    /// </summary>
    /// <param name="services">The container being built.</param>
    public static IServiceCollection AddOpfs(this IServiceCollection services)
    {
#pragma warning disable CA1416 // Registering loads no browser code: every browser operation guards itself.
        services.TryAddSingleton<IOpfsFileSystem, OpfsFileSystem>();
        services.TryAddSingleton<IOpfsLocks, OpfsLocks>();
        services.TryAddSingleton<IOpfsStorageInfo, OpfsStorageInfo>();
#pragma warning restore CA1416
        return services;
    }
}
