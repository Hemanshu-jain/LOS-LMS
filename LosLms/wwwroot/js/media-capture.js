// Live photo + Video KYC capture for Customer Details.
//
// CSP-safe: this is an external file (script-src 'self'), no inline handlers, no eval. getUserMedia and
// MediaRecorder are browser APIs, not resources, so they need no CSP entry — only the Permissions-Policy
// (camera=(self), microphone=(self)) set by the server, and, in the WebView2 desktop shell, the host
// granting the permission request.
//
// Binary is handed to .NET as an IJSStreamReference (DotNet.createJSStreamReference), which streams the
// blob to the server in chunks — no base64 bloat and not bound by the SignalR message-size cap. Photos
// are captured lossless (PNG) at the camera's native resolution; video is recorded at a high bitrate so
// the evidence is not degraded.

(() => {
    // One live MediaStream + recorder per preview element id, so the two panels are independent.
    const streams = new Map();
    const recorders = new Map();

    function pickVideoMime() {
        const prefs = [
            'video/webm;codecs=vp9,opus',
            'video/webm;codecs=vp8,opus',
            'video/webm',
        ];
        for (const m of prefs) {
            if (window.MediaRecorder && MediaRecorder.isTypeSupported(m)) { return m; }
        }
        return 'video/webm';
    }

    // Opens the camera (and mic when withAudio) at up to 1080p and shows the live preview. Returns "" on
    // success or a human-readable error string the C# side can surface.
    window.losStartCamera = async (previewId, withAudio) => {
        try {
            const el = document.getElementById(previewId);
            if (!el) { return 'Preview element not found.'; }

            // Stop an earlier stream on the same panel before reopening.
            window.losStopCamera(previewId);

            const stream = await navigator.mediaDevices.getUserMedia({
                video: { width: { ideal: 1920 }, height: { ideal: 1080 }, facingMode: 'user' },
                audio: !!withAudio,
            });

            streams.set(previewId, stream);
            el.srcObject = stream;
            el.muted = true; // never echo the mic into the room during preview
            await el.play().catch(() => { /* autoplay race — the stream is still live */ });
            return '';
        } catch (err) {
            return (err && err.name === 'NotAllowedError')
                ? 'Camera/microphone permission was denied.'
                : ('Could not start the camera: ' + (err && err.message ? err.message : err));
        }
    };

    // POSTs a captured blob to the server's party-media endpoint (same-origin, auth cookie). Returns
    // { name } on success or { error } — the binary never crosses the Blazor JS-interop boundary, which
    // could not reliably marshal a recorded Blob.
    async function postCapture(url, blob) {
        try {
            // Raw binary body to a dedicated middleware endpoint (handled before antiforgery, so no token
            // is needed). Content-Type marks it non-form so nothing tries to parse it as a form.
            const res = await fetch(url, {
                method: 'POST', body: blob, credentials: 'same-origin',
                headers: { 'Content-Type': 'application/octet-stream' },
            });
            if (!res.ok) { return { error: 'Upload failed (' + res.status + ').' }; }
            return await res.json();
        } catch (e) {
            return { error: 'Upload failed: ' + (e && e.message ? e.message : e) };
        }
    }

    // Grabs the current frame at native resolution as a lossless PNG and uploads it. Returns { name } / { error }.
    window.losCapturePhoto = (previewId, url) => {
        const el = document.getElementById(previewId);
        if (!el || !el.videoWidth) { return { error: 'Camera is not ready.' }; }

        const canvas = document.createElement('canvas');
        canvas.width = el.videoWidth;
        canvas.height = el.videoHeight;
        canvas.getContext('2d').drawImage(el, 0, 0, canvas.width, canvas.height);

        return new Promise((resolve) => {
            canvas.toBlob(async (blob) => {
                if (!blob) { resolve({ error: 'Could not capture the frame.' }); return; }
                resolve(await postCapture(url, blob));
            }, 'image/png');
        });
    };

    // Begins recording the panel's live stream. Returns "" on success or an error string.
    window.losStartRecording = (previewId) => {
        const stream = streams.get(previewId);
        if (!stream) { return 'Open the camera first.'; }

        try {
            const mime = pickVideoMime();
            const recorder = new MediaRecorder(stream, { mimeType: mime, videoBitsPerSecond: 8000000 });
            const chunks = [];
            recorder.ondataavailable = (e) => { if (e.data && e.data.size > 0) { chunks.push(e.data); } };
            recorder.__chunks = chunks;
            recorder.__mime = mime;
            recorders.set(previewId, recorder);
            recorder.start();
            return '';
        } catch (err) {
            return 'Could not start recording: ' + (err && err.message ? err.message : err);
        }
    };

    // Stops recording, assembles the clip and uploads it. Returns { name } / { error }.
    window.losStopRecording = (previewId, url) => {
        const recorder = recorders.get(previewId);
        if (!recorder) { return { error: 'No active recording.' }; }

        return new Promise((resolve) => {
            recorder.onstop = async () => {
                const blob = new Blob(recorder.__chunks, { type: recorder.__mime });
                recorders.delete(previewId);
                if (blob.size === 0) { resolve({ error: 'The recording was empty.' }); return; }
                resolve(await postCapture(url, blob));
            };
            recorder.stop();
        });
    };

    // Stops the camera/mic and releases the hardware (the light goes off).
    window.losStopCamera = (previewId) => {
        const stream = streams.get(previewId);
        if (stream) {
            stream.getTracks().forEach((t) => t.stop());
            streams.delete(previewId);
        }
        const el = document.getElementById(previewId);
        if (el) { el.srcObject = null; }
        recorders.delete(previewId);
    };
})();
