// Turns the results of device.js into text: a summary short enough for one screenshot, and the details behind it.
const MEBIBYTE = 1024 * 1024;
const LONGEST_FAILURE_LINE = 160;

const mebibytes = bytes => `${(bytes / MEBIBYTE).toFixed(1)} MiB`;
const firstLine = text => text.split('\n')[0].slice(0, LONGEST_FAILURE_LINE);

function probeLine(probe) {
    let line = 'not run';
    if (probe !== undefined) {
        line = probe.isAvailable ? 'available' : `NOT available: ${probe.reason} [${probe.failureType ?? 'no failure'}, ${probe.browserError ?? 'no browser error'}]`;
    }
    return line;
}

function batteryLines(battery, seconds) {
    let lines = ['Battery: not run'];
    if (battery?.failed !== undefined) {
        lines = [`Battery: failed to run: ${firstLine(battery.failed)}`];
    } else if (battery !== undefined) {
        const failures = battery.filter(test => !test.passed);
        lines = [`Battery: ${battery.length - failures.length}/${battery.length} passed in ${seconds} s`];
        lines.push(...failures.map(test => `  FAIL ${test.name}: ${firstLine(test.failure)}`));
    }
    return lines;
}

function estimateLines(estimate) {
    let lines = ['Estimate: not run'];
    if (estimate?.failed !== undefined) {
        lines = [`Estimate: failed: ${firstLine(estimate.failed)}`];
    } else if (estimate !== undefined) {
        const rounds = estimate.rounds.map(round => `${mebibytes(round.afterWrite)} > ${mebibytes(round.afterDelete)}`).join(', ');
        lines = [
            `Estimate after removing: ${estimate.verdict}, ${mebibytes(estimate.fileLength)} file, quota ${mebibytes(estimate.quota)}`,
            `  before ${mebibytes(estimate.before)}, written > removed: ${rounds}`,
            `  ${mebibytes(estimate.settled)} after waiting ${estimate.settledAfterSeconds} s`
        ];
    }
    return lines;
}

function snapshotLines(snapshots) {
    let lines = ['Snapshot after a write: not run'];
    if (snapshots?.failed !== undefined) {
        lines = [`Snapshot after a write: failed: ${firstLine(snapshots.failed)}`];
    } else if (snapshots !== undefined) {
        lines = ['Snapshot after a write:'];
        lines.push(...snapshots.map(read => `  ${read.case}: ${read.browserError ?? `read "${read.read}"`} (length ${read.snapshotLength})`));
    }
    return lines;
}

function moveLines(moves) {
    let lines = ['Move with an open writer: not run'];
    if (moves?.failed !== undefined) {
        lines = [`Move with an open writer: failed: ${firstLine(moves.failed)}`];
    } else if (moves !== undefined) {
        lines = ['Move with an open writer:'];
        lines.push(...moves.map(move =>
            `  on the ${move.writerOn}: ${move.moveError === null ? 'moved' : `refused, ${move.moveError}`}; ` +
            `writer ${move.writerError ?? 'completed'}; source=${move.source}, destination=${move.destination}`));
    }
    return lines;
}

function tripLine(name, trip) {
    let line = `  ${name}: not run`;
    if (trip?.lostTrip) {
        line = `  ${name}: no report came back`;
    } else if (trip !== undefined) {
        line = `  ${name}: ${trip.restored ? 'restored' : 'loaded again'}, holder=${trip.hasHolder}, held=${trip.isHeld}, lost=${trip.isLost}, name free=${trip.canTakeTheName}`;
    }
    return line;
}

function backForwardLines(backForward) {
    const control = backForward.control;
    const inconclusive = control !== undefined && !control.restored ? ' INCONCLUSIVE: the control was not restored' : '';
    return [
        `Back/forward:${inconclusive}`,
        tripLine('control, no lock', control),
        tripLine('held', backForward.held),
        tripLine('held, name taken away', backForward.competed)
    ];
}

export function summaryOf(results) {
    const lines = [
        `Device: ${results.device || 'NOT SAID'}`,
        `Agent: ${results.userAgent}`,
        `getDirectory: ${results.browserRoot ?? 'not run'}`,
        `Probe: ${probeLine(results.probe)}`,
        ...batteryLines(results.battery, results.batterySeconds),
        ...estimateLines(results.estimate),
        ...snapshotLines(results.snapshots),
        ...moveLines(results.moves),
        ...backForwardLines(results.backForward)
    ];
    if (results.estimateNow) {
        lines.push(`Estimate on loading this page: ${mebibytes(results.estimateNow.usage)} of ${mebibytes(results.estimateNow.quota)}`);
    }
    if (results.startFailure) {
        lines.push(`The page could not start: ${results.startFailure}`);
    }
    return lines.join('\n');
}

export function detailsOf(results) {
    const failures = Array.isArray(results.battery) ? results.battery.filter(test => !test.passed) : [];
    const details = failures.map(test => `--- ${test.name}\n${test.failure}`).join('\n');
    const { battery, ...measured } = results;
    return `${details}\n--- Results\n${JSON.stringify(measured, null, 1)}`;
}
