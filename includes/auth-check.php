<?php
// includes/auth-check.php
if (session_status() === PHP_SESSION_NONE) {
    session_start();
}

function requireAuth(): void {
    if (!isset($_SESSION['user_id'])) {
        header('Location: ./login.php');
        exit;
    }
}

function getUserRole(): string {
    return strtolower(trim($_SESSION['role'] ?? ''));
}

function isAdmin(): bool {
    return getUserRole() === 'admin';
}

function isStaff(): bool {
    return getUserRole() === 'staff';
}

function redirectToDashboard(): void {
    if (isAdmin()) {
        header('Location: ./admin-dashboard.php');
    } else {
        header('Location: ./staff-dashboard.php');
    }
    exit;
}

function requireRole(string $role): void {
    requireAuth();

    if (getUserRole() !== strtolower(trim($role))) {
        redirectToDashboard();
    }
}

function requireAdmin(): void {
    requireRole('admin');
}

function requireStaff(): void {
    requireRole('staff');
}

function currentUser(): array {
    return [
        'id' => $_SESSION['user_id'] ?? null,
        'name' => $_SESSION['name'] ?? '',
        'email' => $_SESSION['email'] ?? '',
        'role' => getUserRole(),
        'company' => $_SESSION['company'] ?? ''
    ];
}
?>