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

A lock is released by disposing it, and the browser can also take it away while you hold it. If a lock protects more than a
single short step, check it or watch for the loss:

```csharp
using IOpfsLock? held = await locks.TryAcquireAsync("database", OpfsLockMode.Exclusive);
if (held is not null)
{
    held.Lost.Register(StopWritingAndWarnTheUser);
    await WriteAsync(held.Lost);
}
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
| `NotSupportedError`, or a call outside a browser | `OpfsNotSupportedException` |
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
| A dedicated web worker, with .NET or with `opfs.js` alone | Yes, except `DownloadAsync` and `RequestPersistenceAsync` |
| Blazor Web App, component with `InteractiveWebAssembly` | Yes, in the interactive phase |
| Same, during prerendering | No: it runs on the server |
| `InteractiveAuto` | **Not safe**: the first visit runs on the server until the runtime is downloaded |
| `InteractiveServer`, static SSR, MAUI Blazor Hybrid | No |

In a Blazor Web App, reference the package from the client project and call `AddOpfs()` in both `Program` files. The client
one does not run on the server, and prerendering resolves the same services there, so a server without it fails with a 500.
`AddOpfs()` succeeds on a server, and loading the types is safe. Calling an operation there throws
`OpfsNotSupportedException`, and `ProbeAsync` answers "not available". With prerendering on, use `InteractiveWebAssembly` and
call from `OnAfterRenderAsync`, not `OnInitializedAsync`, which runs on the server first. In `InteractiveAuto`, check
`ProbeAsync` before any operation: it answers on the first visit without harming the circuit, and later visits run in the
browser.

The browser baseline is Chrome and Edge, Firefox 111+, Chrome Android 109+ and Safari 26+. Older browsers get a clear
"not available" from `ProbeAsync`.

### In a worker

The whole contract battery runs with .NET inside a dedicated worker in all three engines, so this needs nothing special.
Start the runtime as a page would, with the path to `_framework` as seen from the worker script:

```js
import { dotnet } from '../_framework/dotnet.js';

const runtime = await dotnet.create();
await runtime.runMain();
```

The package finds `opfs.js` from the folder of the runtime, so the worker script can live anywhere. Without .NET, import
`_content/BlazorBasics.Opfs/opfs.js` directly, from a module worker or with `import()` from a classic one.

Two operations need a page and throw `OpfsNotSupportedException` in a worker, because the browser offers them only there:

- `DownloadAsync` needs a `document`. In a worker, create the address with `CreateObjectUrlAsync` and post it to the page,
  which can use it for as long as the worker lives.
- `RequestPersistenceAsync` needs a page. `IsPersistedAsync` works in both.

Shared and service workers are not tested.

## Browser differences to know

Measured on Chromium 156, Firefox 157 and WebKit 27.2 on Linux:

| Behaviour | Chromium | Firefox | WebKit |
| --- | --- | --- | --- |
| Reading a snapshot after the file was written | `NotReadableError` | `AbortError` | Gives the new content, with no error |
| Moving a file with an open writer on it | Refuses | Refuses | Moves it |
| Extra space during a `keepExistingData` write | The whole file | The whole file | Not visible in the estimate |
| The estimate going down after a file is removed | Yes | Yes | **No**, only near the quota or after a restart |
| Where the swap of a write lives | The profile | The profile | **The temporary folder of the system** |

When the quota runs out, all three engines fail at `write`, never at `close`, with `QuotaExceededError`, which arrives as
`OpfsQuotaExceededException`. The previous content stays and no visible leftovers remain, on every way of writing:
writers, `WriteBytesAsync`, `CopyAsync` and `FetchToAsync`. What differs is around that:

| Behaviour | Chromium | Firefox | WebKit |
| --- | --- | --- | --- |
| Quota of the origin | From the free disk | From the free disk | 1000 MiB, whatever the disk |
| Replacing a file over half the quota | Fails: the swap counts | Fails: the swap counts | Fails: the staged copy counts |
| `keepExistingData` on a file over half the quota | Fails at `createWritable` | Works, and the estimate stops counting the file | Fails |
| The quota after hitting it | Unchanged | Unchanged | **Drops to 5 MiB** until the browser restarts |
| A disk that is really full | `QuotaExceededError` | `QuotaExceededError`, then `UnknownError` until there is space | `QuotaExceededError`, or `UnknownError` as a plain `OpfsException` if the swap itself did not fit |

A swap file is what a write leaves behind when it never finishes:

| Abandoned by | Chromium | Firefox | WebKit |
| --- | --- | --- | --- |
| A closed page | Removed, space back at once | Removed, space back at once | Removed, the estimate stays high |
| A killed browser | Stays and counts against the quota | Stays and counts against the quota | Stays in the temporary folder, does not count against the quota |
| Who cleans it | `DeleteAsync` on the file, or on its folder | `DeleteAsync` on the file | Nobody: the system, when it empties that folder |

Listings never show these leftovers: names ending in `.crswap` or `.opfs-staged` are left out, so do not give those endings
to your own files.

When someone leaves a page and comes back with the Back button, the browser may keep the page frozen in its back/forward
cache and restore it as it was, .NET included. A held lock and an open writer then do this:

| Behaviour | Chromium | Firefox | WebKit |
| --- | --- | --- | --- |
| The page enters the cache with a lock held | Yes | **No**, so Back loads it again | Yes |
| The lock while the page is in the cache | Stays held | Free as soon as the page is left | **Free as soon as the page is left** |
| Another tab asks for that lock | Gets it, and the page is thrown out of the cache | Gets it | Gets it, and the page stays in the cache |
| On coming back | Still held, or the page loads again if it was thrown out | The page loads again | **Restored without the lock: `Lost` is cancelled** |
| An open writer | Keeps working | Keeps working | Keeps working |

Only WebKit restores a page whose lock is gone, so `IsHeld` and `Lost` are for that case. In Chromium a call to
`navigator.locks.query()` from another tab is enough to throw the page out too.

- **Snapshots** are only valid until the next write. Do not let a write overlap a series of reads.
- **`keepExistingData`** copies the whole file for as long as the writer is open, so usage doubles for a moment.
- **Keep every file under half of the quota** if it has to be replaced or appended to, because of the swap.
- **The estimate is only an estimate.** In WebKit it reached 955 MiB with no files, and 1000 MiB could still be written:
  the number blocks nothing, and it says nothing about real space. Do not refuse a write on it alone.
- **WebKit keeps a full copy of every finished write.** `close` copies the swap into place and leaves it in the temporary
  folder of the system, even when the browser closes normally. It never counts against the quota, but it is real disk space.
- **WebKit's `close` does not check that copy.** On a nearly full disk it reports success and leaves a short file, with the
  previous content gone: with 40 MiB free, a raw 30 MiB write left 9.87 MiB. This package protects you from it: in WebKit
  each write closes into a staged file, checks its length, and only then replaces the real file, so a write that does not
  fit fails with `OpfsQuotaExceededException` and the previous content stays. The price is one more copy on the disk during
  each write. Code that calls `createWritable` itself is not protected.
- **`move()` overwrites** an existing destination in all three engines. A busy file is reported, never copied around.
- **A closed tab or a killed browser** leaves the previous content and no visible leftovers in all three engines.

The WebKit figures come from Playwright's build on Linux. Safari may differ, above all in the quota and the temporary folder.
The back/forward cache was measured on stock Firefox 159 and, for WebKit, with no automation, because Playwright keeps
Firefox and WebKit out of that cache. A lock held from a worker was not measured.

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
abstractions. `InMemoryOpfsLocks.Lose(name)` takes a lock away from its holders, to test what your code does then.

## Developing this repository

```
dotnet test Tests/BlazorBasics.Opfs.Tests           # no browser: paths, errors, the double, the battery on the double
dotnet test Tests/BlazorBasics.Opfs.BrowserTests    # the battery in real browsers, in a page and in a worker, and the web app
pwsh Tests/run-webkit-tests.ps1                     # the same in WebKit, on Linux through Docker
pwsh Tests/run-webkit-tests.ps1 -Browsers chromium,firefox,webkit    # all three on Linux
```

The tests that kill a browser or fill a disk only run in that container, which gives them a volume of their own.

`Tests/BlazorBasics.Opfs.WebApp` and its `.Client` project are a Blazor Web App with `InteractiveWebAssembly` and
`InteractiveAuto` pages, for `WebAppTests`, which runs it built and published in Release. It lives in `Tests` because its
pages call the package where they should not, to prove what happens.

The browser tests need Playwright's browsers once:
`pwsh Tests/BlazorBasics.Opfs.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium firefox`.

**WebKit does not run on Windows.** Playwright's WebKit for Windows accepts writes with no error and keeps nothing, so
those tests run in Docker. WebKit also needs a persistent profile: in an ephemeral context it has no OPFS at all.

Playwright's WebKit is not real Safari. These stay manual checks before a release:

- [ ] Safari on iOS 26
- [ ] Chrome on Android
- [ ] Firefox in private mode

The demo in `Samples/BlazorBasics.Opfs.Demo` is a diagnostics page: `dotnet run --project Samples/BlazorBasics.Opfs.Demo`.
