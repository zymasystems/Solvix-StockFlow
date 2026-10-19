import { busy } from "./login.js";

for (const form of document.querySelectorAll("form[data-confirm], form[data-save-form]")) {
    form.addEventListener("submit", event => {
        if (form.dataset.confirm && !window.confirm(form.dataset.confirm)) { event.preventDefault(); return; }
        if (!form.checkValidity()) return;
        // Disable only buttons; posted fields, antiforgery and RequestId remain intact.
        busy(form, true);
    });
}
window.addEventListener("pageshow", () => document.querySelectorAll("form").forEach(form => busy(form, false)));
document.querySelector("[data-menu-toggle]")?.addEventListener("click", event => {
    const button = event.currentTarget;
    const open = document.getElementById("sidebar").classList.toggle("open");
    button.setAttribute("aria-expanded", String(open));
});
document.addEventListener("keydown", event => {
    if(event.key === "Escape") {document.getElementById("sidebar")?.classList.remove("open");document.querySelector("[data-menu-toggle]")?.setAttribute("aria-expanded", "false");}
});
document.querySelector("[data-print]")?.addEventListener("click", () => window.print());
const stockType = document.querySelector("[data-stock-type]");
function updateStockHelp() {
    if (!stockType) return;
    const input = document.querySelector("[data-stock-quantity]");
    input.min = stockType.value === "Set" ? "0" : "1";
    document.querySelector("[data-stock-help]").textContent = stockType.value === "Set"
        ? "Set replaces the current stock with this exact quantity. Zero is allowed."
        : stockType.value === "Add" ? "Add increases the current stock by this quantity." : "Subtract reduces the current stock by this quantity. Stock cannot become negative.";
}
stockType?.addEventListener("change", updateStockHelp); updateStockHelp();
