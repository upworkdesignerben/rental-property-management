(() => {
  const list = document.getElementById("residence-list");
  const showError = (message) => {
    const alert = document.querySelector("[data-residence-error]");
    if (alert) { alert.textContent = message; alert.classList.remove("d-none"); }
    else window.alert(message);
  };
  const refresh = async () => {
    const response = await fetch(list.dataset.refreshUrl);
    if (!response.ok || response.redirected) throw new Error("Unable to refresh residences. Reload the page.");
    list.innerHTML = await response.text();
  };
  document.addEventListener("modal:success", async () => {
    if (!list) { window.location.reload(); return; }
    try { await refresh(); } catch (error) { showError(error.message); }
  });
  document.addEventListener("submit", (event) => {
    if (event.target.matches("[data-withdraw]") && !window.confirm("Withdraw this application? This cannot be undone.")) event.preventDefault();
  });
})();
