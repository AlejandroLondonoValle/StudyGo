// ============================================================================
// StudyGo · wwwroot/js/notifications.js
// ============================================================================
document.addEventListener('DOMContentLoaded', () => {
    const notifToggle = document.querySelector('[data-notif-toggle]');
    const notifPanel = document.querySelector('[data-notif-panel]');
    const notifDot = document.querySelector('[data-notif-dot]');

    if (notifToggle && notifPanel) {
        notifToggle.addEventListener('click', async (e) => {
            e.stopPropagation();
            const isHidden = notifPanel.classList.contains('hidden');

            if (isHidden) {
                notifPanel.classList.remove('hidden');
                notifPanel.innerHTML = '<div class="p-4 text-center text-xs text-dark-muted"><i class="fa-solid fa-circle-notch fa-spin"></i> Cargando...</div>';

                try {
                    const response = await fetch('/Notification/GetLatest');
                    const result = await response.json();
                    renderDropdown(result.items, notifPanel);
                    updateBadge(result.unreadCount);
                } catch (err) {
                    notifPanel.innerHTML = '<div class="p-4 text-xs text-red-400">Error al cargar notificaciones.</div>';
                }
            } else {
                notifPanel.classList.add('hidden');
            }
        });

        document.addEventListener('click', (e) => {
            if (!notifPanel.contains(e.target) && e.target !== notifToggle) {
                notifPanel.classList.add('hidden');
            }
        });
    }

    function renderDropdown(items, container) {
        if (!items || items.length === 0) {
            container.innerHTML = '<div class="p-6 text-center text-sm text-dark-muted">Estás al día</div>';
            return;
        }

        let html = '<div class="p-3 border-b border-dark-border eyebrow">Recientes</div><ul class="max-h-64 overflow-y-auto">';
        items.forEach(n => {
            const colorCircle = n.type === "success" ? "bg-brand-mint" : (n.type === "warn" ? "bg-yellow-400" : (n.type === "error" ? "bg-red-400" : "bg-brand-blue"));

            html += `
                <li class="border-b border-dark-border/50 hover:bg-dark-elev transition">
                    <a href="${n.link}" class="flex items-center gap-3 p-3">
                        <div class="h-2 w-2 rounded-full ${colorCircle} shrink-0"></div>
                        <div class="flex-1">
                            <div class="text-xs text-gray-100">${n.message}</div>
                            <div class="text-[10px] text-dark-muted mt-0.5 font-mono">${n.timeRelative}</div>
                        </div>
                    </a>
                </li>`;
        });
        html += '</ul><a href="/Notification" class="block p-2 text-center text-xs text-brand-blue hover:bg-white/5 transition rounded-b-2xl border-t border-dark-border/50">Ver todas</a>';
        container.innerHTML = html;
    }

    function updateBadge(count) {
        if (!notifDot) return;
        if (count > 0) {
            notifDot.classList.remove('hidden');
            notifDot.className = "absolute -top-1 -right-1 h-5 w-5 rounded-full bg-brand-mint text-dark-bg text-[10px] font-bold flex items-center justify-center ring-2 ring-dark-bg";
            notifDot.textContent = count > 9 ? '+9' : count;
        } else {
            notifDot.classList.add('hidden');
        }
    }

    // Solución al refresco de la página: Verifica estado de forma asíncrona al cargar
    async function checkNotificationsOnLoad() {
        try {
            const response = await fetch('/Notification/GetLatest');
            const result = await response.json();
            updateBadge(result.unreadCount);
        } catch (err) {
            console.error("Error al validar campana al inicializar:", err);
        }
    }
    checkNotificationsOnLoad();

    // Conexión en tiempo real por SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notifications")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveNotification", (notification) => {
        if (typeof showToast === "function") {
            showToast(notification.message, notification.type);
        }
        updateBadge(notification.unreadCount);
    });

    connection.start().catch(err => console.error("Error conectando a SignalR: ", err.toString()));
});