(() => {
  const modalElement = document.getElementById("application-modal");
  if (!modalElement) {
    return;
  }

  const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
  const modalContent = modalElement.querySelector(".modal-content");
  let lastTrigger;
  let loading = false;
  const prepareForm = () => {
    const title = modalContent.querySelector(".modal-title");
    if (title) title.id = "application-modal-title";
    if (window.jQuery?.validator?.unobtrusive) {
      window.jQuery.validator.unobtrusive.parse(modalContent);
      const validator = window.jQuery(modalContent.querySelector("form")).data("validator");
      // Keep the submit button stationary when leaving a field with a server error.
      if (validator) validator.settings.onfocusout = false;
    }
  };
  const focusField = () => (modalContent.querySelector(".input-validation-error")
    ?? modalContent.querySelector("input:not([type='hidden']), select, textarea")
    ?? modalContent.querySelector("button") ?? modalElement).focus();
  modalElement.addEventListener("shown.bs.modal", focusField);
  modalElement.addEventListener("hidden.bs.modal", () => lastTrigger?.focus());

  const showError = (message) => {
    const error = modalContent.querySelector("[data-modal-error]");
    if (error) {
      error.textContent = message;
      error.classList.remove("d-none");
    }
  };

  document.addEventListener("click", async (event) => {
    const trigger = event.target.closest("[data-modal-url]");
    if (!trigger || loading) {
      return;
    }

    event.preventDefault();
    loading = true;
    lastTrigger = trigger;
    try {
      const response = await fetch(trigger.dataset.modalUrl, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
      });
      if (!response.ok || response.redirected) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message ?? "The form could not be loaded. Reload or sign in again.");
      }

      modalContent.innerHTML = await response.text();
      prepareForm();
      modal.show();
    } catch (error) {
      window.alert(error.message);
    } finally { loading = false; }
  });

  document.addEventListener("submit", async (event) => {
    const form = event.target.closest("[data-modal-form]");
    if (!form) {
      return;
    }

    event.preventDefault();
    if (form.dataset.saving === "true") return;
    form.dataset.saving = "true";
    const buttons = form.querySelectorAll("button[type='submit']");
    buttons.forEach(button => button.disabled = true);
    try {
      const response = await fetch(form.action, {
        method: "POST",
        body: new FormData(form),
      });
      if (response.status === 400) {
        modalContent.innerHTML = await response.text();
        prepareForm();
        focusField();
        return;
      }
      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        showError(payload?.message ?? "The request could not be completed.");
        return;
      }

      const payload = await response.json();
      if (!payload.success) {
        showError(payload.message ?? "The request could not be completed.");
        return;
      }

      modal.hide();
      document.dispatchEvent(new CustomEvent("modal:success"));
    } catch {
      showError("A network error occurred. Please try again.");
    } finally {
      delete form.dataset.saving;
      buttons.forEach(button => button.disabled = false);
    }
  });
})();
