<?php
// seed_fixed.php - Corrected version
error_reporting(E_ALL);
ini_set('display_errors', 1);

$serviceAccountPath = __DIR__ . '/config/inventory-system-firebase-key.json'; // adjust if needed
$projectId = 'inventory-system-7255c';

if (!file_exists($serviceAccountPath)) {
    die("Service account key not found at: $serviceAccountPath\n");
}

// Helper: Get OAuth2 access token
function getAccessToken($serviceAccountPath) {
    $serviceAccount = json_decode(file_get_contents($serviceAccountPath), true);
    $now = time();
    $jwtHeader = base64_encode(json_encode(['alg' => 'RS256', 'typ' => 'JWT']));
    $jwtClaim = base64_encode(json_encode([
        'iss' => $serviceAccount['client_email'],
        'scope' => 'https://www.googleapis.com/auth/datastore',
        'aud' => 'https://oauth2.googleapis.com/token',
        'exp' => $now + 3600,
        'iat' => $now,
    ]));
    $unsignedJwt = $jwtHeader . '.' . $jwtClaim;
    openssl_sign($unsignedJwt, $signature, $serviceAccount['private_key'], OPENSSL_ALGO_SHA256);
    $jwt = $unsignedJwt . '.' . base64_encode($signature);
    
    $ch = curl_init('https://oauth2.googleapis.com/token');
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query([
        'grant_type' => 'urn:ietf:params:oauth:grant-type:jwt-bearer',
        'assertion' => $jwt,
    ]));
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    if ($httpCode !== 200) {
        die("Failed to get access token: $response\n");
    }
    $data = json_decode($response, true);
    return $data['access_token'];
}

// Helper: Send Firestore request (collection POST = auto-generated ID)
function firestorePost($accessToken, $projectId, $collection, $fields) {
    $url = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents/$collection";
    $body = ['fields' => []];
    foreach ($fields as $key => $value) {
        if (is_string($value)) {
            $body['fields'][$key] = ['stringValue' => $value];
        } elseif (is_int($value) || is_float($value)) {
            $body['fields'][$key] = ['doubleValue' => $value];
        } elseif (is_bool($value)) {
            $body['fields'][$key] = ['booleanValue' => $value];
        } elseif ($value instanceof DateTime) {
            $body['fields'][$key] = ['timestampValue' => $value->format(DateTime::ATOM)];
        }
    }
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Authorization: Bearer ' . $accessToken,
        'Content-Type: application/json',
    ]);
    curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($body));
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    if ($httpCode < 200 || $httpCode >= 300) {
        echo "HTTP $httpCode: $response\n";
        return false;
    }
    return true;
}

// Helper: Update a document with specific ID (PATCH)
function firestorePatch($accessToken, $projectId, $collection, $docId, $fields) {
    $url = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents/$collection/$docId";
    $body = ['fields' => []];
    foreach ($fields as $key => $value) {
        if (is_string($value)) {
            $body['fields'][$key] = ['stringValue' => $value];
        } elseif (is_int($value) || is_float($value)) {
            $body['fields'][$key] = ['doubleValue' => $value];
        } elseif (is_bool($value)) {
            $body['fields'][$key] = ['booleanValue' => $value];
        } elseif ($value instanceof DateTime) {
            $body['fields'][$key] = ['timestampValue' => $value->format(DateTime::ATOM)];
        }
    }
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'PATCH');
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Authorization: Bearer ' . $token,
        'Content-Type: ' . $contentType,
    ]);
    curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($body));
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    if ($httpCode < 200 || $httpCode >= 300) {
        echo "HTTP $httpCode: $response\n";
        return false;
    }
    return true;
}

echo "Getting access token...\n";
$token = getAccessToken($serviceAccountPath);
echo "Access token obtained.\n";

// --- Seed Products ---
$products = [
    ['name' => 'Wireless Mouse', 'sku' => 'WM-001', 'category' => 'Peripherals', 'price' => 29.99, 'stock' => 145, 'minStock' => 20, 'status' => 'In Stock'],
    ['name' => 'Mechanical Keyboard', 'sku' => 'KB-001', 'category' => 'Peripherals', 'price' => 89.99, 'stock' => 3, 'minStock' => 10, 'status' => 'Low Stock'],
    ['name' => 'USB-C Hub 7-in-1', 'sku' => 'UC-012', 'category' => 'Accessories', 'price' => 45.00, 'stock' => 67, 'minStock' => 15, 'status' => 'In Stock'],
    ['name' => 'Monitor Stand Riser', 'sku' => 'MS-003', 'category' => 'Accessories', 'price' => 35.50, 'stock' => 42, 'minStock' => 10, 'status' => 'In Stock'],
    ['name' => 'Webcam HD 1080p', 'sku' => 'WC-007', 'category' => 'Electronics', 'price' => 59.99, 'stock' => 0, 'minStock' => 5, 'status' => 'Out of Stock'],
    ['name' => 'Wireless Headset Pro', 'sku' => 'WH-042', 'category' => 'Audio', 'price' => 79.99, 'stock' => 8, 'minStock' => 15, 'status' => 'Low Stock'],
    ['name' => 'Laptop Stand Aluminum', 'sku' => 'LS-018', 'category' => 'Accessories', 'price' => 49.99, 'stock' => 31, 'minStock' => 10, 'status' => 'In Stock'],
    ['name' => 'HDMI Cable 2m', 'sku' => 'HC-055', 'category' => 'Accessories', 'price' => 12.99, 'stock' => 200, 'minStock' => 50, 'status' => 'In Stock'],
    ['name' => 'Desk Lamp LED', 'sku' => 'DL-009', 'category' => 'Electronics', 'price' => 39.99, 'stock' => 18, 'minStock' => 8, 'status' => 'In Stock'],
    ['name' => 'Bluetooth Speaker', 'sku' => 'BS-023', 'category' => 'Audio', 'price' => 55.00, 'stock' => 5, 'minStock' => 10, 'status' => 'Low Stock'],
];

foreach ($products as $p) {
    $p['createdAt'] = new DateTime();
    if (firestorePost($token, $projectId, 'products', $p)) {
        echo "✓ Added product: {$p['name']}\n";
    } else {
        echo "✗ Failed: {$p['name']}\n";
    }
}

// --- Seed Suppliers ---
$suppliers = [
    ['name' => 'TechWorld Supplies', 'contact' => 'John Smith', 'email' => 'john@techworld.com', 'phone' => '+1 555-0101', 'products' => 45, 'status' => 'Active', 'rating' => 4.8],
    ['name' => 'Global Electronics Co.', 'contact' => 'Sarah Chen', 'email' => 'sarah@globalelec.com', 'phone' => '+1 555-0202', 'products' => 32, 'status' => 'Active', 'rating' => 4.5],
    ['name' => 'Premium Peripherals', 'contact' => 'Mike Johnson', 'email' => 'mike@premperiph.com', 'phone' => '+1 555-0303', 'products' => 28, 'status' => 'Active', 'rating' => 4.2],
    ['name' => 'AudioMax Inc.', 'contact' => 'Lisa Park', 'email' => 'lisa@audiomax.com', 'phone' => '+1 555-0404', 'products' => 15, 'status' => 'Inactive', 'rating' => 3.9],
    ['name' => 'CableConnect Ltd.', 'contact' => 'David Brown', 'email' => 'david@cableconnect.com', 'phone' => '+1 555-0505', 'products' => 22, 'status' => 'Active', 'rating' => 4.6],
    ['name' => 'SmartGear Distribution', 'contact' => 'Emma Wilson', 'email' => 'emma@smartgear.com', 'phone' => '+1 555-0606', 'products' => 38, 'status' => 'Active', 'rating' => 4.7],
];

foreach ($suppliers as $s) {
    $s['createdAt'] = new DateTime();
    if (firestorePost($token, $projectId, 'suppliers', $s)) {
        echo "✓ Added supplier: {$s['name']}\n";
    } else {
        echo "✗ Failed: {$s['name']}\n";
    }
}

// --- Admin user document (requires UID) ---
echo "\n--- Admin User Setup ---\n";
echo "Enter the admin user's email (the one you used to sign up): ";
$handle = fopen("php://stdin", "r");
$adminEmail = trim(fgets($handle));
fclose($handle);

if (empty($adminEmail)) {
    echo "❌ No email provided. Admin user document not created.\n";
} else {
    echo "Enter the Firebase UID for that user (find in Firebase Console -> Authentication -> Users): ";
    $handle2 = fopen("php://stdin", "r");
    $uid = trim(fgets($handle2));
    fclose($handle2);
    if (empty($uid)) {
        echo "❌ No UID provided. Admin user document not created.\n";
    } else {
        $userData = [
            'firstName' => 'Admin',
            'lastName' => 'User',
            'email' => $adminEmail,
            'role' => 'admin',
            'status' => 'Active',
            'createdAt' => new DateTime(),
            'lastLogin' => new DateTime(),
        ];
        if (firestorePatch($token, $projectId, 'users', $uid, $userData)) {
            echo "✓ Admin user document created for UID: $uid\n";
        } else {
            echo "✗ Failed to create admin user document.\n";
        }
    }
}

echo "\nSeeding completed.\n";