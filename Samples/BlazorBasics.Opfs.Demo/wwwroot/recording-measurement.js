// Measures a recording through one IOpfsWriter. The recorder settings and the chained delivery copy what ShotUpAlbums
// runs today: BlazorBasics.Camera 1.4.6 (CameraView.razor.js, CameraVideoOptions) and CameraUiSpec.MaxVideoSeconds.

const MIME_CANDIDATES = ['video/mp4;codecs=avc1.42E01E,mp4a.40.2', 'video/mp4;codecs=avc1.42E01E', 'video/mp4'];
const RESOLUTION = { width: { ideal: 1280 }, height: { ideal: 720 } };
const VIDEO_BITS_PER_SECOND = 2500000;
const AUDIO_BITS_PER_SECOND = 128000;
const CHUNK_MILLISECONDS = 1000;
const RECORDING_MILLISECONDS = 60000;
const HEARTBEAT_MILLISECONDS = 100;
const WAIT_MILLISECONDS = 15000;

const session = { stream: null, mimeType: null, wakeLock: null, preview: null, chunks: [] };

async function requestWakeLock() {
    try {
        return await navigator.wakeLock?.request('screen') ?? null;
    } catch {
        return null;
    }
}

async function requestMicrophone() {
    try {
        return (await navigator.mediaDevices.getUserMedia({ audio: true, video: false })).getAudioTracks();
    } catch {
        return [];
    }
}

export async function openCamera(preview) {
    const mimeType = MIME_CANDIDATES.find(candidate => MediaRecorder.isTypeSupported(candidate));
    if (mimeType === undefined) {
        throw new Error('This browser cannot record MP4, so ShotUpAlbums would offer no video here.');
    }
    const camera = await navigator.mediaDevices.getUserMedia({
        video: { ...RESOLUTION, frameRate: { ideal: 30 }, facingMode: { ideal: 'environment' } },
        audio: false
    });
    const track = camera.getVideoTracks()[0];
    const fastestFrameRate = track.getCapabilities?.()?.frameRate?.max;
    if (typeof fastestFrameRate !== 'number' || fastestFrameRate >= 24) {
        await track.applyConstraints({ ...RESOLUTION, frameRate: { min: 24, ideal: 30 } }).catch(() => { });
    }
    const microphone = await requestMicrophone();
    preview.srcObject = camera;
    await preview.play();
    session.stream = new MediaStream([track, ...microphone]);
    session.mimeType = mimeType;
    session.preview = preview;
    session.chunks = [];
    session.wakeLock = await requestWakeLock();
    const settings = track.getSettings();
    return JSON.stringify({
        mimeType,
        width: settings.width ?? 0,
        height: settings.height ?? 0,
        frameRate: settings.frameRate ?? 0,
        hasAudio: microphone.length > 0,
        wakeLock: session.wakeLock !== null,
        userAgent: navigator.userAgent,
        processors: navigator.hardwareConcurrency ?? 0,
        memoryGigabytes: navigator.deviceMemory ?? 0
    });
}

export async function closeCamera() {
    session.stream?.getTracks().forEach(track => track.stop());
    session.stream = null;
    if (session.preview !== null) {
        session.preview.srcObject = null;
    }
    await session.wakeLock?.release();
    session.wakeLock = null;
}

async function deliver(blob, listener, index, gapMs, arrivedAt, outcome) {
    try {
        const buffer = await blob.arrayBuffer();
        session.chunks.push({ size: blob.size, digest: crypto.subtle.digest('SHA-256', buffer) });
        await listener.invokeMethodAsync('OnChunkAsync', new Uint8Array(buffer), index, gapMs, performance.now() - arrivedAt);
    } catch (error) {
        outcome.interruptions.push(`Chunk ${index} was not delivered: ${error?.message}`);
    }
}

export function record(listener) {
    return new Promise((resolve, reject) => {
        const outcome = { durationMs: 0, chunksSeen: 0, emptyChunks: 0, longestStallMs: 0, drainMs: 0, interruptions: [] };
        const startedAt = performance.now();
        const note = text => outcome.interruptions.push(`${((performance.now() - startedAt) / 1000).toFixed(1)} s: ${text}`);
        const noteHidden = () => {
            if (document.hidden) {
                note('the page went to the background');
            }
        };
        let beat = startedAt;
        const heartbeat = setInterval(() => {
            const now = performance.now();
            outcome.longestStallMs = Math.max(outcome.longestStallMs, now - beat - HEARTBEAT_MILLISECONDS);
            beat = now;
        }, HEARTBEAT_MILLISECONDS);
        document.addEventListener('visibilitychange', noteHidden);
        session.stream.getTracks().forEach(track => {
            track.onended = () => note(`the ${track.kind} track ended`);
            track.onmute = () => note(`the ${track.kind} track was muted`);
        });

        const recorder = new MediaRecorder(session.stream, {
            mimeType: session.mimeType,
            videoBitsPerSecond: VIDEO_BITS_PER_SECOND,
            audioBitsPerSecond: AUDIO_BITS_PER_SECOND
        });
        let queue = Promise.resolve();
        let previousArrival = startedAt;
        recorder.ondataavailable = event => {
            const arrivedAt = performance.now();
            const gapMs = arrivedAt - previousArrival;
            previousArrival = arrivedAt;
            if (event.data && event.data.size > 0) {
                const index = outcome.chunksSeen++;
                queue = queue.then(() => deliver(event.data, listener, index, gapMs, arrivedAt, outcome));
            } else {
                outcome.emptyChunks++;
            }
        };
        recorder.onerror = event => note(`the recorder failed: ${event?.error?.message}`);
        recorder.onstop = () => {
            const stoppedAt = performance.now();
            outcome.durationMs = stoppedAt - startedAt;
            clearInterval(heartbeat);
            document.removeEventListener('visibilitychange', noteHidden);
            queue.then(() => {
                outcome.drainMs = performance.now() - stoppedAt;
                resolve(JSON.stringify(outcome));
            });
        };
        try {
            recorder.start(CHUNK_MILLISECONDS);
            setTimeout(() => {
                if (recorder.state !== 'inactive') {
                    recorder.stop();
                }
            }, RECORDING_MILLISECONDS);
        } catch (error) {
            clearInterval(heartbeat);
            document.removeEventListener('visibilitychange', noteHidden);
            reject(error);
        }
    });
}

function sameBytes(left, right) {
    const leftBytes = new Uint8Array(left);
    const rightBytes = new Uint8Array(right);
    return leftBytes.length === rightBytes.length && leftBytes.every((value, index) => value === rightBytes[index]);
}

async function compareChunks(file, result) {
    let offset = 0;
    for (const chunk of session.chunks) {
        const stored = await crypto.subtle.digest('SHA-256', await file.slice(offset, offset + chunk.size).arrayBuffer());
        result.checkedChunks++;
        if (sameBytes(stored, await chunk.digest)) {
            result.matchedChunks++;
        }
        offset += chunk.size;
    }
}

function eventOf(video, name) {
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => finish(reject, new Error(`The video gave no ${name} event.`)), WAIT_MILLISECONDS);
        const onEvent = () => finish(resolve);
        const onError = () => finish(reject, new Error(video.error?.message || `Media error ${video.error?.code}`));
        function finish(settle, value) {
            clearTimeout(timer);
            video.removeEventListener(name, onEvent);
            video.removeEventListener('error', onError);
            settle(value);
        }
        video.addEventListener(name, onEvent);
        video.addEventListener('error', onError);
    });
}

function playedPast(video, time) {
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => finish(reject, new Error(`Playback stopped at ${video.currentTime} s.`)), WAIT_MILLISECONDS);
        const check = () => {
            if (video.currentTime >= time || video.ended) {
                finish(resolve);
            }
        };
        function finish(settle, value) {
            clearTimeout(timer);
            video.removeEventListener('timeupdate', check);
            settle(value);
        }
        video.addEventListener('timeupdate', check);
    });
}

async function seek(video, time) {
    const seeked = eventOf(video, 'seeked');
    video.currentTime = time;
    await seeked;
}

async function knownDuration(video) {
    if (!Number.isFinite(video.duration)) {
        await seek(video, 1e7);
    }
    return video.duration;
}

async function checkPlayback(video, url, result) {
    const loaded = eventOf(video, 'loadedmetadata');
    video.muted = true;
    video.src = url;
    await loaded;
    result.width = video.videoWidth;
    result.height = video.videoHeight;
    await video.play();
    await playedPast(video, 1);
    result.playable = true;
    video.pause();
    result.durationSeconds = await knownDuration(video);
    await seek(video, Math.max(0, result.durationSeconds - 2));
    await video.play();
    await playedPast(video, result.durationSeconds - 1);
    result.reachedEnd = true;
    video.pause();
    result.decodedFrames = video.getVideoPlaybackQuality?.().totalVideoFrames ?? 0;
    await seek(video, 0);
    video.muted = false;
}

export async function checkFile(video, url) {
    const result = {
        checkedChunks: 0, matchedChunks: 0, playable: false, reachedEnd: false, durationSeconds: 0,
        width: 0, height: 0, decodedFrames: 0, failure: null
    };
    try {
        await compareChunks(await (await fetch(url)).blob(), result);
        await checkPlayback(video, url, result);
    } catch (error) {
        result.failure = `${error?.name}: ${error?.message}`;
    }
    return JSON.stringify(result);
}

export async function copyText(area) {
    let copied = true;
    area.select();
    try {
        await navigator.clipboard.writeText(area.value);
    } catch {
        copied = document.execCommand('copy');
    }
    return copied;
}
