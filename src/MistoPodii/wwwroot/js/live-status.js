(function () {
    const badges = document.querySelectorAll('.event-status[data-event-id]');
    if (!badges.length || !window.signalR) return;

    const labels = { Planned: 'Заплановано', Postponed: 'Перенесено', Cancelled: 'Скасовано' };
    const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/events').withAutomaticReconnect().build();
    connection.on('EventChanged', function (change) {
        if (change.action === 'created' || change.action === 'deleted') {
            window.location.reload();
            return;
        }
        badges.forEach(function (badge) {
            if (Number(badge.dataset.eventId) !== change.id) return;
            badge.textContent = labels[change.status] || change.status;
            badge.classList.remove('just-updated');
            void badge.offsetWidth;
            badge.classList.add('just-updated');
        });
    });
    const indicator = document.getElementById('live-connection');
    function showConnection(text) { if (indicator) indicator.textContent = text; }
    connection.onreconnecting(function () { showConnection('Відновлюємо з’єднання…'); });
    connection.onreconnected(function () { showConnection('З’єднано'); });
    connection.onclose(function () { showConnection('Живі оновлення недоступні. Оновіть сторінку.'); });
    connection.start().then(function () { showConnection('З’єднано'); })
        .catch(function () { showConnection('Живі оновлення недоступні. Оновіть сторінку.'); });
})();
