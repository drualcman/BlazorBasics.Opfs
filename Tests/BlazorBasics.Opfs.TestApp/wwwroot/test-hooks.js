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
    },
    inWorker() {
        return typeof document === 'undefined';
    },
    queryValue(name) {
        return new URLSearchParams(location.search).get(name);
    }
};
