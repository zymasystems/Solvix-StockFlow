<?php
// login.php - No authentication required
session_start();
if (isset($_SESSION['user_id'])) {
    $redirect = isset($_SESSION['role']) && $_SESSION['role'] === 'admin'
        ? './admin-dashboard.php'
        : './staff-dashboard.php';
    header('Location: ' . $redirect);
    exit;
}
$pageTitle = 'Sign In | Solvix StockFlow';
// We won't use the global header/footer for this page to keep the design clean
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title><?php echo $pageTitle; ?></title>
    <meta name="description" content="Sign in to your Solvix StockFlow account">
    
    <!-- Tailwind CSS CDN -->
    <script src="https://cdn.tailwindcss.com"></script>
    <!-- Custom Tailwind config override -->
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    fontFamily: {
                        display: ['Space Grotesk', 'sans-serif'],
                        body: ['Inter', 'sans-serif'],
                    },
                    colors: {
                        brand: {
                            50: '#eef5ff',
                            100: '#d9e8ff',
                            200: '#bcd8ff',
                            300: '#8ec0ff',
                            400: '#599dff',
                            500: '#3378ff',
                            600: '#1b57f5',
                            700: '#1443e1',
                            800: '#1736b6',
                            900: '#19338f',
                            950: '#142157',
                        },
                        surface: {
                            50: '#f8fafc',
                            100: '#f1f5f9',
                            200: '#e2e8f0',
                            300: '#cbd5e1',
                            400: '#94a3b8',
                            500: '#64748b',
                            600: '#475569',
                            700: '#334155',
                            800: '#1e293b',
                            900: '#0f172a',
                            950: '#020617',
                        }
                    }
                }
            }
        }
    </script>
    <!-- Font Awesome -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
    <!-- Google Fonts -->
    <link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <!-- Custom CSS -->
    <link rel="stylesheet" href="./assets/css/login.css">
    <script>
        window.sessionActive = <?php echo json_encode(isset($_SESSION['user_id'])); ?>;
    </script>
    <!-- Firebase SDK -->
    <script src="https://www.gstatic.com/firebasejs/10.14.1/firebase-app-compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/10.14.1/firebase-auth-compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/10.14.1/firebase-firestore-compat.js"></script>
</head>
<body class="bg-surface-50 antialiased">
    <a href="./index.php" class="absolute top-4 left-4 z-20 inline-flex items-center gap-2 px-4 py-2 rounded-2xl border border-surface-200 bg-white text-sm font-medium text-surface-900 shadow-sm hover:bg-surface-100 transition">
        <i class="fa-solid fa-arrow-left"></i>
        Back
    </a>

<!-- Background layers -->
<div class="grid-bg fixed inset-0 z-0"></div>
<div class="floating-shape w-[400px] h-[400px] bg-brand-500/20 -top-48 -left-48"></div>
<div class="floating-shape w-[300px] h-[300px] bg-brand-400/15 bottom-0 right-0"></div>
<div class="floating-shape w-[250px] h-[250px] bg-brand-600/10 top-1/3 left-1/4"></div>

<!-- Toast Container -->
<div id="toastContainer" class="fixed bottom-6 right-6 z-50 flex flex-col gap-3"></div>

<div class="relative z-10 min-h-screen flex items-center justify-center p-4">
    <div class="max-w-6xl w-full mx-auto">
        <div class="flex bg-white rounded-3xl shadow-2xl overflow-hidden backdrop-blur-sm">
            <!-- Left Panel - Branding -->
            <div class="left-panel w-5/12 bg-gradient-to-br from-brand-600 to-brand-800 p-10 text-white hidden lg:flex flex-col justify-between">
                <div>
                    <div class="flex items-center gap-2.5 mb-12">
                        <div class="w-10 h-10 rounded-xl bg-white/20 flex items-center justify-center">
                            <i class="fa-solid fa-cubes text-xl text-white"></i>
                        </div>
                        <span class="font-display font-bold text-2xl tracking-tight">Solvix StockFlow</span>
                    </div>
                    <h2 class="font-display text-3xl font-bold leading-tight mb-4">Welcome back to intelligent inventory</h2>
                    <p class="text-white/80 text-base leading-relaxed">Streamline your operations, reduce costs, and gain real-time visibility across all your warehouses.</p>
                </div>
                <div class="space-y-6">
                    <div class="flex items-center gap-3 text-sm">
                        <i class="fa-regular fa-circle-check text-xl"></i>
                        <span>Trusted by 12,000+ businesses</span>
                    </div>
                    <div class="flex items-center gap-3 text-sm">
                        <i class="fa-regular fa-clock text-xl"></i>
                        <span>99.99% uptime SLA</span>
                    </div>
                    <div class="flex items-center gap-3 text-sm">
                        <i class="fa-regular fa-chart-line text-xl"></i>
                        <span>34% average cost reduction</span>
                    </div>
                </div>
            </div>

            <!-- Right Panel - Forms -->
            <div class="w-full lg:w-7/12 p-4 md:p-8 bg-white overflow-hidden">
                <div class="max-h-[calc(100vh-4rem)] overflow-y-auto pr-0 lg:pr-0">
                    <!-- Tabs -->
                <div class="flex border-b border-surface-200 mb-8 relative">
                    <button id="loginTab" onclick="switchTab('login')" class="flex-1 pb-4 text-center font-semibold text-surface-900 transition-colors">Sign In</button>
                    <button id="registerTab" onclick="switchTab('register')" class="flex-1 pb-4 text-center font-semibold text-surface-400 transition-colors">Create Account</button>
                    <div id="tabIndicator" class="tab-indicator absolute bottom-0 left-0 w-1/2 h-0.5 bg-brand-500 rounded-full transition-transform duration-300"></div>
                </div>

                <!-- Login Form -->
                <div id="loginPanel" class="form-panel active-panel">
                    <div class="mb-6">
                        <h2 class="font-display text-2xl font-bold text-surface-900 mb-1.5">Welcome back</h2>
                        <p class="text-surface-500 text-sm">Sign in to access your dashboard</p>
                    </div>

                    <form id="loginForm" onsubmit="handleLogin(event)" novalidate>
                        <div class="space-y-4 mb-6">
                            <div>
                                <label class="block text-sm font-medium text-surface-700 mb-1.5">Email address</label>
                                <div class="input-group relative">
                                    <i class="fa-regular fa-envelope input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                    <input type="email" id="loginEmail" placeholder="hello@company.com" required
                                        class="w-full pl-10 pr-4 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white">
                                </div>
                                <p id="loginEmailError" class="text-red-500 text-xs mt-1 hidden"></p>
                            </div>
                            <div>
                                <label class="block text-sm font-medium text-surface-700 mb-1.5">Password</label>
                                <div class="input-group relative">
                                    <i class="fa-solid fa-lock input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                    <input type="password" id="loginPassword" placeholder="Enter your password" required
                                        class="w-full pl-10 pr-11 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white">
                                    <button type="button" onclick="togglePassword('loginPassword', this)" class="password-toggle absolute right-3.5 top-1/2 -translate-y-1/2 text-surface-400">
                                        <i class="fa-regular fa-eye text-sm"></i>
                                    </button>
                                </div>
                                <p id="loginPasswordError" class="text-red-500 text-xs mt-1 hidden"></p>
                            </div>
                            <div class="text-right">
                                <a href="#" class="text-sm text-brand-600 hover:text-brand-700 font-medium">Forgot password?</a>
                            </div>
                        </div>

                        <button type="submit" id="loginBtn" class="btn-primary w-full py-3 rounded-xl text-white font-semibold text-sm flex items-center justify-center gap-2">
                            <span id="loginBtnText">Sign In</span>
                            <i id="loginBtnIcon" class="fa-solid fa-arrow-right text-xs"></i>
                            <div id="loginBtnSpinner" class="spinner hidden"></div>
                        </button>
                    </form>

                    <!-- Social Login -->
                    <div class="mt-8">
                        <div class="relative mb-6">
                            <div class="absolute inset-0 flex items-center">
                                <div class="w-full border-t border-surface-200"></div>
                            </div>
                            <div class="relative flex justify-center text-sm">
                                <span class="px-4 bg-white text-surface-400">Or continue with</span>
                            </div>
                        </div>
                        <div class="grid grid-cols-2 gap-3">
                            <button onclick="signInWithGoogle()" class="flex items-center justify-center gap-2.5 py-2.5 px-4 rounded-xl border border-surface-200 bg-white hover:bg-surface-50 hover:border-surface-300 transition-all text-sm font-medium text-surface-700">
                                <svg class="w-4 h-4" viewBox="0 0 24 24">
                                    <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z"/>
                                    <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                                    <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                                    <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
                                </svg>
                                Google
                            </button>
                            <button onclick="signInWithMicrosoft()" class="flex items-center justify-center gap-2.5 py-2.5 px-4 rounded-xl border border-surface-200 bg-white hover:bg-surface-50 hover:border-surface-300 transition-all text-sm font-medium text-surface-700">
                                <svg class="w-4 h-4" viewBox="0 0 24 24">
                                    <rect fill="#F25022" x="1" y="1" width="10" height="10"/>
                                    <rect fill="#7FBA00" x="13" y="1" width="10" height="10"/>
                                    <rect fill="#00A4EF" x="1" y="13" width="10" height="10"/>
                                    <rect fill="#FFB900" x="13" y="13" width="10" height="10"/>
                                </svg>
                                Microsoft
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Register Form -->
                <div id="registerPanel" class="form-panel hidden-panel">
                    <div class="mb-6">
                        <h2 class="font-display text-2xl font-bold text-surface-900 mb-1.5">Create your account</h2>
                        <p class="text-surface-500 text-sm">Start managing your inventory in minutes</p>
                    </div>

                    <form id="registerForm" onsubmit="handleRegister(event)" novalidate>
                        <div class="space-y-4 mb-6">
                            <div class="grid grid-cols-2 gap-3">
                                <div>
                                    <label class="block text-sm font-medium text-surface-700 mb-1.5">First Name</label>
                                    <div class="input-group relative">
                                        <i class="fa-regular fa-user input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                        <input type="text" id="firstName" placeholder="Mash" required
                                            class="w-full pl-10 pr-4 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white">
                                    </div>
                                    <p id="firstNameError" class="text-red-500 text-xs mt-1 hidden"></p>
                                </div>
                                <div>
                                    <label class="block text-sm font-medium text-surface-700 mb-1.5">Last Name</label>
                                    <div class="input-group relative">
                                        <i class="fa-regular fa-user input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                        <input type="text" id="lastName" placeholder="Lets" required
                                            class="w-full pl-10 pr-4 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white">
                                    </div>
                                    <p id="lastNameError" class="text-red-500 text-xs mt-1 hidden"></p>
                                </div>
                            </div>

                            <div>
                                <label class="block text-sm font-medium text-surface-700 mb-1.5">Work Email</label>
                                <div class="input-group relative">
                                    <i class="fa-regular fa-envelope input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                    <input type="email" id="registerEmail" placeholder="mash@company.com" required
                                        class="w-full pl-10 pr-4 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white">
                                </div>
                                <p id="registerEmailError" class="text-red-500 text-xs mt-1 hidden"></p>
                            </div>

                            <div>
                                <label class="block text-sm font-medium text-surface-700 mb-1.5">Company Name</label>
                                <div class="input-group relative">
                                    <i class="fa-regular fa-building input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                    <input type="text" id="companyName" placeholder="Solvix Corp" required
                                        class="w-full pl-10 pr-4 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white">
                                </div>
                                <p id="companyNameError" class="text-red-500 text-xs mt-1 hidden"></p>
                            </div>

                            <div>
                                <label class="block text-sm font-medium text-surface-700 mb-1.5">User Role</label>
                                <div class="input-group relative">
                                    <i class="fa-regular fa-briefcase input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                    <select id="userRole" required
                                        class="w-full pl-10 pr-4 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 focus:outline-none focus:border-brand-500 focus:bg-white appearance-none">
                                        <option value="">Select a role</option>
                                        <option value="staff">Staff</option>
                                        <option value="admin">Admin</option>
                                    </select>
                                </div>
                                <p id="userRoleError" class="text-red-500 text-xs mt-1 hidden"></p>
                            </div>

                            <div>
                                <label class="block text-sm font-medium text-surface-700 mb-1.5">Password</label>
                                <div class="input-group relative">
                                    <i class="fa-solid fa-lock input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                    <input type="password" id="registerPassword" placeholder="Create a strong password" required
                                        class="w-full pl-10 pr-11 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white"
                                        oninput="checkPasswordStrength(this.value)">
                                    <button type="button" onclick="togglePassword('registerPassword', this)" class="password-toggle absolute right-3.5 top-1/2 -translate-y-1/2 text-surface-400">
                                        <i class="fa-regular fa-eye text-sm"></i>
                                    </button>
                                </div>
                                <div id="strengthContainer" class="mt-2 hidden">
                                    <div class="flex gap-1.5 mb-1.5">
                                        <div id="str1" class="strength-bar h-1 flex-1 rounded-full bg-surface-200"></div>
                                        <div id="str2" class="strength-bar h-1 flex-1 rounded-full bg-surface-200"></div>
                                        <div id="str3" class="strength-bar h-1 flex-1 rounded-full bg-surface-200"></div>
                                        <div id="str4" class="strength-bar h-1 flex-1 rounded-full bg-surface-200"></div>
                                    </div>
                                    <p id="strengthText" class="text-xs text-surface-400"></p>
                                </div>
                                <p id="registerPasswordError" class="text-red-500 text-xs mt-1 hidden"></p>
                            </div>

                            <div>
                                <label class="block text-sm font-medium text-surface-700 mb-1.5">Confirm Password</label>
                                <div class="input-group relative">
                                    <i class="fa-solid fa-lock input-icon absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                                    <input type="password" id="confirmPassword" placeholder="Confirm your password" required
                                        class="w-full pl-10 pr-11 py-3 bg-surface-50 border border-surface-200 rounded-xl text-sm text-surface-800 placeholder:text-surface-400 focus:outline-none focus:border-brand-500 focus:bg-white">
                                    <button type="button" onclick="togglePassword('confirmPassword', this)" class="password-toggle absolute right-3.5 top-1/2 -translate-y-1/2 text-surface-400">
                                        <i class="fa-regular fa-eye text-sm"></i>
                                    </button>
                                </div>
                                <p id="confirmPasswordError" class="text-red-500 text-xs mt-1 hidden"></p>
                            </div>

                            <div class="flex items-start gap-2.5">
                                <input type="checkbox" id="agreeTerms" class="checkbox-custom mt-0.5" required>
                                <label for="agreeTerms" class="text-sm text-surface-600 cursor-pointer select-none leading-relaxed">
                                    I agree to the <a href="#" class="text-brand-600 hover:text-brand-700 font-medium">Terms of Service</a> and <a href="#" class="text-brand-600 hover:text-brand-700 font-medium">Privacy Policy</a>
                                </label>
                            </div>
                            <p id="agreeTermsError" class="text-red-500 text-xs -mt-2 hidden"></p>
                        </div>

                        <button type="submit" id="registerBtn" class="btn-primary w-full py-3 rounded-xl text-white font-semibold text-sm flex items-center justify-center gap-2">
                            <span id="registerBtnText">Create Account</span>
                            <i id="registerBtnIcon" class="fa-solid fa-arrow-right text-xs"></i>
                            <div id="registerBtnSpinner" class="spinner hidden"></div>
                        </button>
                    </form>

                    <div class="mt-6 text-center text-xs text-surface-400">
                        By signing up, you agree to our <a href="#" class="text-brand-600">Terms</a>.
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Custom JS -->
<script src="./assets/js/login.js"></script>
</body>
</html>