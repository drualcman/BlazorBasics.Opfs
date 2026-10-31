const encoder = new TextEncoder();

function assertEqual(expected, actual) {
    if (JSON.stringify(expected) !== JSON.stringify(actual)) {
        throw new Error(`Expected ${JSON.stringify(expected)} but got ${JSON.stringify(actual)}.`);
    }
}

async function failureOf(action) {
    let failure = null;
    try {
        await action();
    } catch (error) {
        failure = error.message;
    }
    return failure;
}

function viewOf(text) {
    const bytes = encoder.encode(text);
    return { slice: () => bytes, dispose() { } };
}

function namesIn(json) {
    const content = JSON.parse(json);
    return (content.files ?? content).map(file => file.name).sort();
}

function checks(opfs, root) {
    const path = (...names) => [root, ...names];
    return {
        async 'The probe finds a working file system'() {
            assertEqual('', await opfs.probe());
        },
        async 'Text is written and read back'() {
            await opfs.writeText(path('a.txt'), 'hello');
            assertEqual('hello', await opfs.readText(path('a.txt')));
        },
        async 'Bytes are written and read by ranges'() {
            await opfs.writeBytes(path('a.bin'), viewOf('abcdef'));
            assertEqual([97, 98, 99, 100, 101, 102], [...await opfs.readBytes(path('a.bin'))]);
            assertEqual([98, 99], [...await opfs.readRange(path('a.bin'), 1, 3)]);
        },
        async 'A blob is written and the public file functions read it'() {
            assertEqual(4, await opfs.writeBlob(path('blob.txt'), new Blob(['blob'])));
            assertEqual('blob', await (await opfs.getFile(path('blob.txt'))).text());
            assertEqual('blob', await (await opfs.requireFile(path('blob.txt'))).text());
        },
        async 'A missing file answers as designed'() {
            assertEqual(null, await opfs.getFile(path('none.txt')));
            assertEqual(null, await opfs.fileInfoOrNull(path('none.txt')));
            assertEqual(true, (await failureOf(() => opfs.readText(path('none.txt')))).startsWith('NotFoundError:'));
        },
        async 'Appending and truncating change the file'() {
            await opfs.writeText(path('log.txt'), 'one');
            await opfs.appendBytes(path('log.txt'), viewOf('two'));
            assertEqual('onetwo', await opfs.readText(path('log.txt')));
            await opfs.truncate(path('log.txt'), 2);
            assertEqual('on', await opfs.readText(path('log.txt')));
        },
        async 'A writer shows its content only when completed'() {
            await opfs.writeText(path('w.txt'), 'old');
            const writer = await opfs.openWriter(path('w.txt'), false);
            await opfs.writeToWriter(writer, viewOf('new'));
            assertEqual('old', await opfs.readText(path('w.txt')));
            await opfs.completeWriter(writer);
            assertEqual('new', await opfs.readText(path('w.txt')));
        },
        async 'An aborted writer leaves the file as it was'() {
            await opfs.writeText(path('w.txt'), 'old');
            const writer = await opfs.openWriter(path('w.txt'), false);
            await opfs.writeToWriter(writer, viewOf('new'));
            await opfs.abortWriter(writer);
            assertEqual('old', await opfs.readText(path('w.txt')));
        },
        async 'Listing a folder and a tree finds the files'() {
            await opfs.writeText(path('list', 'a.txt'), 'a');
            await opfs.writeText(path('list', 'deep', 'b.txt'), 'b');
            assertEqual(['a.txt'], namesIn(await opfs.listFolder(path('list'), null)));
            assertEqual(['a.txt', 'deep/b.txt'], namesIn(await opfs.listTree(path('list'), null)));
        },
        async 'A file is copied and moved over a destination'() {
            await opfs.writeText(path('source.txt'), 'source');
            await opfs.writeText(path('destination.txt'), 'older and longer');
            assertEqual(6, await opfs.copy(1, path('source.txt'), path('copy.txt')));
            await opfs.moveFile(path('copy.txt'), path('destination.txt'));
            assertEqual('source', await opfs.readText(path('destination.txt')));
            assertEqual(null, await opfs.getFile(path('copy.txt')));
        },
        async 'A download lands in a file'() {
            const fetched = await opfs.fetchTo(2, `${location.origin}/fixture/bytes/1024`, path('fetched.bin'), 0, 'cors', 'no-store', 'omit');
            assertEqual({ bytes: 1024, tooLarge: false }, JSON.parse(fetched));
        },
        async 'A download over its limit leaves nothing'() {
            const fetched = await opfs.fetchTo(3, `${location.origin}/fixture/chunked/131072`, path('large.bin'), 1024, 'cors', 'no-store', 'omit');
            assertEqual({ bytes: 0, tooLarge: true }, JSON.parse(fetched));
            assertEqual(null, await opfs.getFile(path('large.bin')));
        },
        async 'An object url serves the file'() {
            await opfs.writeText(path('url.txt'), 'from a blob');
            const url = await opfs.objectUrl(path('url.txt'));
            assertEqual('from a blob', await (await fetch(url)).text());
            opfs.revokeObjectUrl(url);
        },
        async 'A lock clashes and is free at once after its release'() {
            const name = `lock-${root}`;
            const held = await opfs.tryAcquireLock(name, 'exclusive');
            assertEqual(null, await opfs.tryAcquireLock(name, 'exclusive'));
            opfs.releaseLock(held);
            opfs.releaseLock(await opfs.tryAcquireLock(name, 'exclusive'));
        },
        async 'A folder moves and a pending move finishes'() {
            await opfs.writeText(path('moves', 'one', 'a.txt'), '1');
            await opfs.moveFolder(path('moves'), ['one'], ['two'], '.notes', 'worker-moves:');
            assertEqual('1', await opfs.readText(path('moves', 'two', 'a.txt')));

            const note = JSON.stringify({ source: path('moves', 'two'), destination: path('moves', 'three') });
            await opfs.writeText(path('moves', '.notes', 'pending.json'), note);
            await opfs.finishPendingMoves(path('moves'), '.notes', 'worker-moves:');
            assertEqual('1', await opfs.readText(path('moves', 'three', 'a.txt')));
        },
        async 'The storage answers'() {
            assertEqual(true, JSON.parse(await opfs.estimate()).quota > 0);
            assertEqual('boolean', typeof await opfs.isPersisted());
        },
        async 'Removing a file and clearing a folder leave nothing'() {
            await opfs.writeText(path('gone', 'a.txt'), 'a');
            await opfs.writeText(path('gone', 'b.txt'), 'b');
            await opfs.remove(path('gone', 'a.txt'));
            assertEqual(['b.txt'], namesIn(await opfs.listFolder(path('gone'), null)));
            await opfs.clearFolder(path('gone'));
            assertEqual([], namesIn(await opfs.listFolder(path('gone'), null)));
        },
        async 'Downloading fails naming the worker'() {
            await opfs.writeText(path('d.txt'), 'd');
            const failure = await failureOf(() => opfs.downloadFile(path('d.txt'), 'd.txt'));
            assertEqual(true, failure.startsWith('NotSupportedError:') && failure.includes('worker'));
        },
        async 'Requesting persistence fails naming the worker'() {
            const failure = await failureOf(() => opfs.requestPersistence());
            assertEqual(true, failure.startsWith('NotSupportedError:') && failure.includes('worker'));
        }
    };
}

export async function runBattery(opfs) {
    const root = `opfs-script-worker-${crypto.randomUUID()}`;
    const results = [];
    for (const [name, check] of Object.entries(checks(opfs, root))) {
        const failure = await failureOf(check);
        results.push({ Name: name, Passed: failure === null, Failure: failure });
    }
    await opfs.remove([root]);
    return results;
}
