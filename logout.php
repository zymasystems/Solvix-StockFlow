<?php
// logout.php - Destroy PHP session and sign out Firebase client
if (session_status() === PHP_SESSION_NONE) {
    session_start();
}

// Clear all session variables
$_SESSION = [];

// Destroy session cookie when present
if (ini_get('session.use_cookies')) {
    $params = session_get_cookie_params();
    setcookie(session_name(), '', time() - 42000,
        $params['path'], $params['domain'],
        $params['secure'], $params['httponly']
    );
}

// Destroy the session
session_destroy();
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Signing Out...</title>
    <script src="https://www.gstatic.com/firebasejs/9.22.2/firebase-app-compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/9.22.2/firebase-auth-compat.js"></script>
</head>
<body style="font-family: Inter, sans-serif; margin: 0; min-height: 100vh; display: flex; align-items: center; justify-content: center; background: #f8fafc; color: #111;">
    <div style="text-align: center; padding: 28px 32px; border-radius: 24px; background: white; box-shadow: 0 16px 40px rgba(15, 23, 42, 0.12);">
        <h1 style="font-size: 1.25rem; margin-bottom: 12px;">Signing out...</h1>
        <p style="color: #475569; margin-bottom: 16px;">This may take a moment. You will be redirected to the login page automatically.</p>
    </div>
    <script>
        const firebaseConfig = {
            apiKey: "AIzaSyDcVI400ryKiI1zRpMjonnAWZgdkvoB9vk",
            authDomain: "inventory-system-7255c.firebaseapp.com",
            projectId: "inventory-system-7255c",
            storageBucket: "inventory-system-7255c.firebasestorage.app",
            messagingSenderId: "226148353453",
            appId: "1:226148353453:web:161bd324c4874795cdcd24",
            measurementId: "G-X49L9CC8TD"
        };
        firebase.initializeApp(firebaseConfig);
        const auth = firebase.auth();
        auth.signOut().catch(console.error).finally(() => {
            window.location.href = './login.php';
        });
    </script>
</body>
</html>
