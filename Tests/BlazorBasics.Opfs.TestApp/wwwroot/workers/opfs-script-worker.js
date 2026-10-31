// Only dynamic imports, so that the same script runs as a module worker and as a classic one.
(async () => {
    try {
        const opfs = await import('/_content/BlazorBasics.Opfs/opfs.js');
        const { runBattery } = await import('./opfs-battery.js');
        postMessage({ results: await runBattery(opfs) });
    } catch (error) {
        postMessage({ failure: `${error}` });
    }
})();
