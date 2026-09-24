(function () {
    const button = document.getElementById('run-benchmark');
    if (!button || !window.signalR) return;

    const status = document.getElementById('benchmark-status');
    const results = document.getElementById('benchmark-results');
    const download = document.getElementById('benchmark-download');
    const rawPanel = document.getElementById('benchmark-raw-panel');
    const rawText = document.getElementById('benchmark-raw');
    const transports = [
        { name: 'WebSockets', type: signalR.HttpTransportType.WebSockets },
        { name: 'Server Sent Events', type: signalR.HttpTransportType.ServerSentEvents },
        { name: 'Long Polling', type: signalR.HttpTransportType.LongPolling }
    ];
    let downloadUrl;

    function median(values) {
        const sorted = [...values].sort((a, b) => a - b);
        return (sorted[9] + sorted[10]) / 2;
    }

    async function measure(transport) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/events', { transport: transport.type })
            .configureLogging(signalR.LogLevel.Error).build();
        const startTime = performance.now();
        const start = performance.now();
        const timings = [];
        try {
            await connection.start();
            const connectMs = performance.now() - start;
            for (let index = 0; index < 20; index++) {
                const payload = 'event-benchmark-' + String(index).padStart(2, '0') + '-' + 'x'.repeat(64);
                const before = performance.now();
                const echoed = await connection.invoke('Echo', payload);
                if (echoed !== payload) throw new Error('Echo response mismatch');
                timings.push(performance.now() - before);
            }
            return { transport: transport.name, connectMs, medianRttMs: median(timings), rttSamplesMs: timings,
                startedAt: new Date().toISOString(), startTime };
        } finally {
            await connection.stop();
        }
    }

    button.addEventListener('click', async function () {
        button.disabled = true;
        results.replaceChildren();
        download.hidden = true;
        rawPanel.hidden = true;
        const run = { measuredAt: new Date().toISOString(), userAgent: navigator.userAgent,
            callsPerTransport: 20, payloadDescription: 'event-benchmark-NN- plus 64 x characters', measurements: [] };
        for (const transport of transports) {
            status.textContent = 'Вимірюємо ' + transport.name + '…';
            try {
                const sample = await measure(transport);
                const resources = performance.getEntriesByType('resource').filter(entry =>
                    entry.startTime >= sample.startTime && entry.name.includes('/hubs/events'));
                sample.httpRequests = resources.length;
                sample.httpTransferBytes = resources.reduce((sum, entry) => sum + entry.transferSize, 0);
                delete sample.startTime;
                run.measurements.push(sample);
                const row = document.createElement('tr');
                [sample.transport, sample.connectMs.toFixed(1) + ' мс', sample.medianRttMs.toFixed(1) + ' мс',
                    String(sample.httpRequests), String(sample.httpTransferBytes)].forEach(value => {
                    const cell = document.createElement('td'); cell.textContent = value; row.append(cell);
                });
                results.append(row);
            } catch (error) {
                run.measurements.push({ transport: transport.name, error: String(error) });
                const row = document.createElement('tr');
                const cell = document.createElement('td');
                cell.colSpan = 5;
                cell.textContent = transport.name + ': не вдалося виміряти (' + error.message + ')';
                row.append(cell);
                results.append(row);
            }
        }
        if (downloadUrl) URL.revokeObjectURL(downloadUrl);
        const json = JSON.stringify(run, null, 2);
        downloadUrl = URL.createObjectURL(new Blob([json], { type: 'application/json' }));
        download.href = downloadUrl;
        download.download = 'transport-benchmark.json';
        download.hidden = false;
        rawText.textContent = json;
        rawPanel.hidden = false;
        status.textContent = 'Замір завершено.';
        button.disabled = false;
    });
})();
