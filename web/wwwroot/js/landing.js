// Load shared sign-in/sign-out handlers once; ES modules are cached per page.
import "./login.js";

// Improve keyboard navigation for the landing layout's skip link.
const skipLink = document.querySelector(".skip-link");
skipLink?.addEventListener("click", () => {
    const main = document.getElementById("main");
    if (!main) return;
    main.setAttribute("tabindex", "-1");
    main.focus({ preventScroll: true });
});
