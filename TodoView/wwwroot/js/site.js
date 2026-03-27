(() => {
    const modalElement = document.getElementById("todoCrudModal");
    const modalContent = document.getElementById("todoCrudModalContent");

    if (!modalElement || !modalContent || !window.bootstrap) {
        return;
    }

    if (modalElement.parentElement !== document.body) {
        document.body.appendChild(modalElement);
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

    const parseValidation = (form) => {
        if (!form || !window.jQuery?.validator?.unobtrusive) {
            return;
        }

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
            throw new Error(`Failed to refresh todo list: ${response.status}`);
        }

        target.innerHTML = await response.text();
    };

    const loadModal = async (url) => {
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
    };

    document.addEventListener("click", async (event) => {
        const trigger = event.target.closest(".js-todo-modal-trigger");
        if (!trigger) {
            return;
        }

        event.preventDefault();

        try {
            await loadModal(trigger.dataset.url);
        } catch (error) {
            console.error(error);
        }
    });

    document.addEventListener("submit", async (event) => {
        const form = event.target.closest(".js-todo-ajax-form");
        if (!form) {
            return;
        }

        event.preventDefault();

        if (window.jQuery && typeof $(form).valid === "function" && !$(form).valid()) {
            return;
        }

        const token = form.querySelector("input[name='__RequestVerificationToken']")?.value
            || document.querySelector("input[name='__RequestVerificationToken']")?.value;
        const formData = new FormData(form);

        if (token && !formData.has("__RequestVerificationToken")) {
            formData.append("__RequestVerificationToken", token);
        }

        const response = await fetch(form.action, {
            method: "POST",
            body: formData,
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

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
            return;
        }

        modalContent.innerHTML = await response.text();
        parseValidation(modalContent.querySelector("form"));
    });

    modalElement.addEventListener("hidden.bs.modal", () => {
        modalContent.innerHTML = "";
    });
})();
