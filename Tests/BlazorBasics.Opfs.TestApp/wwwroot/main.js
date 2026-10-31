import { dotnet } from './_framework/dotnet.js';

globalThis.opfsTestHooks = {
    disableMove() {
        Object.defineProperty(FileSystemFileHandle.prototype, 'move', { value: undefined, configurable: true });
    },
    enableMove() {
        delete FileSystemFileHandle.prototype.move;
    },
    async fetchText(url) {
        return await (await fetch(url)).text();
    },
    heapBytes() {
        return globalThis.opfsRuntime.Module.HEAPU8.buffer.byteLength;
    }
};

try {
    const runtime = await dotnet.create();
    globalThis.opfsRuntime = runtime;
    await runtime.runMain();
    document.getElementById('results').textContent = globalThis.opfsContractResults;
} catch (error) {
    globalThis.opfsFailed = `${error}`;
    throw error;
}
