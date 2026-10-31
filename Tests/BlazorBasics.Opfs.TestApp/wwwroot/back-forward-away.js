// Plays another tab of the site while back-forward.html is in the back/forward cache, then goes back to it.
const SETTLE_MILLISECONDS = 400;
const LOCK_NAME = 'back-forward';

const parameters = new URLSearchParams(location.search);

addEventListener('load', () => setTimeout(async () => {
    if (parameters.get('compete') === 'true') {
        const granted = await navigator.locks.request(LOCK_NAME, { ifAvailable: true }, lock => lock !== null);
        await fetch(`/fixture/back-forward/${parameters.get('run')}-away`, { method: 'POST', body: JSON.stringify({ granted }) });
    }
    history.back();
}, SETTLE_MILLISECONDS));
