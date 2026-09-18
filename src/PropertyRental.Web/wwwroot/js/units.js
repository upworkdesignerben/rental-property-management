(() => {
  document.addEventListener("modal:success", () => window.location.reload());

  document.addEventListener("submit", async (event) => {
    const form = event.target.closest("[data-delete-unit]");
    if (!form) {
      return;
    }

    event.preventDefault();
    if (!window.confirm(`Delete unit ${form.dataset.unitNumber}?`)) {
      return;
    }

    const response = await fetch(form.action, {
      method: "POST",
      body: new FormData(form),
    });
    if (response.ok) {
      window.location.reload();
      return;
    }

    const payload = await response.json().catch(() => null);
    window.alert(payload?.message ?? "The unit could not be deleted.");
  });
})();
