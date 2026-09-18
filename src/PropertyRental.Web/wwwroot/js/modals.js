(() => {
  const modalElement = document.getElementById("application-modal");
  if (!modalElement) {
    return;
  }

  const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
  const modalContent = modalElement.querySelector(".modal-content");

  const showError = (message) => {
    const error = modalContent.querySelector("[data-modal-error]");
    if (error) {
      error.textContent = message;
      error.classList.remove("d-none");
    }
  };

  document.addEventListener("click", async (event) => {
    const trigger = event.target.closest("[data-modal-url]");
    if (!trigger) {
      return;
    }

    try {
      const response = await fetch(trigger.dataset.modalUrl, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
      });
      if (!response.ok) {
        throw new Error("The form could not be loaded.");
      }

      modalContent.innerHTML = await response.text();
      modal.show();
    } catch (error) {
      window.alert(error.message);
    }
  });

  document.addEventListener("submit", async (event) => {
    const form = event.target.closest("[data-modal-form]");
    if (!form) {
      return;
    }

    event.preventDefault();
    try {
      const response = await fetch(form.action, {
        method: "POST",
        body: new FormData(form),
      });
      if (response.status === 400) {
        modalContent.innerHTML = await response.text();
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
    }
  });
})();
