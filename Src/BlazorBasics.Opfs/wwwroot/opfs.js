// The file system of BlazorBasics.Opfs. Paths are arrays of stored segments. Every export rethrows failures as
// "name: message", because a JSException carries only a message. writeBlob, getFile and requireFile are public JavaScript API.
// State lives on globalThis because each import URL is a different instance of this module.

const state = globalThis.__blazorBasicsOpfs ??= { operations: new Map(), releasingLocks: new Map() };

function described(error) {
    const name = error?.name ?? 'AbortError';
    const message = error?.message ?? 'The stream was already closed or aborted.';
    return new Error(`${name}: ${message}`);
}

function guarded(action) {
    return async (...args) => {
        try {
            return await action(...args);
        } catch (error) {
            throw described(error);
        }
    };
}

function fetchFailure(status, text) {
    const error = new Error(`${status}|${text}`);
    error.name = 'OpfsFetchError';
    return error;
}

async function orNull(action) {
    try {
        return await action();
    } catch (error) {
        if (error?.name !== 'NotFoundError') {
            throw error;
        }
        return null;
    }
}

async function folder(names, create = false) {
    let current = await navigator.storage.getDirectory();
    for (const name of names) {
        current = await current.getDirectoryHandle(name, { create });
    }
    return current;
}

async function fileHandle(names) {
    return await (await folder(names.slice(0, -1))).getFileHandle(names.at(-1));
}

async function fileOf(names) {
    return await (await fileHandle(names)).getFile();
}

async function entriesOf(target) {
    const entries = [];
    for await (const entry of target.entries()) {
        entries.push(entry);
    }
    return entries;
}

function beginOperation(operationId) {
    const controller = new AbortController();
    state.operations.set(operationId, controller);
    return controller;
}

export function abortOperation(operationId) {
    state.operations.get(operationId)?.abort();
}

// ---- Probe ----

export const probe = guarded(async () => {
    let reason = '';
    if (typeof navigator.storage?.getDirectory !== 'function') {
        reason = 'This browser has no Origin Private File System.';
    } else if (typeof FileSystemFileHandle === 'undefined' || typeof FileSystemFileHandle.prototype.createWritable !== 'function') {
        reason = 'This browser cannot write with createWritable.';
    } else {
        const root = await navigator.storage.getDirectory();
        const name = `.opfs-probe-${crypto.randomUUID()}`;
        const handle = await root.getFileHandle(name, { create: true });
        try {
            const writable = await handle.createWritable();
            await writable.write(new Uint8Array([1, 2, 3]));
            await writable.close();
            if ((await handle.getFile()).size !== 3) {
                reason = 'A written file did not keep its content.';
            }
        } finally {
            await root.removeEntry(name);
        }
    }
    return reason;
});

// ---- Reading ----

function record(name, file, text) {
    return { name, length: file.size, modified: file.lastModified, text };
}

async function describeFile(name, handle, textSuffix) {
    return await orNull(async () => {
        const file = await handle.getFile();
        const text = textSuffix && name.endsWith(textSuffix) ? await file.text() : null;
        return record(name, file, text);
    });
}

export const fileInfoOrNull = guarded(async names => {
    const handle = await orNull(() => fileHandle(names));
    const info = handle === null ? null : await describeFile(names.at(-1), handle, null);
    return info === null ? null : JSON.stringify(info);
});

export const snapshotOrNull = guarded(async names => {
    return await orNull(() => fileOf(names));
});

export const listFolder = guarded(async (names, textSuffix) => {
    const content = { files: [], folders: [] };
    for (const [name, handle] of await entriesOf(await folder(names))) {
        if (handle.kind === 'directory') {
            content.folders.push(name);
        } else {
            const info = await describeFile(name, handle, textSuffix);
            if (info !== null) {
                content.files.push(info);
            }
        }
    }
    return JSON.stringify(content);
});

async function walk(target, prefix, textSuffix, files) {
    for (const [name, handle] of await entriesOf(target)) {
        const relative = prefix ? `${prefix}/${name}` : name;
        if (handle.kind === 'directory') {
            await walk(handle, relative, textSuffix, files);
        } else {
            const info = await describeFile(relative, handle, textSuffix);
            if (info !== null) {
                files.push(info);
            }
        }
    }
}

export const listTree = guarded(async (names, textSuffix) => {
    const files = [];
    await walk(await folder(names), '', textSuffix, files);
    return JSON.stringify(files);
});

export const readText = guarded(async names => await (await fileOf(names)).text());

export const readBytes = guarded(async names => new Uint8Array(await (await fileOf(names)).arrayBuffer()));

export const readRange = guarded(async (names, start, end) => {
    return new Uint8Array(await (await fileOf(names)).slice(start, end).arrayBuffer());
});

export const readSnapshotRange = guarded(async (file, start, end) => {
    return new Uint8Array(await file.slice(start, end).arrayBuffer());
});

export function lengthOfSnapshot(file) {
    return file.size;
}

export function bytesOf(array) {
    return array;
}

export const getFile = guarded(async names => await orNull(() => fileOf(names)));

export const requireFile = guarded(async names => await fileOf(names));

// ---- Writing ----

async function forget(target) {
    if (target.created) {
        try {
            await target.parent.removeEntry(target.name);
        } catch (error) {
            if (error?.name !== 'NoModificationAllowedError' && error?.name !== 'NotFoundError') {
                throw error;
            }
        }
    }
}

async function openTarget(names, keepExistingData) {
    const parent = await folder(names.slice(0, -1), true);
    const name = names.at(-1);
    const existing = await orNull(() => parent.getFileHandle(name));
    const handle = existing ?? await parent.getFileHandle(name, { create: true });
    const target = { parent, name, handle, writable: null, created: existing === null };
    try {
        target.writable = await handle.createWritable({ keepExistingData });
    } catch (error) {
        await forget(target);
        throw error;
    }
    return target;
}

async function abandon(target) {
    try {
        await target.writable.abort();
    } catch {
        // The failure that led here is the one to report.
    }
    try {
        await forget(target);
    } catch {
        // The failure that led here is the one to report.
    }
}

async function writeWhole(names, keepExistingData, write) {
    const target = await openTarget(names, keepExistingData);
    try {
        const result = await write(target.writable, target.handle);
        await target.writable.close();
        return result;
    } catch (error) {
        await abandon(target);
        throw error;
    }
}

async function pipeBlob(blob, writable, signal) {
    await blob.stream().pipeTo(writable, { signal, preventClose: true, preventAbort: true });
    return blob.size;
}

function copyOf(view) {
    const bytes = view.slice();
    view.dispose();
    return bytes;
}

export const writeBytes = guarded(async (names, view) => {
    const bytes = copyOf(view);
    await writeWhole(names, false, writable => writable.write(bytes));
});

export const writeText = guarded(async (names, text) => {
    await writeWhole(names, false, writable => writable.write(text));
});

export const writeBlob = guarded(async (names, blob) => {
    return await writeWhole(names, false, writable => pipeBlob(blob, writable));
});

export const appendBytes = guarded(async (names, view) => {
    const bytes = copyOf(view);
    await writeWhole(names, true, async (writable, handle) => {
        await writable.seek((await handle.getFile()).size);
        await writable.write(bytes);
    });
});

export const truncate = guarded(async (names, length) => {
    const handle = await fileHandle(names);
    if ((await handle.getFile()).size > length) {
        await writeWhole(names, true, writable => writable.truncate(length));
    }
});

export const openWriter = guarded(async (names, keepExistingData) => {
    const target = await openTarget(names, keepExistingData);
    if (keepExistingData) {
        try {
            await target.writable.seek((await target.handle.getFile()).size);
        } catch (error) {
            await abandon(target);
            throw error;
        }
    }
    return target;
});

export const writeToWriter = guarded(async (target, view) => {
    const bytes = copyOf(view);
    await target.writable.write(bytes);
});

export const completeWriter = guarded(async target => {
    await target.writable.close();
});

export const abortWriter = guarded(async target => {
    await target.writable.abort();
    await forget(target);
});

export const abandonWriter = guarded(abandon);

// ---- Moving data ----

export const copy = guarded(async (operationId, from, to) => {
    const controller = beginOperation(operationId);
    try {
        const file = await fileOf(from);
        return await writeWhole(to, false, writable => pipeBlob(file, writable, controller.signal));
    } finally {
        state.operations.delete(operationId);
    }
});

async function copyOver(handle, destinationParent, destinationName, moveFailure) {
    const destinationHandle = await destinationParent.getFileHandle(destinationName, { create: true });
    const writable = await destinationHandle.createWritable();
    try {
        await pipeBlob(await handle.getFile(), writable);
        await writable.close();
    } catch (error) {
        try {
            await writable.abort();
        } catch {
            // The failure that led here is the one to report.
        }
        const reported = moveFailure === null ? '' : ` (moving it had failed with ${moveFailure.name}: ${moveFailure.message})`;
        throw { name: error?.name, message: `${error?.message}${reported}` };
    }
}

async function exists(parent, name) {
    return await orNull(() => parent.getFileHandle(name)) !== null;
}

async function moveEntry(sourceParent, sourceName, handle, destinationParent, destinationName) {
    let moved = false;
    let moveFailure = null;
    if (typeof handle.move === 'function') {
        try {
            await handle.move(destinationParent, destinationName);
            moved = true;
        } catch (error) {
            if (error?.name === 'NoModificationAllowedError' || !await exists(sourceParent, sourceName)) {
                throw error;
            }
            moveFailure = error;
        }
    }
    if (!moved) {
        await copyOver(handle, destinationParent, destinationName, moveFailure);
        await sourceParent.removeEntry(sourceName);
    }
}

export const moveFile = guarded(async (from, to) => {
    const sourceParent = await folder(from.slice(0, -1));
    const handle = await sourceParent.getFileHandle(from.at(-1));
    if (from.join('/') !== to.join('/')) {
        const destinationParent = await folder(to.slice(0, -1), true);
        await moveEntry(sourceParent, from.at(-1), handle, destinationParent, to.at(-1));
    }
});

function counter(maxBytes, outcome) {
    return new TransformStream({
        transform(chunk, controller) {
            outcome.bytes += chunk.byteLength;
            if (maxBytes > 0 && outcome.bytes > maxBytes) {
                outcome.tooLarge = true;
                controller.error(new DOMException('The download went over its limit.', 'AbortError'));
            } else {
                controller.enqueue(chunk);
            }
        }
    });
}

async function pipeResponse(response, names, maxBytes, signal) {
    const outcome = { bytes: 0, tooLarge: false };
    try {
        await writeWhole(names, false, async writable => {
            if (response.body !== null) {
                await response.body.pipeThrough(counter(maxBytes, outcome), { signal })
                    .pipeTo(writable, { signal, preventClose: true, preventAbort: true });
            }
        });
    } catch (error) {
        if (!outcome.tooLarge) {
            throw error?.name === 'TypeError' ? fetchFailure(0, error.message) : error;
        }
    }
    return outcome;
}

export const fetchTo = guarded(async (operationId, url, names, maxBytes, mode, cache, credentials) => {
    const controller = beginOperation(operationId);
    try {
        let response;
        try {
            response = await fetch(url, { mode, cache, credentials, signal: controller.signal });
        } catch (error) {
            throw error?.name === 'TypeError' ? fetchFailure(0, error.message) : error;
        }
        if (!response.ok) {
            throw fetchFailure(response.status, response.statusText);
        }
        let outcome;
        if (maxBytes > 0 && Number(response.headers.get('content-length')) > maxBytes) {
            controller.abort();
            outcome = { bytes: 0, tooLarge: true };
        } else {
            outcome = await pipeResponse(response, names, maxBytes, controller.signal);
        }
        return JSON.stringify({ bytes: outcome.tooLarge ? 0 : outcome.bytes, tooLarge: outcome.tooLarge });
    } finally {
        state.operations.delete(operationId);
    }
});

export const objectUrl = guarded(async names => URL.createObjectURL(await fileOf(names)));

export function revokeObjectUrl(url) {
    URL.revokeObjectURL(url);
}

export const downloadFile = guarded(async (names, suggestedFileName) => {
    const address = URL.createObjectURL(await fileOf(names));
    const link = document.createElement('a');
    link.href = address;
    link.download = suggestedFileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(address), 60000);
});

// ---- Removing ----

async function clear(target) {
    for (const [name] of await entriesOf(target)) {
        await target.removeEntry(name, { recursive: true });
    }
}

export const clearFolder = guarded(async names => {
    const target = await orNull(() => folder(names));
    if (target !== null) {
        await clear(target);
    }
});

export const remove = guarded(async names => {
    if (names.length === 0) {
        await clear(await folder([]));
    } else {
        const parent = await orNull(() => folder(names.slice(0, -1)));
        if (parent !== null) {
            await orNull(() => parent.removeEntry(names.at(-1), { recursive: true }));
        }
    }
});

// ---- Whole folders ----

async function saveNote(target, name, content) {
    const handle = await target.getFileHandle(name, { create: true });
    const writable = await handle.createWritable();
    await writable.write(content);
    await writable.close();
}

async function moveContent(sourceNames, destinationNames) {
    const source = await orNull(() => folder(sourceNames));
    if (source !== null) {
        const destination = await folder(destinationNames, true);
        for (const [name, handle] of await entriesOf(source)) {
            if (handle.kind === 'file') {
                await moveEntry(source, name, handle, destination, name);
            } else {
                await moveContent(sourceNames.concat([name]), destinationNames.concat([name]));
            }
        }
        await (await folder(sourceNames.slice(0, -1))).removeEntry(sourceNames.at(-1), { recursive: true });
    }
}

async function finishMoves(rootNames, notesFolderName) {
    const notes = await orNull(() => folder(rootNames.concat([notesFolderName])));
    if (notes !== null) {
        for (const [name, handle] of await entriesOf(notes)) {
            const move = JSON.parse(await (await handle.getFile()).text());
            await moveContent(move.source, move.destination);
            await notes.removeEntry(name);
        }
    }
}

export const finishPendingMoves = guarded((rootNames, notesFolderName, lockPrefix) => {
    return navigator.locks.request(lockPrefix + rootNames.join('/'), () => finishMoves(rootNames, notesFolderName));
});

export const moveFolder = guarded((rootNames, sourceNames, destinationNames, notesFolderName, lockPrefix) => {
    return navigator.locks.request(lockPrefix + rootNames.join('/'), async () => {
        await finishMoves(rootNames, notesFolderName);
        const source = rootNames.concat(sourceNames);
        const destination = rootNames.concat(destinationNames);
        if (await orNull(() => folder(source)) !== null) {
            const notes = await folder(rootNames.concat([notesFolderName]), true);
            const noteName = `${crypto.randomUUID()}.json`;
            await saveNote(notes, noteName, JSON.stringify({ source, destination }));
            await moveContent(source, destination);
            await notes.removeEntry(noteName);
        }
    });
});

// ---- Locks ----

async function releasedLock(name) {
    const releasing = state.releasingLocks.get(name);
    if (releasing !== undefined) {
        await Promise.all([...releasing]);
    }
}

export const tryAcquireLock = guarded(async (name, mode) => {
    await releasedLock(name);
    return await new Promise((resolve, reject) => {
        const request = navigator.locks.request(name, { mode, ifAvailable: true }, lock => {
            if (lock === null) {
                resolve(null);
                return undefined;
            }
            return new Promise(release => resolve({ name, release, request }));
        });
        request.catch(reject);
    });
});

export function releaseLock(holder) {
    const releasing = state.releasingLocks.get(holder.name) ?? new Set();
    state.releasingLocks.set(holder.name, releasing);
    const released = holder.request.then(() => undefined, () => undefined).finally(() => {
        releasing.delete(released);
        if (releasing.size === 0 && state.releasingLocks.get(holder.name) === releasing) {
            state.releasingLocks.delete(holder.name);
        }
    });
    releasing.add(released);
    holder.release();
}

// ---- Storage ----

export const estimate = guarded(async () => {
    const answer = typeof navigator.storage?.estimate === 'function' ? await navigator.storage.estimate() : null;
    return answer === null ? null : JSON.stringify({ usage: answer.usage ?? 0, quota: answer.quota ?? 0 });
});

export const isPersisted = guarded(async () => await navigator.storage.persisted());

export const requestPersistence = guarded(async () => await navigator.storage.persist());
