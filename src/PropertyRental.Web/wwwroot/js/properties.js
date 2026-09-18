(() => {
  document.addEventListener("modal:success", () => window.location.reload());

  document.addEventListener("submit", async (event) => {
    const form = event.target.closest("[data-delete-property]");
    if (!form) {
      return;
    }

    event.preventDefault();
    if (!window.confirm(`Delete ${form.dataset.propertyName}?`)) {
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
    window.alert(payload?.message ?? "The property could not be deleted.");
  });
})();
