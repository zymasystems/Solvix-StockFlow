
// Firebase configuration (replace with your own)
const firebaseConfig = {
  apiKey: "AIzaSyDcVI400ryKiI1zRpMjonnAWZgdkvoB9vk",
  authDomain: "inventory-system-7255c.firebaseapp.com",
  projectId: "inventory-system-7255c",
  storageBucket: "inventory-system-7255c.firebasestorage.app",
  messagingSenderId: "226148353453",
  appId: "1:226148353453:web:161bd324c4874795cdcd24",
  measurementId: "G-X49L9CC8TD"
};

// Initialize Firebase
firebase.initializeApp(firebaseConfig);
const auth = firebase.auth();
const db = firebase.firestore();
const providerGoogle = new firebase.auth.GoogleAuthProvider();
const providerMicrosoft = new firebase.auth.OAuthProvider('microsoft.com');

function normalizeRole(role) {
    return String(role || '').trim().toLowerCase();
}

function redirectByRole(role, adminPath = 'admin-dashboard.php', staffPath = 'staff-dashboard.php') {
    const normalized = normalizeRole(role);
    window.location.href = normalized === 'admin' ? adminPath : staffPath;
}

// Tab Switching
function switchTab(tab) {
    const loginPanel = document.getElementById('loginPanel');
    const registerPanel = document.getElementById('registerPanel');
    const loginTab = document.getElementById('loginTab');
    const registerTab = document.getElementById('registerTab');
    const indicator = document.getElementById('tabIndicator');

    clearAllErrors();

    if (tab === 'login') {
        loginPanel.className = 'form-panel active-panel';
        registerPanel.className = 'form-panel hidden-panel';
        loginTab.classList.replace('text-surface-400', 'text-surface-900');
        registerTab.classList.replace('text-surface-900', 'text-surface-400');
        indicator.style.transform = 'translateX(0)';
    } else {
        registerPanel.className = 'form-panel active-panel';
        loginPanel.className = 'form-panel hidden-panel';
        registerTab.classList.replace('text-surface-400', 'text-surface-900');
        loginTab.classList.replace('text-surface-900', 'text-surface-400');
        indicator.style.transform = 'translateX(100%)';
    }
}

// Password Toggle
function togglePassword(inputId, btn) {
    const input = document.getElementById(inputId);
    const icon = btn.querySelector('i');
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.replace('fa-eye', 'fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.replace('fa-eye-slash', 'fa-eye');
    }
}

// Password Strength Checker
function checkPasswordStrength(password) {
    const container = document.getElementById('strengthContainer');
    const bars = [document.getElementById('str1'), document.getElementById('str2'), document.getElementById('str3'), document.getElementById('str4')];
    const text = document.getElementById('strengthText');

    if (password.length === 0) {
        container.classList.add('hidden');
        return;
    }

    container.classList.remove('hidden');

    let score = 0;
    if (password.length >= 8) score++;
    if (/[a-z]/.test(password) && /[A-Z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[^a-zA-Z0-9]/.test(password)) score++;

    const levels = [
        { color: '#ef4444', label: 'Weak — add uppercase, numbers, symbols' },
        { color: '#f97316', label: 'Fair — try adding more variety' },
        { color: '#eab308', label: 'Good — almost there' },
        { color: '#22c55e', label: 'Strong — excellent password' }
    ];

    bars.forEach((bar, i) => {
        if (i < score) {
            bar.style.backgroundColor = levels[score - 1].color;
        } else {
            bar.style.backgroundColor = '#e2e8f0';
        }
    });

    text.textContent = levels[Math.max(0, score - 1)].label;
    text.style.color = levels[Math.max(0, score - 1)].color;
}

// Validation Helpers
function showError(id, message) {
    const el = document.getElementById(id);
    el.textContent = message;
    el.classList.remove('hidden');
}

function hideError(id) {
    const el = document.getElementById(id);
    el.classList.add('hidden');
}

function clearAllErrors() {
    document.querySelectorAll('[id$="Error"]').forEach(el => el.classList.add('hidden'));
}

function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function setLoading(btnId, textId, iconId, spinnerId, isLoading) {
    const btn = document.getElementById(btnId);
    const text = document.getElementById(textId);
    const icon = document.getElementById(iconId);
    const spinner = document.getElementById(spinnerId);

    if (isLoading) {
        btn.disabled = true;
        btn.style.opacity = '0.8';
        text.textContent = 'Please wait...';
        if (icon) icon.classList.add('hidden');
        if (spinner) spinner.classList.remove('hidden');
    } else {
        btn.disabled = false;
        btn.style.opacity = '1';
        text.textContent = btnId === 'loginBtn' ? 'Sign In' : 'Create Account';
        if (icon) icon.classList.remove('hidden');
        if (spinner) spinner.classList.add('hidden');
    }
}

async function parseApiResponse(response) {
    const text = await response.text();
    if (!text) {
        throw new Error('Empty response from server.');
    }
    try {
        return JSON.parse(text);
    } catch (error) {
        throw new Error('Invalid JSON from server: ' + text);
    }
}

async function saveUserToFirestore(user, extras = {}) {
    try {
        if (!user || !user.uid) return null;
        const userRef = db.collection('users').doc(user.uid);
        const snap = await userRef.get();
        const exists = snap.exists;

        const payload = {
            uid: user.uid,
            email: user.email || null,
            displayName: user.displayName || extras.name || '',
            role: extras.role || 'staff',
            photoURL: user.photoURL || null,
            lastLogin: firebase.firestore.FieldValue.serverTimestamp(),
        };

        if (!exists) {
            payload.createdAt = firebase.firestore.FieldValue.serverTimestamp();
        }

        await userRef.set(payload, { merge: true });
        return payload;
    } catch (err) {
        console.error('Failed to save user to Firestore', err);
        return null;
    }
}

// Handle Login with Firebase
async function handleLogin(e) {
    e.preventDefault();
    clearAllErrors();

    const email = document.getElementById('loginEmail').value.trim();
    const password = document.getElementById('loginPassword').value;
    let valid = true;

    if (!email) {
        showError('loginEmailError', 'Email address is required');
        valid = false;
    } else if (!isValidEmail(email)) {
        showError('loginEmailError', 'Please enter a valid email address');
        valid = false;
    }

    if (!password) {
        showError('loginPasswordError', 'Password is required');
        valid = false;
    }

    if (!valid) return;

    setLoading('loginBtn', 'loginBtnText', 'loginBtnIcon', 'loginBtnSpinner', true);

    try {
        const userCredential = await auth.signInWithEmailAndPassword(email, password);
        const user = userCredential.user;
        const idToken = await user.getIdToken();

        // Save/update user in Firestore (client-side)
        await saveUserToFirestore(user);

        // Verify ID token on server and start PHP session
        const form = new FormData();
        form.append('idToken', idToken);
        form.append('name', user.displayName || '');

        const response = await fetch('./verify-login.php', {
            method: 'POST',
            body: form,
            credentials: 'include',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        const result = await parseApiResponse(response);
        if (result.success) {
            const target = normalizeRole(result.role) === 'admin' ? './admin-dashboard.php' : './staff-dashboard.php';
            window.location.href = target;
        } else {
            showToast(result.message || 'Session creation failed', 'error');
        }
    } catch (error) {
        let message = 'Login failed. ';
        switch (error.code) {
            case 'auth/user-not-found':
                message = 'No account found with this email.';
                break;
            case 'auth/wrong-password':
                message = 'Incorrect password.';
                break;
            case 'auth/invalid-email':
                message = 'Invalid email format.';
                break;
            default:
                message = error.message;
        }
        showError('loginEmailError', message);
        setLoading('loginBtn', 'loginBtnText', 'loginBtnIcon', 'loginBtnSpinner', false);
    }
}

// Handle Registration with Firebase
async function handleRegister(e) {
    e.preventDefault();
    clearAllErrors();

    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();
    const email = document.getElementById('registerEmail').value.trim();
    const company = document.getElementById('companyName').value.trim();
    const role = document.getElementById('userRole').value;
    const password = document.getElementById('registerPassword').value;
    const confirm = document.getElementById('confirmPassword').value;
    const agreed = document.getElementById('agreeTerms').checked;

    let valid = true;

    if (!firstName) { 
            showError('firstNameError', 'First name is required'); 
            valid = false; 
        }
    if (!lastName) {
         showError('lastNameError', 'Last name is required'); 
         valid = false; 
        }
    if (!email) { 
        showError('registerEmailError', 'Work email is required'); 
        valid = false;
     } else if (!isValidEmail(email)) { 
        showError('registerEmailError', 'Please enter a valid email address');
         valid = false; 
        }
    if (!company) { 
        showError('companyNameError', 'Company name is required'); 
        valid = false; 
    }
    if (!password) {
         showError('registerPasswordError', 'Password is required'); 
         valid = false;
         }
    else if (password.length < 8) { 
        showError('registerPasswordError', 'Password must be at least 8 characters'); 
        valid = false; 
    }
    if (!confirm) {
        showError('confirmPasswordError', 'Please confirm your password'); 
        valid = false; 
    }
    else if (password !== confirm) { 
        showError('confirmPasswordError', 'Passwords do not match'); 
        valid = false; 
    }
    if (!agreed) {
         showError('agreeTermsError', 'You must agree to the terms'); 
         valid = false; 
        }

    if (!valid) return;

    setLoading('registerBtn', 'registerBtnText', 'registerBtnIcon', 'registerBtnSpinner', true);

    try {
        // Step 1: Create Firebase User
        const userCredential = await auth.createUserWithEmailAndPassword(email, password);
        const user = userCredential.user;

        // Step 2: Update display name
        await user.updateProfile({ displayName: `${firstName} ${lastName}` });

        // Step 3: Get ID Token
        const idToken = await user.getIdToken();

        // Step 4: Send ALL data to PHP in ONE request (This is key)
        const form = new FormData();
        form.append('idToken', idToken);
        form.append('firstName', firstName);
        form.append('lastName', lastName);
        form.append('company', company);
        form.append('role', role);

        const response = await fetch('./verify-login.php', {
            method: 'POST',
            body: form,
            credentials: 'include'
        });

        const data = await parseApiResponse(response);

        if (data.success) {
            showToast('Account created successfully! Redirecting...', 'success');
            
            const target = normalizeRole(data.role) === 'admin' 
                ? './admin-dashboard.php' 
                : './staff-dashboard.php';

            // Faster redirect
            setTimeout(() => {
                window.location.href = target;
            }, 900);

        } else {
            throw new Error(data.message || 'Failed to create session');
        }

    } catch (error) {
        console.error(error);
        
        let message = error.message || 'Registration failed';
        
        if (error.code === 'auth/email-already-in-use') {
            message = 'This email is already registered.';
        } else if (error.code === 'auth/weak-password') {
            message = 'Password is too weak. Try a stronger password.';
        }

        showError('registerEmailError', message);
        showToast(message, 'error');

    } finally {
        // Always reset loading state
        setLoading('registerBtn', 'registerBtnText', 'registerBtnIcon', 'registerBtnSpinner', false);
    }
}

// Social Login: Google
async function signInWithGoogle() {
    try {
        const result = await auth.signInWithPopup(providerGoogle);
        const user = result.user;
        const idToken = await user.getIdToken();

        await saveUserToFirestore(user);

        const form = new FormData();
        form.append('idToken', idToken);

        const response = await fetch('./verify-login.php', {
            method: 'POST',
            body: form,
            credentials: 'include',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
        const data = await parseApiResponse(response);
        if (data.success) {
            showToast('Signed in with Google! Redirecting...', 'success');
            const target = normalizeRole(data.role) === 'admin' ? './admin-dashboard.php' : './staff-dashboard.php';
            setTimeout(() => window.location.href = target, 1500);
        }
    } catch (error) {
        showToast('Google sign-in failed: ' + error.message, 'error');
    }
}

// Social Login: Microsoft
async function signInWithMicrosoft() {
    try {
        const result = await auth.signInWithPopup(providerMicrosoft);
        const user = result.user;
        const idToken = await user.getIdToken();

        await saveUserToFirestore(user);

        const form = new FormData();
        form.append('idToken', idToken);

        const response = await fetch('./verify-login.php', {
            method: 'POST',
            body: form,
            credentials: 'include',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
        const data = await parseApiResponse(response);
        if (data.success) {
            showToast('Signed in with Microsoft! Redirecting...', 'success');
            const target = normalizeRole(data.role) === 'admin' ? './admin-dashboard.php' : './staff-dashboard.php';
            setTimeout(() => window.location.href = target, 1500);
        }
    } catch (error) {
        showToast('Microsoft sign-in failed: ' + error.message, 'error');
    }
}

// Toast Notification System
function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');

    const icons = {
        success: 'fa-circle-check',
        error: 'fa-circle-xmark',
        info: 'fa-circle-info',
        warning: 'fa-triangle-exclamation'
    };

    const colors = {
        success: 'bg-emerald-50 border-emerald-200 text-emerald-800',
        error: 'bg-red-50 border-red-200 text-red-800',
        info: 'bg-brand-50 border-brand-200 text-brand-800',
        warning: 'bg-amber-50 border-amber-200 text-amber-800'
    };

    const iconColors = {
        success: 'text-emerald-500',
        error: 'text-red-500',
        info: 'text-brand-500',
        warning: 'text-amber-500'
    };

    toast.className = `toast flex items-start gap-3 px-4 py-3 rounded-xl border shadow-lg ${colors[type]} max-w-sm`;
    toast.innerHTML = `
        <i class="fa-solid ${icons[type]} ${iconColors[type]} mt-0.5 text-sm flex-shrink-0"></i>
        <p class="text-sm font-medium leading-relaxed">${message}</p>
        <button onclick="dismissToast(this)" class="ml-auto flex-shrink-0 opacity-60 hover:opacity-100 transition-opacity">
            <i class="fa-solid fa-xmark text-xs"></i>
        </button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        if (toast.parentNode) {
            toast.classList.add('toast-exit');
            setTimeout(() => toast.remove(), 300);
        }
    }, 4500);
}

function dismissToast(btn) {
    const toast = btn.closest('.toast');
    toast.classList.add('toast-exit');
    setTimeout(() => toast.remove(), 300);
}

// Keyboard accessibility for tabs
document.addEventListener('keydown', function(e) {
    if (e.key === 'Enter' && document.activeElement.id === 'loginTab') {
        switchTab('login');
    }
    if (e.key === 'Enter' && document.activeElement.id === 'registerTab') {
        switchTab('register');
    }
});

// Add subtle input focus animations
document.querySelectorAll('input').forEach(input => {
    input.addEventListener('focus', function() {
        this.closest('.input-group')?.querySelector('.input-icon')?.classList.add('text-brand-500');
    });
    input.addEventListener('blur', function() {
        this.closest('.input-group')?.querySelector('.input-icon')?.classList.remove('text-brand-500');
    });
});

// Initialize: check if already logged in
auth.onAuthStateChanged(async (user) => {
    const params = new URLSearchParams(window.location.search);
    const logoutFlow = params.has('logout');

    if (user) {
        if (logoutFlow) {
            await auth.signOut();
            window.history.replaceState({}, document.title, window.location.pathname);
            return;
        }
        // Ensure server session exists and get role for proper redirect.
        try {
            const idToken = await user.getIdToken();

            // Ensure Firestore user exists/updated
            await saveUserToFirestore(user);

            const form = new FormData();
            form.append('idToken', idToken);

            const response = await fetch('./verify-login.php', {
                method: 'POST',
                body: form,
                credentials: 'include',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const data = await parseApiResponse(response);
            if (data.success) {
                if (data.role === 'admin') {
                    window.location.href = './admin-dashboard.php';
                } else {
                    window.location.href = './staff-dashboard.php';
                }
            } else {
                // Could not create session; sign out client to allow manual login
                await auth.signOut();
            }
        } catch (err) {
            console.error('Session sync failed:', err);
            await auth.signOut();
        }
    }
});