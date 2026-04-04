(() => {
    const container = document.getElementById("todo-list-container");

    if (!container) {
        return;
    }

    const scheduledTimers = new Map();
    const activeRequests = new Set();
    let refreshInFlight = false;
    let pollTimerId = null;
    let deliveredReminderKeys = new Set();
    const pollIntervalMs = 15000;

    const buildReminderKey = (reminder) => `${reminder.id}:${reminder.reminderAt}`;

    const clearScheduledTimers = () => {
        scheduledTimers.forEach((timerId) => window.clearTimeout(timerId));
        scheduledTimers.clear();
    };

    const stopPolling = () => {
        if (pollTimerId !== null) {
            window.clearInterval(pollTimerId);
            pollTimerId = null;
        }
    };

    const getRequestToken = () => document.querySelector("input[name='__RequestVerificationToken']")?.value || "";

    const readReminderState = () => {
        const configNode = container.querySelector("#todo-reminder-config");
        const dataNode = container.querySelector("#todo-reminder-data");

        if (!configNode || !dataNode) {
            return { triggerUrl: "", reminders: [] };
        }

        try {
            return {
                triggerUrl: configNode.dataset.triggerUrl || "",
                reminders: JSON.parse(dataNode.textContent || "[]")
            };
        } catch (error) {
            console.error(error);
            return { triggerUrl: "", reminders: [] };
        }
    };

    const showReminderPopup = (notification, fallbackTitle) => {
        const config = notification || {
            title: "Reminder",
            message: `"${fallbackTitle}" is due now.`,
            tone: "info",
            duration: 5000
        };

        if (typeof window.Pop?.show === "function") {
            window.Pop.show(config);
            return;
        }

        document.dispatchEvent(new CustomEvent("pop:show", { detail: config }));
    };

    const refreshList = async () => {
        const refreshUrl = container.dataset.refreshUrl;

        if (!refreshUrl || refreshInFlight) {
            return;
        }

        refreshInFlight = true;

        try {
            const response = await fetch(refreshUrl, {
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (!response.ok) {
                throw new Error(`Failed to refresh reminders: ${response.status}`);
            }

            container.innerHTML = await response.text();
            document.dispatchEvent(new CustomEvent("ajax:content-refreshed", {
                detail: { target: container, url: refreshUrl }
            }));
        } catch (error) {
            console.error(error);
        } finally {
            refreshInFlight = false;
        }
    };

    const startPolling = () => {
        if (pollTimerId !== null) {
            return;
        }

        pollTimerId = window.setInterval(() => {
            if (document.hidden || activeRequests.size > 0) {
                return;
            }

            void refreshList();
        }, pollIntervalMs);
    };

    const triggerReminder = async (reminder, triggerUrl) => {
        const reminderKey = buildReminderKey(reminder);

        if (!triggerUrl || deliveredReminderKeys.has(reminderKey) || activeRequests.has(reminder.id)) {
            return;
        }

        activeRequests.add(reminder.id);

        const formData = new FormData();
        const token = getRequestToken();

        formData.append("id", reminder.id);

        if (token) {
            formData.append("__RequestVerificationToken", token);
        }

        try {
            const response = await fetch(triggerUrl, {
                method: "POST",
                body: formData,
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (!response.ok) {
                throw new Error(`Failed to trigger reminder: ${response.status}`);
            }

            const result = await response.json();
            if (!result.success) {
                await refreshList();
                return;
            }

            deliveredReminderKeys.add(reminderKey);
            showReminderPopup(result.notification, reminder.title);
            await refreshList();
        } catch (error) {
            console.error(error);
        } finally {
            activeRequests.delete(reminder.id);
        }
    };

    const scheduleReminder = (reminder, triggerUrl) => {
        const reminderKey = buildReminderKey(reminder);

        if (deliveredReminderKeys.has(reminderKey)) {
            return;
        }

        const reminderTime = new Date(reminder.reminderAt);
        const delay = reminderTime.getTime() - Date.now();

        if (Number.isNaN(reminderTime.getTime())) {
            return;
        }

        if (delay <= 0) {
            void triggerReminder(reminder, triggerUrl);
            return;
        }

        const timerId = window.setTimeout(() => {
            scheduledTimers.delete(reminderKey);
            void triggerReminder(reminder, triggerUrl);
        }, Math.min(delay, 2147483647));

        scheduledTimers.set(reminderKey, timerId);
    };

    const syncReminders = () => {
        clearScheduledTimers();

        const { triggerUrl, reminders } = readReminderState();

        if (reminders.length > 0) {
            startPolling();
        } else {
            stopPolling();
        }

        reminders.forEach((reminder) => scheduleReminder(reminder, triggerUrl));
    };

    document.addEventListener("ajax:content-refreshed", (event) => {
        if (event.detail?.target?.id !== "todo-list-container") {
            return;
        }

        deliveredReminderKeys = new Set();
        syncReminders();
    });

    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) {
            syncReminders();
            void refreshList();
            return;
        }

        stopPolling();
    });

    syncReminders();
})();
