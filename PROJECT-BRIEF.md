# BlazorBasics.Opfs: project brief

This brief is the starting point for a new session. It holds everything decided so far, so nothing needs to be
re-evaluated. Read it whole, then read the sources in section 4, and only then write code.

The folder holds only this file. There is no repository, project or solution yet.

## 1. What this is

A NuGet package that gives .NET running in the browser a file system over the Origin Private File System (OPFS).
It manages files and nothing else: no database, no metadata store, no fallback to IndexedDB, no UI components.

It exists because two applications carry their own OPFS code and should share it, so an improvement reaches both:

| Application | Today | Uses OPFS for |
| --- | --- | --- |
| ShotUpAlbums (`D:\ShotUpAlbums`), Blazor WebAssembly PWA | JS worker + `createSyncAccessHandle`, `IJSRuntime` | Upload queue, video recording, offline photo cache |
| AmbesaSql (`D:\JsonDatabase`), SQL engine | `createWritable`, `[JSImport]`, no Blazor | Database files, backups, engine configuration |

Positioning: a high-level OPFS file library for .NET in the browser, without a Blazor dependency, where large bytes
never cross into WebAssembly memory. Do not claim it is the only OPFS package on NuGet (see section 15).

### Non-goals

- Synchronous or random-access I/O for engines like SQLite. That needs sync access handles in a worker, the complexity
  this design removes on purpose.
- Blazor Server support (section 3, D3).
- Browsers older than the 2026 baseline. They get a clear "unavailable" answer from the probe and nothing else.
- Falling back to another store. Falling back is the application's decision.

## 2. Working rules for this project

Project memory does not follow this new folder, so these rules are copied here. They come from the user.

- Reply to the user in Spanish. Code, comments, identifiers and documentation are in English.
- Always commit directly to `master`. Never create a branch.
- Never commit just because it builds: the user reviews the working tree first and authorises each commit.
- Run the tests after any code change, not only the build.
- Never add a local NuGet source or a local project reference to a consumer. It breaks CI with NU1301. Publish to
  nuget.org first, then consume by `PackageReference`.
- One class, record, enum or interface per file, however small. Private nested types used only in that file may stay.
- Remove dead code when you find it, and verify with a build.
- If a browser bug cannot be explained by reading the code, instrument it and ask for a log by the second failed
  attempt. Check first that the instrumented path is the one really running.
- All three ShotUp UIs keep feature parity, but this package only touches the PWA (section 14).
- C# style, from the global CLAUDE.md: no `var`, a single `return` at the end, no `break` or `continue`, `foreach` only
  for full traversal, no `_` prefix on fields, descriptive names, about 100 lines per class. Comments only when
  strictly necessary. Library XML docs are required (`GenerateDocumentationFile`), as in the sibling libraries.
- In AmbesaSql, match its own conventions: read `Documentation/Pendientes/00 INDICE.md`, warn before launching its
  tests, make one commit per point. Its working tree has unrelated changes under `Company/`: leave them alone.

## 3. Decisions already made

Do not reopen these without new evidence.

| # | Decision | Why |
| --- | --- | --- |
| D1 | **Browser baseline is 2026**: Chrome and Edge, Firefox 111+, Chrome Android 109+, Safari 26+ | The user's call. These are the MDN compat data versions for `createWritable` |
| D2 | **One write path: `createWritable`. No worker and no sync access handles** | With D1 the only reason for ShotUp's worker is gone. It removes the worker, its queue, per-file locks, `.tmp` plus rename and the `.download` file, and it is what AmbesaSql already uses. It works on the page thread and inside a worker |
| D3 | **`[JSImport]` interop, WebAssembly only** | AmbesaSql must not depend on Blazor or `IJSRuntime`. OPFS lives in the browser, so server-side code has no use for it. This is the first BlazorBasics library that is not also Server |
| D4 | **No dependency on Blazor** despite the name. No `Microsoft.AspNetCore.Components.*` | Valid for Blazor, plain WebAssembly and Uno, as AmbesaSql documented |
| D5 | **Never swallow errors.** Errors pass through, typed, with the original as `InnerException`. The caller decides what "missing" means | The user's call. See section 9 |
| D6 | **Package is called `BlazorBasics.Opfs`**, repository `https://github.com/drualcman/BlazorBasics.Opfs` | Family naming. The user creates the GitHub repository; confirm before pushing |
| D7 | **The AmbesaSql engine does not change.** Only `AmbesaSql.Storage.Browser` does | `IAmbesaSqlStorage` stays. The engine stays single threaded |
| D8 | **Nothing changes on disk in either application** | No migration. Names, folders and note format stay, by configuration where needed |
| D9 | **Start from AmbesaSql's core, add ShotUp's operations** | AmbesaSql has the better errors, locks, moves and tests. ShotUp has the operations that keep bytes out of .NET |
| D10 | **Must be testable at three levels** | Both applications trust their data to it. See section 11 |
| D11 | **`net10.0` only** (a choice made in this brief, tell the user) | Both consumers are .NET 10, and .NET 8 and 9 leave support in November 2026. The other libraries target net8/9/10; widen only if the user asks |

## 4. Sources to read first

**ShotUpAlbums**

- `Src/ShotUpAlbums.UI.Blazor/wwwroot/js/opfs-store.js`, `opfs-writer.worker.js`: code to replace. Keep `list`, `listTree`, `copy`, `fetchTo` behaviour.
- `Src/ShotUpAlbums.UI.Blazor/Services/Opfs/*`: `OpfsFileSystem`, `OpfsReadStream`, `OpfsAvailability`, `OpfsBrowserFileAccess`, `OpfsStorageGuard`.
- `Src/ShotUpAlbums.UI.Blazor/Contexts/Opfs*.cs`: the four stores that stay in the application.
- `Src/ShotUpAlbums.UI.Blazor/wwwroot/js/storage-diagnostics.js`, `service-worker.published.js`.
- `Src/ShotUpAlbums.UI.Views/wwwroot/js/blob-uploader.js`, `Src/ShotUpAlbums.UI.Core/Interfaces/IBrowserFileReference.cs`.

**AmbesaSql** (`D:\JsonDatabase`)

- `Src/AmbesaSql.Storage.Browser/*`, above all `wwwroot/ambesasql-opfs.js`, `OpfsInterop.cs`, `OpfsFiles.cs`, `OpfsFolders.cs`, `OpfsReadStream.cs`, `WebLocks.cs`, `OpfsNames.cs`.
- `Documentation/07 Implementation/64 Almacenamiento_intercambiable.md`: decisions and the storage contract.
- `Tests/AmbesaSql.Storage.Browser.TestApp`, `Tests/AmbesaSql.Storage.Browser.Tests`, `Tests/AmbesaSql.Storage.ContractTests`: test infrastructure to generalise.
- `Documentation/Pendientes/20 Primera publicacion.md`, `21 Paquetes por plataforma.md`.

**Repository template**

- `D:\BlazorLibraries\BlazorBasics.SignaturePad`: layout, csproj metadata, `Injections.cs`, README style, `.gitattributes`, `.gitignore`, `LICENSE.md`.
- `D:\BlazorLibraries\BlazorBasics.Camera`: the same family with JavaScript in `wwwroot`.

## 5. Repository layout

```
BlazorBasics.Opfs/
├─ BlazorBasics.Opfs.slnx
├─ README.md  LICENSE.md  icon.svg  icon-512.png  .gitattributes  .gitignore
├─ Src/
│  ├─ BlazorBasics.Opfs/
│  │  ├─ BlazorBasics.Opfs.csproj
│  │  ├─ Injections.cs
│  │  ├─ GlobalUsings.cs
│  │  ├─ Abstractions/      IOpfsFileSystem, IOpfsWriter, IOpfsFileSnapshot, IOpfsLocks, IOpfsStorageInfo
│  │  ├─ Models/            OpfsPath, OpfsFileInfo, OpfsFolderContent, options and results
│  │  ├─ Exceptions/        OpfsException and its subclasses
│  │  ├─ Services/          implementations, the error translator
│  │  ├─ Streams/           read streams
│  │  ├─ Interop/           [JSImport] declarations and the module import
│  │  └─ wwwroot/opfs.js
│  └─ BlazorBasics.Opfs.Testing/   InMemoryOpfsFileSystem (second package)
├─ Tests/
│  ├─ BlazorBasics.Opfs.ContractTests/   the battery, written against IOpfsFileSystem
│  ├─ BlazorBasics.Opfs.Tests/           plain xUnit: paths, errors, the in-memory double
│  ├─ BlazorBasics.Opfs.TestApp/         WebAssembly app without Blazor that runs the battery
│  └─ BlazorBasics.Opfs.BrowserTests/    Playwright host
└─ Samples/
   └─ BlazorBasics.Opfs.Demo/            Blazor WebAssembly with a diagnostics page
```

## 6. Project file

Differences from `BlazorBasics.SignaturePad.csproj`, the rest of the metadata stays:

- `Sdk="Microsoft.NET.Sdk.Razor"`, needed to serve `wwwroot` as a static web asset, as in AmbesaSql.
- `TargetFramework` `net10.0` (D11).
- `AllowUnsafeBlocks` true: the `[JSImport]` generator needs it. Hand-written code uses no `unsafe`.
- No `PackageReference` to `Microsoft.AspNetCore.Components.Web`. `Injections.cs` needs only
  `Microsoft.Extensions.DependencyInjection.Abstractions`.
- `<SupportedPlatform Include="browser" />`.
- Do **not** put `[assembly: SupportedOSPlatform("browser")]` on the whole assembly as AmbesaSql does. The interfaces,
  models and `AddOpfs()` must load on the server during prerendering and in test hosts. Annotate only the browser
  implementations.
- `<Version>1.0.0</Version>`, `GeneratePackageOnBuild` in Release, description and release notes in the family style.

## 7. Public API

A sketch to refine, not a final contract. Every async method takes a `CancellationToken`, left out here for brevity.

```csharp
public readonly record struct OpfsPath
{
    public static OpfsPath Root { get; }
    public static OpfsPath Parse(string path);                               // "a/b/c", no encoding (ShotUp)
    public static OpfsPath FromSegments(IEnumerable<string> segments);       // encodes '/', '\' and '%' (AmbesaSql)
    public IReadOnlyList<string> Segments { get; }
    public OpfsPath Parent { get; }
    public string Name { get; }
    public OpfsPath Combine(string name);
    public static implicit operator OpfsPath(string path);
}

public interface IOpfsFileSystem
{
    Task<OpfsProbeResult> ProbeAsync();

    // Reading. "OrNull" returns null only for NotFound; every other failure still throws.
    Task<OpfsFileInfo?> GetInfoOrNullAsync(OpfsPath file);
    Task<IOpfsFileSnapshot?> OpenSnapshotOrNullAsync(OpfsPath file);
    Task<OpfsFolderContent> ListAsync(OpfsPath folder, string? textSuffix = null);
    Task<IReadOnlyList<OpfsFileInfo>> ListTreeAsync(OpfsPath folder, string? textSuffix = null);
    Task<string> ReadTextAsync(OpfsPath file);
    Task<byte[]> ReadBytesAsync(OpfsPath file);
    Stream OpenLazyRead(OpfsPath file, long length);

    // Writing. Every write is all or nothing: createWritable swaps the file in on close.
    Task WriteBytesAsync(OpfsPath file, ReadOnlyMemory<byte> content);
    Task WriteTextAsync(OpfsPath file, string content);
    Task WriteStreamAsync(OpfsPath file, Stream content);
    Task<IOpfsWriter> OpenWriterAsync(OpfsPath file, bool keepExistingData = false);
    Task AppendAsync(OpfsPath file, ReadOnlyMemory<byte> content);
    Task TruncateAsync(OpfsPath file, long length);

    // Moving data with no bytes in .NET.
    Task<long> CopyAsync(OpfsPath from, OpfsPath to);
    Task MoveFileAsync(OpfsPath from, OpfsPath to);
    Task<OpfsFetchResult> FetchToAsync(Uri url, OpfsPath file, OpfsFetchOptions? options = null);
    Task<string> CreateObjectUrlAsync(OpfsPath file);
    void RevokeObjectUrl(string url);
    Task DownloadAsync(OpfsPath file, string suggestedFileName);

    // Removing. Removing what is not there is success, as File.Delete does.
    Task DeleteAsync(OpfsPath fileOrFolder);
    Task ClearFolderAsync(OpfsPath folder);

    // Whole folders, recoverable after a closed tab.
    Task MoveFolderAsync(OpfsPath root, OpfsPath source, OpfsPath destination, OpfsFolderMoveOptions options);
    Task FinishPendingFolderMovesAsync(OpfsPath root, OpfsFolderMoveOptions options);
}

public interface IOpfsWriter : IAsyncDisposable
{
    Task WriteAsync(ReadOnlyMemory<byte> content);
    Task CompleteAsync();     // makes the content visible
    Task AbortAsync();        // leaves the file as it was
}                             // DisposeAsync without CompleteAsync aborts

public interface IOpfsFileSnapshot : IDisposable
{
    long Length { get; }
    Task<byte[]> ReadRangeAsync(long start, long length);
    Stream OpenRead();        // takes ownership of the snapshot
}

public interface IOpfsLocks
{
    Task<IDisposable?> TryAcquireAsync(string name, OpfsLockMode mode);   // Web Locks with ifAvailable
}

public interface IOpfsStorageInfo
{
    Task<OpfsStorageEstimate?> EstimateAsync();
    Task<bool> IsPersistedAsync();
    Task<bool> RequestPersistenceAsync();
}
```

Notes:

- `AddOpfs()` lives in `Microsoft.Extensions.DependencyInjection.Injections` and registers singletons with `TryAdd`,
  like SignaturePad. A storage class that needs no DI can create the implementation directly: AmbesaSql's
  `new BrowserStorage()` has no container.
- `OpfsFolderMoveOptions` carries the notes folder name and the lock prefix (D8), with neutral defaults.
- `OpfsFileInfo` has `Name`, `Length`, `ModifiedUtc`, `Text`. `Text` is filled for names ending in `textSuffix`, so
  listing ShotUp's queue is one call rather than one per photo.
- Leave the lazy read stream unsealed so ShotUp can add its `IBrowserFileReference` marker in a small subclass. The
  stream reports its path. It is async only: synchronous `Read` throws `NotSupportedException`.
- `OpfsFetchResult` has `Bytes` and `TooLarge`. Exceeding the limit is a result, not an exception: it is an expected
  business outcome. Defaults are `mode: cors`, `cache: no-store`, `credentials: omit`, and never Cache Storage, where
  an opaque response counts against quota with megabytes of padding.

## 8. JavaScript design

One file, `wwwroot/opfs.js`, imported with
`JSHost.ImportAsync("BlazorBasics.Opfs", "../_content/BlazorBasics.Opfs/opfs.js")`, the form AmbesaSql already uses.

- **Paths are arrays of segments** on the JS side. All string parsing stays in C#.
- **State lives on `globalThis.__blazorBasicsOpfs`**, not in the module. Each import URL, with or without `?v=`, is a
  different module instance, and the state must be shared. That includes the lock release tracking, which is module
  state in AmbesaSql today.
- **No `document` or `window`** outside `downloadFile` and the guarded bfcache hooks, so it works inside a worker.
- **`list` and `read` results cross as a JSON string**, deserialised with a source-generated `JsonSerializerContext`,
  so trimmed apps need no reflection. Large byte reads cross as `Uint8Array`.
- **Bytes stay in JS**: `writable.write(blob)` or `blob.stream().pipeTo(writable)` for `File` and `Blob`,
  `response.body.pipeTo(writable)` with a counting `TransformStream` for `fetchTo`. `pipeTo` aborts the destination on
  failure, so a cut download leaves nothing.
- **`writeBlob`, `getFile` and `requireFile` are a documented public JS API.** `File` objects cannot cross between
  `JSObject` and `IJSObjectReference`, so ShotUp bridges them in JavaScript terms (section 14).
- **Cancellation**: each cancellable operation gets an id tied to an `AbortController`; `CancellationToken.Register`
  calls `abortOperation(id)`. Today ShotUp's token stops .NET waiting but the `fetch` keeps running.
- **Abort on failure, always**: a writable neither closed nor aborted keeps its lock on the file, so every path must
  end in `close` or `abort`. If `abort` itself throws, the original error is the one reported.

### Moving a file without losing the destination

```
try handle.move(destinationFolder, name)
on failure: if the source no longer exists, rethrow
            copy over the destination through createWritable (atomic)
            remove the source
```

Never remove the destination first. ShotUp's `rename` does, and a tab dying there leaves no file under the real name.
Do not swallow the failed `move`: if the copy also fails, report it. Section 12 lists what must be measured.

## 9. Error model

JS functions never catch to hide a failure. `JSException` carries only a message, so every exported function rethrows
as `new Error(`${error.name}: ${error.message}`)`, as Ahtola does. A C# translator maps the name, and the original
stays as `InnerException`.

| Browser error | Exception |
| --- | --- |
| `NotFoundError` | `OpfsNotFoundException` |
| `TypeMismatchError` | `OpfsTypeMismatchException` |
| `QuotaExceededError` | `OpfsQuotaExceededException` |
| `NoModificationAllowedError` | `OpfsFileBusyException` |
| `SecurityError`, `NotAllowedError` | `OpfsAccessDeniedException` |
| Fetch failure or HTTP error status | `OpfsFetchException`, with the status code |
| Anything else | `OpfsException` |

`OpfsException` derives from `IOException` and keeps the browser error name in `BrowserErrorName`. A library call
outside a browser throws `OpfsNotSupportedException` (section 10).

Three deliberate exceptions to "missing throws":

1. `...OrNull` methods, because in AmbesaSql "missing" is a normal, frequent answer and an exception per query is a
   real cost. They hide only `NotFound`. The caller opts in by choosing the method.
2. `DeleteAsync`: the requested end state already holds, as with `File.Delete`.
3. `ProbeAsync`: its job is to answer. It returns `OpfsProbeResult(IsAvailable, Reason, Failure)` and keeps the failure.

`ListAsync` on a missing folder throws `OpfsNotFoundException`. Both applications return an empty list today, so each
adapter catches it.

## 10. Hosting: WebAssembly only

OPFS is in the browser, and `[JSImport]` works only with .NET running there.

| Host | Works? |
| --- | --- |
| Blazor WebAssembly, plain WebAssembly, Uno, a .NET web worker | Yes |
| Blazor Web App, component with `InteractiveWebAssembly` | Yes, in the interactive phase |
| Same, during prerendering | Runs on the server: not available |
| `InteractiveAuto` | **Not safe**: the first visit runs on the server until the runtime is downloaded |
| `InteractiveServer`, static SSR, MAUI Blazor Hybrid | No |

Rules this creates:

- `AddOpfs()` must succeed on the server, because a client project's `Program` and DI also run there for prerendering.
- Every browser operation checks `OperatingSystem.IsBrowser()` first and throws `OpfsNotSupportedException` with a
  message naming the likely cause (prerendering, `InteractiveAuto`, Server) and the fix.
- The README documents: use `InteractiveWebAssembly`, call from `OnAfterRenderAsync`, not `OnInitializedAsync` while
  prerendering.
- Add a sample or test for a `Blazor Web App` client component with prerendering on.

## 11. Testing

**Level 1, plain xUnit, no browser** (`BlazorBasics.Opfs.Tests`). `OpfsPath`, name encoding, error translation,
`OpfsNotSupportedException` outside a browser, and the in-memory double.

**Level 2, contract battery in real browsers.** Generalise AmbesaSql's setup: an `Microsoft.NET.Sdk.WebAssembly` app
with no Blazor runs the battery in the page, a test host publishes it after build, serves it on `127.0.0.1` (a secure
context) and opens it with Playwright. Run it in **Chromium, Firefox and WebKit**, not only Edge. The battery must
cover at least:

- Every operation in section 7, including the `...OrNull` behaviour and each typed exception.
- A failing stream or an aborted writer leaves the previous content untouched.
- Replacing a destination by `MoveFileAsync`: both when `move()` overwrites and through the copy route.
- A folder move interrupted halfway is finished by the next instance. AmbesaSql has this test.
- Two instances on one folder share locks, and a released lock can be taken again at once.
- Two pages at once. Closing a page mid-write leaves the previous content and no visible leftovers.
- Large files, as ManagedCode does at 128 MiB, proving memory does not grow with file size.
- `fetchTo`: limit declared by `content-length`, limit exceeded while streaming, HTTP error, abort.
- Names with `/`, `\`, `%`, and the `%2F` literal, returning as written.
- `IOpfsWriter` disposed without completing.

Playwright's WebKit on Windows is not real Safari, so Safari on iOS stays a manual check. List it in the README.

**Level 3, for consumers** (`BlazorBasics.Opfs.Testing`). `InMemoryOpfsFileSystem` implements the abstractions. It runs
the same contract battery, so it cannot drift from real behaviour. It lets ShotUp test its four stores with plain
`dotnet test`; today they have no tests.

## 12. Verify before building on it

Run this spike first and report the numbers to the user. If a result contradicts D2, stop and discuss it.

1. **Recording**: a 60 second video through one open `IOpfsWriter`, on a mid-range Android phone.
2. **Quota**: does exhaustion surface at `write` or at `close`? Does an abort leave anything behind?
3. **Space**: how much extra does the swap copy use during a write? `keepExistingData` copies the whole file (BlazeDb
   documents this; not measured by us). With a queue video this may double usage for a moment.
4. **`move()`**: it is non-standard, though present in all three engines. Does it overwrite in Chromium, Firefox and
   WebKit? ShotUp's code says at least one refuses.
5. **Reading during a write**: does a `File` taken before a write become unreadable after the swap, on each engine?
6. **`AmbesaSql` read after append**: its contract requires reads to see an append once the task ends.
7. **Publishing**: does a published PWA load `_content/BlazorBasics.Opfs/opfs.js` offline through the service worker?
8. **Abandoned swap files**: what happens to them after a crash, and do they count against quota?

**Performance baseline, required.** AmbesaSql's rule is that anything touching reads or writes is measured before and
after. Measure the browser baseline **before** touching `AmbesaSql.Storage.Browser`: `INSERT`, a read, and appending to
an index `.log`. BenchmarkDotNet does not run in the browser, so use a timing page in the test app.

Expected result for AmbesaSql: about the same speed. Same browser API, same `[JSImport]` crossing. Two things can slow
it and both are under our control: exceptions on hot paths (use `...OrNull`) and extra crossings per operation (keep
batch reads through one snapshot). Possible small gain: avoiding `content.ToArray()` in `WriteBytesAsync` if
`ArraySegment<byte>` can cross as a memory view during an awaited call. Check that before relying on it.

## 13. Order of work

Each step ends with the tests green and the user reviewing before any commit.

1. Spike and baseline (section 12). Report.
2. Repository scaffold from section 5, with `git init` on `master`. Confirm with the user before pushing.
3. Paths, exceptions, translator, level 1 tests.
4. `opfs.js` and interop for reads, writes, delete. Level 2 infrastructure with the first battery.
5. Writers, streams, copy, move, fetch, locks, folder moves, storage info. Rest of the battery in all three engines.
6. `BlazorBasics.Opfs.Testing`, running the battery.
7. README and demo. Publish `1.0.0` to nuget.org. The user publishes or authorises it.
8. Migrate AmbesaSql (section 14) and compare against the baseline.
9. Migrate ShotUp (section 14), with manual checks on Chrome Android, Firefox private mode and Safari iOS 26.

AmbesaSql goes first because its battery is the safety net. ShotUp's OPFS code has no automated tests, and it was
only finished on 2026-10-31 with its manual browser checks still pending.

## 14. Migrating the consumers

### AmbesaSql

Only `Src/AmbesaSql.Storage.Browser` changes (D7).

- Remove: `wwwroot/ambesasql-opfs.js`, `OpfsInterop`, `OpfsModule`, `OpfsNames`, `OpfsRoot`, `OpfsFiles`, `OpfsFolders`,
  `OpfsReadStream`, `WebLocks`. Keep the lock adapter only if a type must map `AmbesaSqlLockMode`.
- Keep and adapt: `BrowserStorage`, `BrowserAppendFile`, `BrowserBackups`, `BrowserStorageRegistration`.
- Keep `AllowUnsafeBlocks` only if the project still has its own `[JSImport]`.

Behaviour the adapter must preserve, because tests or data depend on it:

- Folder `.ambesasql-moves` hidden from the root listing, move lock prefix `AmbesaSql-moves:`, lock prefix `AmbesaSql:`.
  A closed tab may have left a note there.
- Move note format `{"source":[...],"destination":[...]}` with full segments. A test writes this literal.
- Reads of a missing file return null. Listing a missing folder returns empty. Names come back decoded in ordinal order.
- `TypeMismatchError` counts as missing, as today.
- Deleting folder path `[]` empties the root (`ClearFolderAsync`).
- `ReadFileRangesAsync` uses one snapshot and returns null when a range is out of bounds.
- A failed stream leaves the file as it was.

Worth adding: call `RequestPersistenceAsync` (nothing does today, so a browser under storage pressure can evict the
database) and consider revalidating locks after a return from the back/forward cache, as BlazeDb does.

Update `Documentation/07 Implementation/64 Almacenamiento_intercambiable.md`, and note the dependency in pendientes 20
and 21, because the `AmbesaSql.Browser` package will pull this one.

### ShotUpAlbums

Only `ShotUpAlbums.UI.Blazor` gets the `PackageReference`. `UI.Core`, `UI.Views`, MAUI and WPF must not reference it:
they reach OPFS through `IBrowserFileAccess`.

- Remove: `wwwroot/js/opfs-store.js`, `opfs-writer.worker.js`, `OpfsFileSystem`, `OpfsReadStream`, `OpfsListing`,
  `OpfsFileEntry`, `OpfsDownload`.
- Stores change little: `OpfsPendingUploadStore`, `OpfsPendingFolder`, `OpfsVideoRecordingStore`,
  `OpfsOfflineMediaStore`. Keep a small application wrapper that turns `OpfsNotFoundException` into today's behaviour
  (empty listing, `false` from a move).
- `OpfsAvailability` keeps deciding at boot, and keeps the 4000 ms limit covering the module import too, because this
  runs before first paint. The probe must now check `createWritable`. Safari before 26 falls to the existing IndexedDB
  stores and loses the offline cache: accepted by D1.
- `OpfsStorageGuard`: use `EstimateAsync` instead of `shotupDiagnostics.storage`.
- Recording: keep one `IOpfsWriter` per recording id across `AppendAsync` calls, never one append per chunk, which
  would rewrite the whole file each time. `BeginAsync` still creates the empty `.part`, so the crash notice keeps
  counting. Losing an unfinished recording changes nothing: `.part` files are discarded at start already.
- The `.tmp` staging file of the queue is application logic and stays, with its sweep in `IsStray`.
- `FetchToAsync` failures: the store catches them, because one piece that cannot be downloaded must never stop the sweep.
- `window.__shotupSafeBoot` moves out of the package: ShotUp checks it before calling.
- `OpfsBrowserFileAccess` keeps the `blob-uploader.js` bridge in `IJSRuntime` terms: it imports
  `_content/BlazorBasics.Opfs/opfs.js` itself and passes the `File` from `getFile` to `registerFile`, and from
  `fileOf` to `writeBlob`. **Trap**: the `files` map in `blob-uploader.js` is module state, so that module must always
  be imported through the same URL, with `?v=`, using `ImportJavascript`.
- Add a small `OpfsBrowserFileStream` subclass of the lazy stream implementing `IBrowserFileReference`.
- The service worker needs no change: `/\.js$/` in `criticalAssets` covers `_content`. Confirm with spike item 7.
- Run all three test projects: `ShotUpAlbums.Application.Tests`, `ShotUpAlbums.UI.Core.Tests`, `ShotUpAlbums.UI.Views.Tests`.

## 15. Prior art

Reviewed on 2026-10-31. Nothing here is adaptable as a base. Take ideas, not code.

| Package | What it is | Why not | Idea worth taking |
| --- | --- | --- | --- |
| `KristofferStrube.Blazor.FileSystem` | 1:1 wrapper of the File System API, about 95,000 downloads, supports Blazor Server | `IJSRuntime`, handle by handle, logic and calls in .NET | Closest in scope. Our difference is the high-level, path-based API, no Blazor, and bytes staying in JS |
| `BlazeDb.Browser` | Database storage on `createWritable` | Tied to its engine, version 0.1.0 | Always abort on failure, strict `NotFoundError`, write capability check, persistence request, revalidating locks after bfcache |
| `ManagedCode.Storage.Browser` | Storage provider | Requires IndexedDB and `Microsoft.JSInterop`, swallows errors | Begin/append/complete/abort writes, Playwright large-file and multi-tab tests, failing clearly without OPFS |
| `Devolutions.Ahtola.Data.Sqlite.Browser` | SQLite in a worker | Needs cross-origin isolation and `SharedArrayBuffer` | `name: message` errors, cancelling by operation id |
| `SqliteWasmBlazor`, `Bit.Bsqlite*`, `Varve.Store.Browser` | Engines on sync access handles | Different need, see non-goals | None |
| `BLite.Wasm`, `Blazor.StorageManagement*` | Engine pages, Storage API bindings | Not a file API | None |

ManagedCode, BlazeDb and Ahtola are the ones whose code was read. The others were judged from their description,
README or a quick look at contents. Searches covered the obvious names, not every possible one.

## 16. Open questions for the user

1. Is `net10.0` only acceptable (D11), or should the family's net8/net9 targets stay?
2. Is `BlazorBasics.Opfs.Testing` a separate package, or should it live in the test folder for now?
3. An icon for the package.
