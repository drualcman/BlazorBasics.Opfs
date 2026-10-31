using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>The writes of the storage tests, chunk by chunk through the browser API, so they can tell where a failure surfaced.</summary>
internal static class QuotaPageScripts
{
    public const string MODULE = "await import('/_content/BlazorBasics.Opfs/opfs.js')";

    private const string HELPERS = """
        const handleOf = async (folder, name) => {
            const root = await navigator.storage.getDirectory();
            const parent = await root.getDirectoryHandle(folder, { create: true });
            return await parent.getFileHandle(name, { create: true });
        };
        const write = async (folder, name, mebibytes, keepExistingData) => {
            const handle = await handleOf(folder, name);
            let writable;
            try {
                writable = await handle.createWritable({ keepExistingData });
            } catch (error) {
                return `createWritable ${error.name}`;
            }
            if (keepExistingData) {
                await writable.seek((await handle.getFile()).size);
            }
            for (let written = 0; written < mebibytes; written++) {
                try {
                    await writable.write(new Uint8Array(1024 * 1024));
                } catch (error) {
                    await writable.abort();
                    return `write ${error.name}`;
                }
            }
            try {
                await writable.close();
            } catch (error) {
                return `close ${error.name}`;
            }
            return 'written';
        };
        """;

    public static Task<string> WriteAsync(IPage page, string folder, string name, int mebibytes, bool keepExistingData = false) =>
        page.EvaluateAsync<string>($"async ([folder, name, mebibytes, keep]) => {{ {HELPERS} return await write(folder, name, mebibytes, keep); }}",
            new object[] { folder, name, mebibytes, keepExistingData });

    public static Task WriteTextAsync(IPage page, string folder, string name, string text) =>
        page.EvaluateAsync($"async ([folder, name, text]) => {{ const opfs = {MODULE}; await opfs.writeText([folder, name], text); }}",
            new object[] { folder, name, text });

    public static Task<string> ReadTextAsync(IPage page, string folder, string name) =>
        page.EvaluateAsync<string>($"async ([folder, name]) => {{ const opfs = {MODULE}; return await opfs.readText([folder, name]); }}",
            new object[] { folder, name });

    public static Task<string[]> NamesAsync(IPage page, string folder) =>
        page.EvaluateAsync<string[]>("""
            async folder => {
                const root = await navigator.storage.getDirectory();
                const names = [];
                for await (const name of (await root.getDirectoryHandle(folder)).keys()) {
                    names.push(name);
                }
                return names.sort();
            }
            """, folder);

    public static Task<string> ListedNamesAsync(IPage page, string folder) =>
        page.EvaluateAsync<string>($"async folder => {{ const opfs = {MODULE}; return await opfs.listFolder([folder], null); }}", folder);

    public static Task RemoveFileAsync(IPage page, string folder, string name) =>
        page.EvaluateAsync($"async ([folder, name]) => {{ const opfs = {MODULE}; await opfs.remove([folder, name]); }}", new object[] { folder, name });

    public static Task<string> WriteBlobAsync(IPage page, string folder, string name, int mebibytes) =>
        CatchingAsync(page, "await opfs.writeBlob([folder, name], new Blob(parts));", folder, name, mebibytes);

    public static Task<string> WriteThroughWriterAsync(IPage page, string folder, string name, int mebibytes) =>
        CatchingAsync(page, """
            const writer = await opfs.openWriter([folder, name], false);
            for (const part of parts) {
                await opfs.writeToWriter(writer, { slice: () => part, dispose: () => { } });
            }
            await opfs.completeWriter(writer);
            """, folder, name, mebibytes);

    public static Task<string> AppendAsync(IPage page, string folder, string name, int mebibytes) =>
        CatchingAsync(page, """
            const all = new Uint8Array(await new Blob(parts).arrayBuffer());
            await opfs.appendBytes([folder, name], { slice: () => all, dispose: () => { } });
            """, folder, name, mebibytes);

    public static Task RemoveAsync(IPage page, string folder) =>
        page.EvaluateAsync("async folder => { await (await navigator.storage.getDirectory()).removeEntry(folder, { recursive: true }); }", folder);

    public static Task<double> UsageAsync(IPage page) =>
        page.EvaluateAsync<double>("async () => (await navigator.storage.estimate()).usage");

    public static Task<double> QuotaAsync(IPage page) =>
        page.EvaluateAsync<double>("async () => (await navigator.storage.estimate()).quota");

    private static Task<string> CatchingAsync(IPage page, string action, string folder, string name, int mebibytes) =>
        page.EvaluateAsync<string>($$"""
            async ([folder, name, mebibytes]) => {
                const opfs = {{MODULE}};
                const parts = Array.from({ length: mebibytes }, () => new Uint8Array(1024 * 1024));
                try {
                    {{action}}
                    return 'no failure';
                } catch (error) {
                    return error.message;
                }
            }
            """, new object[] { folder, name, mebibytes });
}
