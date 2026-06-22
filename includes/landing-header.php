<?php
if (session_status() === PHP_SESSION_NONE) {
    session_start();
}
// Determine if user is logged in
$isLoggedIn = isset($_SESSION['user_id']);
$pageTitle = $pageTitle ?? 'Solvix StockFlow - Inventory Management System';
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title><?php echo htmlspecialchars($pageTitle); ?></title>
    <meta name="description" content="Intelligent inventory management system">
    
    <!-- Tailwind CSS CDN -->
    <script src="https://cdn.tailwindcss.com"></script>
    <!-- Tailwind config override -->
    <script src="./assets/js/styles.js"></script>
    <!-- Font Awesome -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
    <!-- Google Fonts -->
    <link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300;400;500;600;700&family=DM+Sans:ital,wght@0,400;0,500;0,600;0,700;1,400&display=swap" rel="stylesheet">
    <!-- Custom CSS -->
    <link rel="stylesheet" href="./assets/css/landing-style.css">
    <link rel="stylesheet" href="./assets/css/dashboard.css">
</head>
<body>

<!-- Background layers (only on non-dashboard pages? Keep globally but subtle) -->
<div class="grid-bg"></div>
<div class="orb orb-1"></div>
<div class="orb orb-2"></div>
<div class="orb orb-3"></div>

<!-- Navigation -->
<nav id="navbar" class="fixed top-0 left-0 right-0 z-50 transition-all duration-300" style="top: <?php echo isset($showTicker) && $showTicker ? '38px' : '0'; ?>;">
    <div class="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
        <a href="<?php echo $isLoggedIn ? 'dashboard.php' : 'index.php'; ?>" class="flex items-center gap-2.5 no-underline">
            <div class="w-9 h-9 rounded-lg flex items-center justify-center" style="background: var(--accent);">
                <i class="fa-solid fa-cubes text-white text-sm"></i>
            </div>
            <span class="font-display font-bold text-xl" style="color: var(--fg);">Solvix StockFlow</span>
        </a>
            <!-- Public navigation -->
            <div class="hidden md:flex items-center gap-8">
                <a href="index.php#features" class="nav-link font-body text-sm no-underline">Features</a>
                <a href="index.php#how-it-works" class="nav-link font-body text-sm no-underline">How It Works</a>
                <a href="index.php#pricing" class="nav-link font-body text-sm no-underline">Pricing</a>
                <a href="index.php#testimonials" class="nav-link font-body text-sm no-underline">Testimonials</a>
            </div>
            <div class="hidden md:flex items-center gap-4">
                <a href="login.php" class="btn-secondary text-sm" style="padding: 10px 22px;">Sign In</a>
                <a href="login.php" class="btn-primary text-sm" style="padding: 10px 22px;">Start Free Trial</a>
            </div>
        
        
        <button id="menuBtn" class="md:hidden text-xl" style="color: var(--fg); background: none; border: none; cursor: pointer;">
            <i class="fa-solid fa-bars"></i>
        </button>
    </div>
</nav>

<!-- Mobile menu -->
<div id="mobileMenu" class="mobile-menu">
    <button id="closeMenu" style="position: absolute; top: 24px; right: 24px; background: none; border: none; color: var(--fg); font-size: 1.5rem; cursor: pointer;">
        <i class="fa-solid fa-xmark"></i>
    </button>
        <a href="index.php#features" onclick="closeMobileMenu()">Features</a>
        <a href="index.php#how-it-works" onclick="closeMobileMenu()">How It Works</a>
        <a href="index.php#pricing" onclick="closeMobileMenu()">Pricing</a>
        <a href="index.php#testimonials" onclick="closeMobileMenu()">Testimonials</a>
        <a href="login.php" class="btn-primary" style="margin-top: 16px; display: inline-block;">Sign In</a>

</div>

<!-- Main content wrapper with top padding to account for fixed nav -->
<div style="padding-top: <?php echo isset($showTicker) && $showTicker ? '90px' : '70px'; ?>;">