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
            await loadModal(modalUrl);
        } catch (error) {
            console.error(error);
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
