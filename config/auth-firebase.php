<?php
// auth-firebase.php
session_start();
header('Content-Type: application/json');

require_once __DIR__ . '/config/firebase.php';

function apiResponse(array $payload): void
{
    if (!headers_sent()) {
        header('Content-Type: application/json');
    }
    echo json_encode($payload);
    exit;
}

$input = json_decode(file_get_contents('php://input'), true);
$idToken = $input['idToken'] ?? null;

if (!$idToken) {
    apiResponse(['success' => false, 'message' => 'No token provided.']);
}

try {
    $auth = getFirebaseAuth();
    $verifiedIdToken = $auth->verifyIdToken($idToken);
    $uid = $verifiedIdToken->claims()->get('sub');
    $firebaseUser = $auth->getUser($uid);

    $firstName = $input['firstName'] ?? ($firebaseUser->displayName ? explode(' ', $firebaseUser->displayName)[0] : '');
    $lastName  = $input['lastName']  ?? ($firebaseUser->displayName ? explode(' ', $firebaseUser->displayName)[1] ?? '' : '');
    $company   = $input['company'] ?? '';
    $role      = isset($input['role']) ? strtolower(trim($input['role'])) : null;
    $isRegistration = isset($input['role']);

    $firestoreSuccess = false;

    // === Firestore User Handling ===
    try {
        $db = getFirestore();
        $userDocRef = $db->collection('users')->document($uid);
        $userSnapshot = $userDocRef->snapshot();

        if (!$userSnapshot->exists()) {
            // New User
            $role = $isRegistration && $role ? $role : 'staff';

            $userData = [
                'uid'        => $uid,
                'email'      => $firebaseUser->email,
                'firstName'  => $firstName,
                'lastName'   => $lastName,
                'fullName'   => trim($firstName . ' ' . $lastName),
                'company'    => $company,
                'role'       => $role,
                'createdAt'  => new \Google\Cloud\Firestore\Timestamp(new \DateTime()),
                'lastLogin'  => new \Google\Cloud\Firestore\Timestamp(new \DateTime()),
                'photoURL'   => $firebaseUser->photoUrl ?? null,
            ];

            $userDocRef->set($userData);
        } else {
            // Existing User - Update last login
            $userData = $userSnapshot->data();
            $role = strtolower(trim($userData['role'] ?? 'staff'));

            $userDocRef->update([
                ['path' => 'lastLogin', 'value' => new \Google\Cloud\Firestore\Timestamp(new \DateTime())]
            ]);
        }
        $firestoreSuccess = true;
    } catch (Exception $e) {
        // Firestore failed, continue with session only
        error_log("Firestore error: " . $e->getMessage());
    }

    // Session Data
    $_SESSION['user_id']   = $uid;
    $_SESSION['email']     = $firebaseUser->email;
    $_SESSION['name']      = trim($firstName . ' ' . $lastName);
    $_SESSION['role']      = $role;
    $_SESSION['company']   = $company;
    $_SESSION['logged_in'] = true;

    apiResponse([
        'success' => true,
        'role'    => $role,
        'message' => 'Login successful'
    ]);

} catch (Exception $e) {
    apiResponse([
        'success' => false,
        'message' => $e->getMessage()
    ]);
}
?>