// Presentation + Firebase browser authentication. No direct Firestore reads/writes.
let firebasePromise;
async function firebaseClient() {
    if (!firebasePromise) firebasePromise = (async () => {
        const element = document.getElementById("firebase-web-config");
        const config = JSON.parse(element?.textContent || "{}");
        if (!config.apiKey || !config.authDomain || !config.projectId || !config.appId)
            throw new Error("Sign-in is not configured. Ask the administrator to check Firebase web settings.");
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
export function busy(form, value) {
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
        return messages[error.code] || "Firebase sign-in could not be completed. Try again or contact your administrator.";
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
