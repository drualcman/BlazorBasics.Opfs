[![Nuget](https://img.shields.io/nuget/v/BlazorBasics.Opfs?style=for-the-badge)](https://www.nuget.org/packages/BlazorBasics.Opfs)
[![Nuget](https://img.shields.io/nuget/dt/BlazorBasics.Opfs?style=for-the-badge)](https://www.nuget.org/packages/BlazorBasics.Opfs)

# BlazorBasics.Opfs

A file system for .NET running in the browser, over the Origin Private File System (OPFS).

You work with paths, not handles. Every write is all or nothing. Failures arrive as typed exceptions. And large bytes
never cross into WebAssembly memory: copying, moving and downloading a file all stay in JavaScript, so a 128 MiB video
costs .NET the same as a thumbnail.

It manages files and nothing else: no database, no metadata store, no UI, and no fallback to another store. Falling back
is your application's decision.

Despite the name it has no dependency on Blazor. It works in Blazor WebAssembly, plain WebAssembly and Uno. Its only
dependency is `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Install

```
dotnet add package BlazorBasics.Opfs
```

```csharp
// Program.cs
builder.Services.AddOpfs();
```

Without a container, create it directly: `IOpfsFileSystem fileSystem = new OpfsFileSystem();`.

## The bare minimum

```csharp
public class Notes(IOpfsFileSystem files)
{
    public async Task<string?> SaveAndReadAsync()
    {
        OpfsProbeResult probe = await files.ProbeAsync();
        string? text = null;
        if (probe.IsAvailable)
        {
            await files.WriteTextAsync("notes/today.txt", "Hello");
            text = await files.ReadTextAsync("notes/today.txt");
        }
        return text;
    }
}
```

## Paths

`OpfsPath` converts from a string, which is taken as it is. Use `FromSegments` for names you do not control, because it
encodes `/`, `\` and `%`:

```csharp
OpfsPath plain = "photos/2026/a.jpg";
OpfsPath safe = OpfsPath.FromSegments(["backups", "D:\\"]);
```

A listing gives each name as stored in `Name`, which goes back into `OpfsPath.Combine` unchanged, and as written in
`DecodedName`.

## What is in it

| Group | Members |
| --- | --- |
| Reading | `GetInfoOrNullAsync`, `OpenSnapshotOrNullAsync`, `ListAsync`, `ListTreeAsync`, `ReadTextAsync`, `ReadBytesAsync`, `ReadRangeAsync`, `OpenLazyRead` |
| Writing | `WriteBytesAsync`, `WriteTextAsync`, `WriteStreamAsync`, `OpenWriterAsync`, `AppendAsync`, `TruncateAsync` |
| No bytes in .NET | `CopyAsync`, `MoveFileAsync`, `FetchToAsync`, `CreateObjectUrlAsync`, `RevokeObjectUrl`, `DownloadAsync` |
| Removing | `DeleteAsync`, `ClearFolderAsync` |
| Whole folders | `MoveFolderAsync`, `FinishPendingFolderMovesAsync` |
| Around the files | `IOpfsLocks` (Web Locks), `IOpfsStorageInfo` (estimate and persistence), `ProbeAsync` |

Long writes go through one writer. Each `AppendAsync` makes the browser copy the whole file, so never use it once per
chunk:

```csharp
await using IOpfsWriter writer = await files.OpenWriterAsync("videos/take1.webm");
foreach (byte[] chunk in chunks)
{
    await writer.WriteAsync(chunk);
}
await writer.CompleteAsync();
```

Disposing a writer without completing it aborts: the file stays as it was, and a file it created is gone.

`FetchToAsync` streams a URL straight to a file. Going over `MaxBytes` is a result, not an exception. Cancelling really
stops the request.

```csharp
OpfsFetchResult result = await files.FetchToAsync(url, "cache/photo.jpg", new OpfsFetchOptions { MaxBytes = 50_000_000 });
```

## Errors

Nothing is swallowed. Every failure is an `OpfsException`, which is an `IOException`, with the browser error in
`BrowserErrorName` and the original as `InnerException`.

| Browser error | Exception |
| --- | --- |
| `NotFoundError` | `OpfsNotFoundException` |
| `TypeMismatchError` | `OpfsTypeMismatchException` |
| `QuotaExceededError` | `OpfsQuotaExceededException` |
| `NoModificationAllowedError` | `OpfsFileBusyException` |
| `SecurityError`, `NotAllowedError` | `OpfsAccessDeniedException` |
| Network failure or HTTP error status | `OpfsFetchException`, with `StatusCode` |
| Anything else | `OpfsException` |

There are three deliberate exceptions to "missing throws":

- `...OrNull` methods return null for `NotFound` only, because in a hot path an exception per lookup is a real cost.
- `DeleteAsync` and `ClearFolderAsync` succeed on what is not there, as `File.Delete` does.
- `ProbeAsync` answers, and keeps the failure in its result.

`ListAsync` on a missing folder throws `OpfsNotFoundException`.

## Where it runs

OPFS is in the browser, and `[JSImport]` only works with .NET running there.

| Host | Works? |
| --- | --- |
| Blazor WebAssembly, plain WebAssembly, Uno | Yes |
| Blazor Web App, component with `InteractiveWebAssembly` | Yes, in the interactive phase |
| Same, during prerendering | No: it runs on the server |
| `InteractiveAuto` | **Not safe**: the first visit runs on the server until the runtime is downloaded |
| `InteractiveServer`, static SSR, MAUI Blazor Hybrid | No |

`AddOpfs()` succeeds on a server, because a client project's startup also runs there for prerendering. Calling an
operation there throws `OpfsNotSupportedException`, and `ProbeAsync` answers "not available". With prerendering on, use
`InteractiveWebAssembly` and call from `OnAfterRenderAsync`, not `OnInitializedAsync`.

The browser baseline is Chrome and Edge, Firefox 111+, Chrome Android 109+ and Safari 26+. Older browsers get a clear
"not available" from `ProbeAsync`.

## Browser differences to know

Measured on Chromium 156, Firefox 157 and WebKit 27.2 on Linux:

| Behaviour | Chromium | Firefox | WebKit |
| --- | --- | --- | --- |
| Reading a snapshot after the file was written | `NotReadableError` | `AbortError` | Gives the new content, with no error |
| Moving a file with an open writer on it | Refuses | Refuses | Moves it |
| Extra space during a `keepExistingData` write | The whole file | The whole file | Not visible in the estimate |
| The estimate going down after a file is removed | Yes | Yes | **No** |

- **Snapshots** are only valid until the next write. Do not let a write overlap a series of reads.
- **`keepExistingData`** copies the whole file for as long as the writer is open, so usage doubles for a moment.
- **The estimate is only an estimate.** In the WebKit run it never went down, so do not refuse a write on it alone.
- **`move()` overwrites** an existing destination in all three engines. A busy file is reported, never copied around.
- **A closed tab** leaves the previous content and no visible leftovers in all three engines.

## From JavaScript

`File` objects cannot cross between `JSObject` and `IJSObjectReference`, so a bridge has to work in JavaScript terms. The
module at `_content/BlazorBasics.Opfs/opfs.js` exports these as public API. Paths are arrays of segments:

| Function | What it does |
| --- | --- |
| `getFile(names)` | The `File`, or null when it is not there |
| `requireFile(names)` | The `File`, or it rejects |
| `writeBlob(names, blob)` | Writes a `Blob` or `File`, all or nothing, and returns its length |

## Testing your application

`BlazorBasics.Opfs.Testing` is an in-memory double for plain `dotnet test`. It passes the same contract battery as the
real file system, so it cannot drift from real behaviour:

```csharp
InMemoryOpfsFileSystem files = new();
await files.WriteTextAsync("queue/a.meta", "{}");
```

Downloads go through its `Fetcher` property, and `InMemoryOpfsLocks` and `InMemoryOpfsStorageInfo` cover the other two
abstractions.

## Developing this repository

```
dotnet test Tests/BlazorBasics.Opfs.Tests           # no browser: paths, errors, the double, the battery on the double
dotnet test Tests/BlazorBasics.Opfs.BrowserTests    # the battery in real browsers
pwsh Tests/run-webkit-tests.ps1                     # the same in WebKit, on Linux through Docker
```

The browser tests need Playwright's browsers once:
`pwsh Tests/BlazorBasics.Opfs.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium firefox`.

**WebKit does not run on Windows.** Playwright's WebKit for Windows accepts writes with no error and keeps nothing, so
those tests run in Docker. WebKit also needs a persistent profile: in an ephemeral context it has no OPFS at all.

Playwright's WebKit is not real Safari. These stay manual checks before a release:

- [ ] Safari on iOS 26
- [ ] Chrome on Android
- [ ] Firefox in private mode

The demo in `Samples/BlazorBasics.Opfs.Demo` is a diagnostics page: `dotnet run --project Samples/BlazorBasics.Opfs.Demo`.
