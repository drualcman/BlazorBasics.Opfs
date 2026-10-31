// Two folders deep on purpose: "../_content" from here would miss, so the battery passes only if the runtime resolves
// the import of the package from its own folder, not from this script.
import { runContract } from '../../contract-run.js';

try {
    postMessage({ results: await runContract() });
} catch (error) {
    postMessage({ failure: `${error}` });
}
