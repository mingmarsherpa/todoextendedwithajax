(() => {
    const root = document.getElementById("app-pop-root");
    const pendingQueue = Array.isArray(window.__pendingPopups) ? window.__pendingPopups : [];

    if (!root) {
        return;
    }

    const iconByTone = {
        success: "bi-check2-circle",
        info: "bi-eye",
        danger: "bi-exclamation-octagon"
    };

    let popupSequence = 0;

    const dismissPopup = (popup) => {
        if (!popup || popup.dataset.leaving === "true") {
            return;
        }

        popup.dataset.leaving = "true";
        popup.classList.add("is-leaving");
        window.setTimeout(() => popup.remove(), 220);
    };

    const show = ({ title, message, tone = "success", duration = 3200 } = {}) => {
        if (!message) {
            return;
        }

        popupSequence += 1;

        const popup = document.createElement("section");
        popup.className = `app-pop app-pop-${tone}`;
        popup.setAttribute("role", "status");
        popup.setAttribute("aria-live", "polite");
        popup.dataset.popupId = `pop-${popupSequence}`;

        const icon = document.createElement("div");
        icon.className = "app-pop-icon";

        const iconGlyph = document.createElement("i");
        iconGlyph.className = `bi ${iconByTone[tone] || iconByTone.info}`;
        icon.setAttribute("aria-hidden", "true");
        icon.appendChild(iconGlyph);

        const copy = document.createElement("div");
        copy.className = "app-pop-copy";

        if (title) {
            const titleElement = document.createElement("div");
            titleElement.className = "app-pop-title";
            titleElement.textContent = title;
            copy.appendChild(titleElement);
        }

        const messageElement = document.createElement("div");
        messageElement.className = "app-pop-message";
        messageElement.textContent = message;
        copy.appendChild(messageElement);

        const closeButton = document.createElement("button");
        closeButton.type = "button";
        closeButton.className = "app-pop-close";
        closeButton.setAttribute("aria-label", "Dismiss notification");
        closeButton.innerHTML = '<i class="bi bi-x-lg" aria-hidden="true"></i>';
        closeButton.addEventListener("click", () => dismissPopup(popup));

        popup.append(icon, copy, closeButton);
        root.appendChild(popup);

        window.requestAnimationFrame(() => {
            popup.classList.add("is-visible");
        });

        if (duration > 0) {
            window.setTimeout(() => dismissPopup(popup), duration);
        }
    };

    window.Pop = { show };
    window.__pendingPopups = [];

    const pagePopup = document.getElementById("app-pop-config");
    if (pagePopup?.dataset.popMessage) {
        show({
            title: pagePopup.dataset.popTitle,
            message: pagePopup.dataset.popMessage,
            tone: pagePopup.dataset.popTone || "success"
        });
    }

    pendingQueue.forEach((popup) => show(popup));

    document.addEventListener("pop:show", (event) => {
        show(event.detail);
    });
})();
