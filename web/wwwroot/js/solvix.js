// Presentation + Firebase browser authentication. No direct Firestore reads/writes.
let firebasePromise;
async function firebaseClient() {
    if (!firebasePromise) firebasePromise = (async () => {
        const element = document.getElementById("firebase-web-config");
        const config = JSON.parse(element?.textContent || "{}");
        if (!config.apiKey || /^(YOUR_|COPY_)/i.test(config.apiKey) || !config.authDomain || !config.projectId || !config.appId)
            throw new Error("Sign-in is not configured. Set Firebase:Web:ApiKey, AuthDomain and AppId from your Firebase web app settings, then restart the website.");
        const [appSdk, authSdk] = await Promise.all([
            import("https://www.gstatic.com/firebasejs/13.0.0/firebase-app.js"),
            import("https://www.gstatic.com/firebasejs/13.0.0/firebase-auth.js")
        ]);
        const app = appSdk.getApps().length ? appSdk.getApp() : appSdk.initializeApp(config);
        const auth = authSdk.getAuth(app);
        await authSdk.setPersistence(auth, authSdk.browserSessionPersistence);
        return { auth, sdk: authSdk };
    })();
    return firebasePromise;
}
function message(form, text, success = false) {
    const box = form.querySelector("[data-form-message]");
    if (!box) return;
    box.textContent = text; box.hidden = false;
    box.classList.toggle("error", !success); box.classList.toggle("success", success);
}
function busy(form, value) {
    form.querySelectorAll("button").forEach(button => { button.disabled = value; });
    form.setAttribute("aria-busy", String(value));
}
async function post(form) {
    const response = await fetch(form.action, {
        method: "POST", body: new FormData(form), credentials: "same-origin",
        headers: { "Accept": "application/json" }
    });
    const contentType = response.headers.get("content-type") || "";
    if (!contentType.includes("json")) throw new Error("The request could not be completed. Refresh the page and try again.");
    const data = await response.json();
    if (!response.ok) {
        const validation = data.errors ? Object.values(data.errors).flat().join(" ") : "";
        throw new Error(validation || data.detail || data.title || "Request failed.");
    }
    return data;
}
function readable(error) {
    if (error.code?.startsWith("auth/")) {
        const messages = {
            "auth/invalid-api-key": "The Firebase web API key is invalid. Check Firebase:Web:ApiKey in the website configuration.",
            "auth/app-not-authorized": "Firebase rejected this web app. Check its Firebase configuration and API key restrictions.",
            "auth/unauthorized-domain": "This website domain is not authorized. Add it under Firebase Authentication > Settings > Authorized domains.",
            "auth/configuration-not-found": "Firebase Authentication is not configured for this project. Check its Authentication settings.",
            "auth/user-disabled": "This account is disabled. Contact your administrator.",
            "auth/invalid-email": "Enter a valid email address.",
            "auth/web-storage-unsupported": "Browser storage is unavailable. Allow site storage and try again.",
            "auth/invalid-credential": "Sign-in failed. Check your email and password.",
            "auth/wrong-password": "Sign-in failed. Check your email and password.",
            "auth/user-not-found": "Sign-in failed. Check your email and password.",
            "auth/weak-password": "Choose a stronger password that meets your organisation's policy.",
            "auth/popup-closed-by-user": "The sign-in window was closed. Please try again.",
            "auth/popup-blocked": "Allow the sign-in popup and try again.",
            "auth/too-many-requests": "Too many attempts. Please wait and try again.",
            "auth/network-request-failed": "Check your connection and try again.",
            "auth/operation-not-allowed": "This sign-in method has not been enabled by your administrator.",
            "auth/account-exists-with-different-credential": "Use the sign-in method already linked to this email."
        };
        // Show only a bounded SDK code, never provider responses, credentials or tokens.
        const code = /^auth\/[a-z0-9-]{1,80}$/.test(error.code) ? error.code : "auth/unknown";
        return messages[code] || `Firebase sign-in failed (${code}). Contact your administrator with this code.`;
    }
    return error.message || "The request could not be completed.";
}
function redirectLocal(path) {
    const target = new URL(path || "/Dashboard", window.location.origin);
    if (target.origin !== window.location.origin) throw new Error("Invalid redirect destination.");
    window.location.assign(target.href);
}
for (const form of document.querySelectorAll("[data-account]")) {
    // Preload the provider modules without signing in automatically.
    firebaseClient().catch(error => message(form, readable(error)));
    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (!form.reportValidity()) return;
        const mode = form.dataset.account;
        const email = form.querySelector("#auth-email").value.trim();
        const password = form.querySelector("#auth-password").value;
        if (mode === "register" && password !== form.querySelector("#auth-confirm").value) {
            message(form, "The passwords do not match."); return;
        }
        busy(form, true);
        try {
            const {auth, sdk} = await firebaseClient();
            let credential;
            if (mode === "register") {
                try { credential = await sdk.createUserWithEmailAndPassword(auth, email, password); }
                catch (error) {
                    // Complete a profile after a previous successful Firebase signup.
                    // Existing account ownership is proved by a fresh password sign-in.
                    if (error.code !== "auth/email-already-in-use") throw error;
                    credential = await sdk.signInWithEmailAndPassword(auth, email, password);
                }
            } else credential = await sdk.signInWithEmailAndPassword(auth, email, password);
            form.elements.namedItem("IdToken").value = await credential.user.getIdToken();
            const result = await post(form);
            if (mode === "login") redirectLocal(result.redirectUrl);
            else {
                message(form, result.message || "Profile created. Please wait for administrator approval.", true);
                form.querySelectorAll("input[type=password]").forEach(input => {input.value = "";});
            }
        } catch (error) { message(form, readable(error)); }
        finally { form.elements.namedItem("IdToken").value = ""; busy(form, false); }
    });
    for (const button of form.querySelectorAll("[data-provider]")) {
        button.addEventListener("click", async () => {
            busy(form, true);
            try {
                const {auth, sdk} = await firebaseClient();
                const provider = button.dataset.provider === "google" ? new sdk.GoogleAuthProvider() : new sdk.OAuthProvider("microsoft.com");
                provider.setCustomParameters({prompt: "select_account"});
                const credential = await sdk.signInWithPopup(auth, provider);
                form.elements.namedItem("IdToken").value = await credential.user.getIdToken();
                const result = await post(form); redirectLocal(result.redirectUrl);
            } catch (error) { message(form, readable(error)); }
            finally { form.elements.namedItem("IdToken").value = ""; busy(form, false); }
        });
    }
}
for (const form of document.querySelectorAll("[data-logout]")) {
    form.addEventListener("submit", async event => {
        event.preventDefault(); busy(form, true);
        try {
            const result = await post(form); // Clear server cookie even if Firebase is unavailable.
            try {
                const {auth, sdk} = await firebaseClient(); await sdk.signOut(auth);
            } catch {
                message(form, "Website session cleared. Browser sign-out failed; close this tab and sign out of your Firebase session before using a shared device.");
                return;
            }
            redirectLocal(result.redirectUrl);
        } catch (error) { message(form, readable(error)); }
        finally { busy(form, false); }
    });
}
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
