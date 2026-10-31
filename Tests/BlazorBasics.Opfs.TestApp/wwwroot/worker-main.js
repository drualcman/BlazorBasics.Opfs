const worker = new Worker('./workers/dotnet/contract-worker.js', { type: 'module' });

worker.onmessage = ({ data }) => {
    if (data.failure === undefined) {
        globalThis.opfsContractResults = data.results;
        document.getElementById('results').textContent = data.results;
    } else {
        globalThis.opfsFailed = data.failure;
    }
};

worker.onerror = event => {
    globalThis.opfsFailed = event.message || 'The worker did not start.';
};
