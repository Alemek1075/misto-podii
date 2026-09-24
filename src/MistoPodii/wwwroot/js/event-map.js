(function () {
    const mapElement = document.getElementById('events-map');
    if (!mapElement) return;

    const status = document.getElementById('map-status');
    const list = document.getElementById('map-event-list');
    const summary = document.getElementById('category-summary');
    const category = document.getElementById('map-category');
    const date = document.getElementById('map-date');
    const filters = document.getElementById('map-filters');
    const chartElement = document.getElementById('category-chart');
    let map;
    let markers;
    let chart;
    let currentRequest;

    if (window.L) {
        map = L.map(mapElement).setView([50.4501, 30.5234], 12);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);
        markers = L.layerGroup().addTo(map);
    } else {
        mapElement.textContent = 'Мапа тимчасово недоступна. Список подій є нижче.';
    }

    if (window.Chart) {
        chart = new Chart(chartElement, {
            type: 'bar',
            data: { labels: [], datasets: [{ label: 'Події', data: [], backgroundColor: '#006e68', borderRadius: 2 }] },
            options: { responsive: true, maintainAspectRatio: false, indexAxis: 'y', animation: false,
                plugins: { legend: { display: false } }, scales: { x: { beginAtZero: true, ticks: { precision: 0 } } } }
        });
    }

    function eventLink(item) {
        const link = document.createElement('a');
        link.href = '/Events/Details/' + item.id;
        link.textContent = item.title;
        return link;
    }

    function render(items) {
        list.replaceChildren();
        summary.replaceChildren();
        if (markers) markers.clearLayers();

        const byVenue = new Map();
        const byCategory = new Map();
        items.forEach(function (item) {
            if (!byVenue.has(item.venueId)) byVenue.set(item.venueId, []);
            byVenue.get(item.venueId).push(item);
            byCategory.set(item.category, (byCategory.get(item.category) || 0) + 1);

            const row = document.createElement('li');
            row.append(eventLink(item));
            const description = document.createElement('span');
            description.textContent = ' · ' + item.venueName + ' · ' + item.startsAtUtc.slice(0, 16).replace('T', ' ') + ' UTC';
            row.append(description);
            list.append(row);
        });

        const bounds = [];
        byVenue.forEach(function (events) {
            const first = events[0];
            bounds.push([first.latitude, first.longitude]);
            if (!markers) return;
            const popup = document.createElement('div');
            const heading = document.createElement('strong');
            heading.textContent = first.venueName;
            popup.append(heading);
            const links = document.createElement('ul');
            events.forEach(function (event) {
                const row = document.createElement('li');
                row.append(eventLink(event));
                links.append(row);
            });
            popup.append(links);
            L.circleMarker([first.latitude, first.longitude], {
                radius: 9 + Math.min(events.length, 5), color: '#006e68', weight: 2,
                fillColor: '#e69b45', fillOpacity: 1
            }).bindPopup(popup).addTo(markers);
        });
        if (map && bounds.length === 1) map.setView(bounds[0], 14);
        if (map && bounds.length > 1) map.fitBounds(bounds, { padding: [32, 32], maxZoom: 14 });

        const categories = [...byCategory].sort((a, b) => a[0].localeCompare(b[0], 'uk'));
        categories.forEach(function ([name, count]) {
            const item = document.createElement('li');
            item.textContent = name + ': ' + count;
            summary.append(item);
        });
        if (chart) {
            chart.data.labels = categories.map(([name]) => name);
            chart.data.datasets[0].data = categories.map(([, count]) => count);
            chart.update();
        }
        status.textContent = items.length ? 'Показано подій: ' + items.length + '. Місць на мапі: ' + bounds.length + '.' : 'За цими фільтрами подій немає.';
    }

    async function load() {
        if (currentRequest) currentRequest.abort();
        currentRequest = new AbortController();
        const url = new URL(mapElement.dataset.url, window.location.origin);
        if (category.value) url.searchParams.set('category', category.value);
        if (date.value) url.searchParams.set('date', date.value);
        status.textContent = 'Завантажуємо події…';
        try {
            const response = await fetch(url, { signal: currentRequest.signal });
            if (!response.ok) throw new Error('Map data request failed');
            render(await response.json());
        } catch (error) {
            if (error.name !== 'AbortError') status.textContent = 'Не вдалося оновити події. Спробуйте змінити фільтр.';
        }
    }

    category.addEventListener('change', load);
    date.addEventListener('change', load);
    filters.addEventListener('reset', function () { setTimeout(load, 0); });
    load();
})();
