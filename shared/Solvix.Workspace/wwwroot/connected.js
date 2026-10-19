window.connectedUi = {
  atTop: () => window.scrollY <= 5,
  print: () => window.print(),
  signOut: () => document.querySelector('[data-logout]')?.requestSubmit(),
  changeEmail: async (currentEmail, newEmail, password) => {
    const config = JSON.parse(document.getElementById('firebase-web-config').textContent);
    const app = await import('https://www.gstatic.com/firebasejs/13.0.0/firebase-app.js');
    const sdk = await import('https://www.gstatic.com/firebasejs/13.0.0/firebase-auth.js');
    const auth = sdk.getAuth(app.getApps().length ? app.getApp() : app.initializeApp(config));
    const user = (await sdk.signInWithEmailAndPassword(auth, currentEmail, password)).user;
    await sdk.verifyBeforeUpdateEmail(user, newEmail);
    window.connectedUi.signOut();
  }
};
