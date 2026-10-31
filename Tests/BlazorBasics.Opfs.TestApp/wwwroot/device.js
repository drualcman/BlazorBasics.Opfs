// The manual checks of a real device. Every result stays in localStorage, because the back/forward trips leave this page.
// It never calls runMain, so the battery only runs when asked. Each change is also sent to the computer that serves the page.
// With start=back-forward the trips begin alone, for a browser that no automation can click.
import { dotnet } from './_framework/dotnet.js';
import { detailsOf, summaryOf } from './device-report.js';
import './test-hooks.js';

const RESULTS_KEY = 'opfs-device-results';
const DEVICE_KEY = 'opfs-device-name';
const PLAN_KEY = 'opfs-device-back-forward-plan';
const BACK_FORWARD_TRIPS = [
    { name: 'control', lock: false, compete: false },
    { name: 'held', lock: true, compete: false },
    { name: 'competed', lock: true, compete: true }
];

const element = id => document.getElementById(id);
const results = JSON.parse(localStorage.getItem(RESULTS_KEY) ?? 'null') ?? newResults();
let checks = null;

function newResults() {
    const id = `${new Date().toISOString().replace(/\D/g, '').slice(0, 14)}-${crypto.randomUUID().slice(0, 8)}`;
    return { id, userAgent: navigator.userAgent, backForward: {} };
}

function status(text) {
    element('status').textContent = text;
}

async function publish() {
    results.device = element('device').value;
    results.userAgent = navigator.userAgent;
    localStorage.setItem(RESULTS_KEY, JSON.stringify(results));
    const summary = summaryOf(results);
    const report = `${summary}\n\n${detailsOf(results)}`;
    element('summary').textContent = summary;
    element('report').textContent = report;
    try {
        await fetch(`/fixture/device-report/${results.id}`, { method: 'POST', body: report });
    } catch (error) {
        status(`The report did not reach the computer: ${error}`);
    }
}

function setButtons(enabled) {
    for (const id of ['run-all', 'run-measurements', 'run-battery', 'run-back-forward']) {
        element(id).disabled = !enabled;
    }
}

async function browserRoot() {
    let answer = 'getDirectory worked';
    try {
        await navigator.storage.getDirectory();
    } catch (error) {
        answer = `${error?.name}: ${error?.message}`;
    }
    return answer;
}

// Asked on every load, so opening the page again after restarting the browser shows whether the space came back.
async function estimateNow() {
    let estimate = null;
    try {
        estimate = await navigator.storage.estimate();
    } catch (error) {
        console.warn(error);
    }
    return estimate === null ? null : { usage: estimate.usage, quota: estimate.quota };
}

async function probe() {
    status('Probing...');
    results.browserRoot = await browserRoot();
    results.probe = JSON.parse(await checks.ProbeAsync());
    results.persisted = await navigator.storage?.persisted?.() ?? null;
    await publish();
}

// A measurement that fails is a result: the others still run, and the report shows it.
async function measure(key, title, action) {
    status(`${title}...`);
    try {
        results[key] = JSON.parse(await action());
    } catch (error) {
        results[key] = { failed: `${error}` };
    }
    await publish();
}

async function measurements() {
    await probe();
    if (results.probe.isAvailable) {
        await measure('snapshots', 'Snapshot read after a write', () => checks.MeasureSnapshotsAsync());
        await measure('moves', 'Move with an open writer', () => checks.MeasureMovesAsync());
        await measure('estimate', 'Estimate after removing a file, up to 30 seconds', () => checks.MeasureEstimateAsync());
    }
}

async function battery() {
    await probe();
    if (results.probe.isAvailable) {
        const started = performance.now();
        let done = 0;
        globalThis.opfsTestProgress = name => status(`Battery, test ${++done}: ${name}`);
        await measure('battery', 'Battery', () => checks.RunBatteryAsync());
        results.batterySeconds = Math.round((performance.now() - started) / 1000);
        await publish();
    }
}

function nextBackForwardTrip() {
    const trip = BACK_FORWARD_TRIPS.find(candidate => results.backForward[candidate.name] === undefined);
    if (trip === undefined) {
        localStorage.removeItem(PLAN_KEY);
        status('Finished.');
        setButtons(true);
    } else {
        const run = `${results.id}-${trip.name}`;
        localStorage.setItem(PLAN_KEY, JSON.stringify({ run, name: trip.name }));
        location.href = `back-forward.html?run=${run}&lock=${trip.lock}&compete=${trip.compete}&return=device.html`;
    }
}

function collectBackForwardTrip() {
    const plan = JSON.parse(localStorage.getItem(PLAN_KEY) ?? 'null');
    if (plan !== null) {
        results.backForward[plan.name] = JSON.parse(localStorage.getItem(`opfs-device-back-forward-${plan.run}`) ?? 'null') ?? { lostTrip: true };
    }
    return plan !== null;
}

function startBackForward() {
    results.backForward = {};
    localStorage.setItem(RESULTS_KEY, JSON.stringify(results));
    nextBackForwardTrip();
}

function buttonRuns(id, action) {
    element(id).addEventListener('click', async () => {
        setButtons(false);
        await action();
        status('Finished.');
        setButtons(true);
    });
}

async function copyReport() {
    try {
        await navigator.clipboard.writeText(element('report').textContent);
        status('Copied.');
    } catch (error) {
        status(`Could not copy, so take a screenshot of the summary: ${error}`);
    }
}

async function start() {
    element('device').value = localStorage.getItem(DEVICE_KEY) ?? '';
    element('device').addEventListener('input', event => {
        localStorage.setItem(DEVICE_KEY, event.target.value);
        results.device = event.target.value;
    });
    element('copy').addEventListener('click', copyReport);
    element('reset').addEventListener('click', () => {
        localStorage.removeItem(RESULTS_KEY);
        localStorage.removeItem(PLAN_KEY);
        location.reload();
    });
    const returned = collectBackForwardTrip();
    await publish();

    const runtime = await dotnet.create();
    globalThis.opfsRuntime = runtime;
    const exports = await runtime.getAssemblyExports('BlazorBasics.Opfs.TestApp');
    checks = exports.BlazorBasics.Opfs.TestApp.DeviceChecks;

    buttonRuns('run-measurements', measurements);
    buttonRuns('run-battery', battery);
    element('run-back-forward').addEventListener('click', startBackForward);
    element('run-all').addEventListener('click', async () => {
        setButtons(false);
        await measurements();
        await battery();
        startBackForward();
    });

    if (returned) {
        await publish();
        nextBackForwardTrip();
    } else if (new URLSearchParams(location.search).get('start') === 'back-forward') {
        startBackForward();
    } else {
        results.estimateNow = await estimateNow();
        await publish();
        status('Ready.');
        setButtons(true);
    }
    globalThis.opfsDeviceReady = true;
}

start().catch(error => {
    status(`The page could not start: ${error}`);
    results.startFailure = `${error}`;
    publish();
});
