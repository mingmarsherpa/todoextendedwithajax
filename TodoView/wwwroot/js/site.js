(() => {
    const modalElement = document.getElementById("ajaxCrudModal");
    const modalContent = document.getElementById("ajaxCrudModalContent");

    if (!modalElement || !modalContent || !window.bootstrap) {
        return;
    }

    if (modalElement.parentElement !== document.body) {
        document.body.appendChild(modalElement);
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

    const queuePopup = (config) => {
        if (!Array.isArray(window.__pendingPopups)) {
            window.__pendingPopups = [];
        }

        window.__pendingPopups.push(config);
    };

    const showPopup = (config) => {
        if (!config?.message) {
            return;
        }

        if (typeof window.Pop?.show === "function") {
            window.Pop.show(config);
            return;
        }

        queuePopup(config);
        document.dispatchEvent(new CustomEvent("pop:show", { detail: config }));
    };

    const buildRecordLabelFromForm = (form) => {
        const firstName = form.querySelector("[name='Input.FirstName']")?.value?.trim() || "";
        const lastName = form.querySelector("[name='Input.LastName']")?.value?.trim() || "";
        const email = form.querySelector("[name='Input.Email']")?.value?.trim() || "";
        const todoTitle = form.querySelector("[name='Todo.Title']")?.value?.trim() || "";
        const fullName = `${firstName} ${lastName}`.trim();

        return fullName || email || todoTitle || "the item";
    };

    const buildFallbackNotification = (form) => {
        const actionType = form.dataset.successAction;
        const userLabel = form.dataset.successUser || buildRecordLabelFromForm(form);

        if (!actionType) {
            return null;
        }

        const titleMap = {
            create: "User created",
            edit: "User updated",
            delete: "User deleted"
        };

        const verbMap = {
            create: "Created",
            edit: "Updated",
            delete: "Deleted"
        };

        return {
            title: form.dataset.successTitle || titleMap[actionType] || "User updated",
            message: form.dataset.successMessage || `${verbMap[actionType] || "Updated"} ${userLabel}.`,
            tone: form.dataset.successTone || "success"
        };
    };

    const renderRequestError = (message) => {
        const alertMarkup = `
            <div class="alert alert-danger m-3" role="alert">
                ${message}
            </div>`;

        const existingBody = modalContent.querySelector(".modal-body");
        if (existingBody) {
            existingBody.insertAdjacentHTML("afterbegin", alertMarkup);
            return;
        }

        modalContent.innerHTML = alertMarkup;
        modal.show();
        showPopup({
            title: "Request failed",
            message,
            tone: "danger"
        });
    };

    const parseValidation = (form) => {
        if (!form || !window.jQuery?.validator?.unobtrusive) {
            return;
        }

        form.noValidate = true;
        $(form).removeData("validator");
        $(form).removeData("unobtrusiveValidation");
        $.validator.unobtrusive.parse(form);
    };

    const refreshList = async (target, url) => {
        const response = await fetch(url, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        if (!response.ok) {
            throw new Error(`Failed to refresh list: ${response.status}`);
        }

        target.innerHTML = await response.text();
        document.dispatchEvent(new CustomEvent("ajax:content-refreshed", {
            detail: { target, url }
        }));
    };

    const loadModal = async (url, trigger) => {
        const response = await fetch(url, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        if (!response.ok) {
            throw new Error(`Failed to load modal: ${response.status}`);
        }

        modalContent.innerHTML = await response.text();
        parseValidation(modalContent.querySelector("form"));
        modal.show();

        if (trigger?.dataset.popMessage) {
            showPopup({
                title: trigger.dataset.popTitle,
                message: trigger.dataset.popMessage,
                tone: trigger.dataset.popTone || "info"
            });
        }
    };

    document.addEventListener("click", async (event) => {
        const trigger = event.target.closest(".js-ajax-modal-trigger");
        if (!trigger) {
            return;
        }

        event.preventDefault();

        const modalUrl = trigger.dataset.url || trigger.getAttribute("href");
        if (!modalUrl) {
            return;
        }

        try {
            await loadModal(modalUrl, trigger);
        } catch (error) {
            console.error(error);
            showPopup({
                title: "Modal failed",
                message: "The dialog could not be opened. Try again.",
                tone: "danger"
            });
        }
    });

    document.addEventListener("submit", async (event) => {
        const form = event.target.closest(".js-ajax-form");
        if (!form) {
            return;
        }

        event.preventDefault();
        form.noValidate = true;

        const token = form.querySelector("input[name='__RequestVerificationToken']")?.value
            || document.querySelector("input[name='__RequestVerificationToken']")?.value;
        const formData = new FormData(form);

        if (token && !formData.has("__RequestVerificationToken")) {
            formData.append("__RequestVerificationToken", token);
        }

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: formData,
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (!response.ok) {
                throw new Error(`Request failed: ${response.status}`);
            }

            const contentType = response.headers.get("content-type") || "";

            if (contentType.includes("application/json")) {
                const result = await response.json();
                if (!result.success) {
                    return;
                }

                const targetSelector = form.dataset.refreshTarget;
                const refreshTarget = targetSelector ? document.querySelector(targetSelector) : null;
                const refreshUrl = result.reloadUrl || refreshTarget?.dataset.refreshUrl;

                if (refreshTarget && refreshUrl) {
                    await refreshList(refreshTarget, refreshUrl);
                }

                modal.hide();
                showPopup(result.notification || buildFallbackNotification(form));
                return;
            }

            modalContent.innerHTML = await response.text();
            parseValidation(modalContent.querySelector("form"));
        } catch (error) {
            console.error(error);
            renderRequestError("The request could not be completed. Check the server response and try again.");
        }
    }, true);

    modalElement.addEventListener("hidden.bs.modal", () => {
        modalContent.innerHTML = "";
    });
})();
