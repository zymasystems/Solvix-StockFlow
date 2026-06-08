<?php
session_start();
header('Content-Type: application/json');
require_once __DIR__ . '/config/firebase.php';

function apiResponse($success, $message = '', $role = null) {
    echo json_encode(['success' => $success, 'message' => $message, 'role' => $role]);
    exit;
}

$input = json_decode(file_get_contents('php://input'), true) ?: $_POST;
$idToken = $input['idToken'] ?? null;

if (!$idToken) {
    apiResponse(false, 'No token provided.');
}

try {
    $auth = getFirebaseAuth();
    $verifiedIdToken = $auth->verifyIdToken($idToken);
    $uid = $verifiedIdToken->claims()->get('sub');
    $firebaseUser = $auth->getUser($uid);

    $firstName = $input['firstName'] ?? '';
    $lastName  = $input['lastName']  ?? '';
    $company   = $input['company']   ?? '';
    $inputRole = strtolower(trim($input['role'] ?? ''));

    // Get role from custom claims or input
    $existingClaims = $firebaseUser->customClaims ?? [];
    $role = $inputRole ?: ($existingClaims['role'] ?? 'staff');

    // Save role to claims if registering
    if ($inputRole && empty($existingClaims['role'])) {
        $auth->setCustomUserClaims($uid, ['role' => $inputRole]);
    }

    $_SESSION['user_id']   = $uid;
    $_SESSION['email']     = $firebaseUser->email;
    $_SESSION['name']      = trim($firstName . ' ' . $lastName) ?: ($firebaseUser->displayName ?? '');
    $_SESSION['role']      = $role;
    $_SESSION['company']   = $company;
    $_SESSION['logged_in'] = true;

    apiResponse(true, 'Login successful', $role);

} catch (Exception $e) {
    apiResponse(false, $e->getMessage());
}