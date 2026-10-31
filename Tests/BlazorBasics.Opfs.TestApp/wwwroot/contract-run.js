import { dotnet } from './_framework/dotnet.js';
import './test-hooks.js';

export async function runContract() {
    const runtime = await dotnet.create();
    globalThis.opfsRuntime = runtime;
    await runtime.runMain();
    return globalThis.opfsContractResults;
}
