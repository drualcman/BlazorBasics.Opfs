// Drives itself, because automation keeps WebKit and Firefox out of the back/forward cache: it takes the lock, leaves for
// back-forward-away.html, and reports what it finds on coming back. With lock=false it is the control: a page with no lock
// that should be restored. With return it goes back to that page after reporting, which is how device.html uses it.
import { dotnet } from './_framework/dotnet.js';
import './test-hooks.js';

const SETTLE_MILLISECONDS = 400;

const parameters = new URLSearchParams(location.search);
const run = parameters.get('run');
const takesTheLock = parameters.get('lock') !== 'false';
const returnPage = parameters.get('return');
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
    if (returnPage !== null) {
        localStorage.setItem(`opfs-device-back-forward-${run}`, JSON.stringify(answer));
        location.replace(returnPage);
    }
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
        if (takesTheLock) {
            await probe.AcquireAsync();
        }
        setTimeout(() => { location.href = `back-forward-away.html${location.search}`; }, SETTLE_MILLISECONDS);
    }
});
