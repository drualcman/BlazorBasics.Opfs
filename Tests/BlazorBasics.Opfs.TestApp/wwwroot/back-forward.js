// Drives itself, because automation keeps WebKit and Firefox out of the back/forward cache: it takes the lock, leaves for
// back-forward-away.html, and reports what it finds on coming back.
import { dotnet } from './_framework/dotnet.js';
import './test-hooks.js';

const SETTLE_MILLISECONDS = 400;

const run = new URLSearchParams(location.search).get('run');
const visitedKey = `opfs-back-forward-${run}`;
let probe = null;

async function start() {
    const runtime = await dotnet.create();
    const exports = await runtime.getAssemblyExports('BlazorBasics.Opfs.TestApp');
    probe = exports.BlazorBasics.Opfs.TestApp.BackForwardLockProbe;
    await runtime.runMain();
}

async function report(restored) {
    await new Promise(settled => setTimeout(settled, SETTLE_MILLISECONDS));
    const answer = {
        restored,
        hasHolder: probe.HasHolder(),
        isHeld: probe.IsHeld(),
        isLost: probe.IsLost(),
        canTakeTheName: await probe.CanTakeTheNameAsync()
    };
    await fetch(`/fixture/back-forward/${run}`, { method: 'POST', body: JSON.stringify(answer) });
}

addEventListener('pageshow', async event => {
    if (event.persisted) {
        await report(true);
    } else if (sessionStorage.getItem(visitedKey) !== null) {
        await start();
        await report(false);
    } else {
        sessionStorage.setItem(visitedKey, 'visited');
        await start();
        await probe.AcquireAsync();
        setTimeout(() => { location.href = `back-forward-away.html${location.search}`; }, SETTLE_MILLISECONDS);
    }
});
