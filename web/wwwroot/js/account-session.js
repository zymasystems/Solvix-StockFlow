// Works with your existing Firebase compat SDK `auth` instance.
// Load AFTER the Firebase SDK has been initialised. Replace old verify-login.php calls.
async function postAccountForm(url, form) {
    const response = await fetch(url, { method: "POST", body: new FormData(form), credentials: "same-origin" });
    const data = await response.json();
    if (!response.ok) {
        const errors = data.errors ? Object.values(data.errors).flat().join(" ") : "";
        throw new Error(errors || data.detail || data.title || "Request failed.");
    }
    return data;
}
window.solvixAccount = {
    async login(auth, form) {
        if (!auth.currentUser) throw new Error("Sign in with Firebase first.");
        form.elements.namedItem("IdToken").value = await auth.currentUser.getIdToken();
        const data = await postAccountForm("/Account/Login", form);
        window.location.assign(data.redirectUrl);
    },
    async register(auth, form) {
        if (!auth.currentUser) throw new Error("Create your Firebase account first.");
        form.elements.namedItem("IdToken").value = await auth.currentUser.getIdToken();
        // Display returned message as textContent; do not automatically sign in again.
        return await postAccountForm("/Account/Register", form);
    },
    async logout(auth, form) {
        const data = await postAccountForm("/Account/Logout", form);
        // If Firebase sign-out fails, surface the error instead of an automatic-login loop.
        await auth.signOut();
        window.location.assign(data.redirectUrl);
    }
};
