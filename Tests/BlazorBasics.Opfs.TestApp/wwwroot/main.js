import { runContract } from './contract-run.js';

try {
    document.getElementById('results').textContent = await runContract();
} catch (error) {
    globalThis.opfsFailed = `${error}`;
    throw error;
}
