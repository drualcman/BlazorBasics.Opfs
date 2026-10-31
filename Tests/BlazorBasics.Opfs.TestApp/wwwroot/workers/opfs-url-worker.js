import * as opfs from '/_content/BlazorBasics.Opfs/opfs.js';

const names = [`opfs-url-worker-${crypto.randomUUID()}`, 'a.txt'];
await opfs.writeText(names, 'from the worker');
postMessage(await opfs.objectUrl(names));
