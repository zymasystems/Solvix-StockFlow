<?php
// seed.php - Run this script once to populate Firestore with initial data
require_once __DIR__ . '/vendor/autoload.php';

use Kreait\Firebase\Factory;
use Kreait\Firebase\Auth\UserRecord;

// Path to your Firebase service account key
$serviceAccountPath = __DIR__ . '/config/inventory-system-firebase-key.json';

if (!file_exists($serviceAccountPath)) {
    die("Service account key not found at: $serviceAccountPath\n");
}

$factory = (new Factory)->withServiceAccount($serviceAccountPath);
$auth = $factory->createAuth();
$firestore = $factory->createFirestore();
$db = $firestore->database();

echo "Starting Firestore seed...\n";

// ==================== 1. Seed Products ====================
$products = [
    [
        'name' => 'Wireless Mouse',
        'sku' => 'WM-001',
        'category' => 'Peripherals',
        'price' => 29.99,
        'stock' => 145,
        'minStock' => 20,
        'status' => 'In Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Mechanical Keyboard',
        'sku' => 'KB-001',
        'category' => 'Peripherals',
        'price' => 89.99,
        'stock' => 3,
        'minStock' => 10,
        'status' => 'Low Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'USB-C Hub 7-in-1',
        'sku' => 'UC-012',
        'category' => 'Accessories',
        'price' => 45.00,
        'stock' => 67,
        'minStock' => 15,
        'status' => 'In Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Monitor Stand Riser',
        'sku' => 'MS-003',
        'category' => 'Accessories',
        'price' => 35.50,
        'stock' => 42,
        'minStock' => 10,
        'status' => 'In Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Webcam HD 1080p',
        'sku' => 'WC-007',
        'category' => 'Electronics',
        'price' => 59.99,
        'stock' => 0,
        'minStock' => 5,
        'status' => 'Out of Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Wireless Headset Pro',
        'sku' => 'WH-042',
        'category' => 'Audio',
        'price' => 79.99,
        'stock' => 8,
        'minStock' => 15,
        'status' => 'Low Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Laptop Stand Aluminum',
        'sku' => 'LS-018',
        'category' => 'Accessories',
        'price' => 49.99,
        'stock' => 31,
        'minStock' => 10,
        'status' => 'In Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'HDMI Cable 2m',
        'sku' => 'HC-055',
        'category' => 'Accessories',
        'price' => 12.99,
        'stock' => 200,
        'minStock' => 50,
        'status' => 'In Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Desk Lamp LED',
        'sku' => 'DL-009',
        'category' => 'Electronics',
        'price' => 39.99,
        'stock' => 18,
        'minStock' => 8,
        'status' => 'In Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Bluetooth Speaker',
        'sku' => 'BS-023',
        'category' => 'Audio',
        'price' => 55.00,
        'stock' => 5,
        'minStock' => 10,
        'status' => 'Low Stock',
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
];

$productCollection = $db->collection('products');
$productCount = 0;
foreach ($products as $product) {
    $productCollection->add($product);
    $productCount++;
}
echo " Added $productCount products.\n";

// ==================== 2. Seed Suppliers ====================
$suppliers = [
    [
        'name' => 'TechWorld Supplies',
        'contact' => 'John Smith',
        'email' => 'john@techworld.com',
        'phone' => '+1 555-0101',
        'products' => 45,
        'status' => 'Active',
        'rating' => 4.8,
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Global Electronics Co.',
        'contact' => 'Sarah Chen',
        'email' => 'sarah@globalelec.com',
        'phone' => '+1 555-0202',
        'products' => 32,
        'status' => 'Active',
        'rating' => 4.5,
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'Premium Peripherals',
        'contact' => 'Mike Johnson',
        'email' => 'mike@premperiph.com',
        'phone' => '+1 555-0303',
        'products' => 28,
        'status' => 'Active',
        'rating' => 4.2,
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'AudioMax Inc.',
        'contact' => 'Lisa Park',
        'email' => 'lisa@audiomax.com',
        'phone' => '+1 555-0404',
        'products' => 15,
        'status' => 'Inactive',
        'rating' => 3.9,
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'CableConnect Ltd.',
        'contact' => 'David Brown',
        'email' => 'david@cableconnect.com',
        'phone' => '+1 555-0505',
        'products' => 22,
        'status' => 'Active',
        'rating' => 4.6,
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
    [
        'name' => 'SmartGear Distribution',
        'contact' => 'Emma Wilson',
        'email' => 'emma@smartgear.com',
        'phone' => '+1 555-0606',
        'products' => 38,
        'status' => 'Active',
        'rating' => 4.7,
        'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
    ],
];

$supplierCollection = $db->collection('suppliers');
$supplierCount = 0;
foreach ($suppliers as $supplier) {
    $supplierCollection->add($supplier);
    $supplierCount++;
}
echo " Added $supplierCount suppliers.\n";

// ==================== 3. Seed Admin User Document & Set Custom Claim ====================
// You must know the UID of the Firebase Auth user who will be admin.
// Option 1: Prompt for email, fetch user by email.
// Option 2: Hardcode a known email (update as needed).
echo "\n--- Admin User Setup ---\n";
echo "Enter the email address of the admin user (the one you use to log in): ";
$handle = fopen("php://stdin", "r");
$adminEmail = trim(fgets($handle));
fclose($handle);

if (empty($adminEmail)) {
    echo " No email provided. Skipping admin user setup.\n";
} else {
    try {
        // Get the user by email
        $user = $auth->getUserByEmail($adminEmail);
        $uid = $user->uid;
        
        // Create/update Firestore user document
        $userDocRef = $db->collection('users')->document($uid);
        $userData = [
            'firstName' => 'Admin',
            'lastName' => 'User',
            'email' => $adminEmail,
            'role' => 'admin',
            'status' => 'Active',
            'createdAt' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
            'lastLogin' => new \Google\Cloud\Firestore\Timestamp(new DateTime()),
        ];
        $userDocRef->set($userData, ['merge' => true]);
        echo " Admin user document created/updated for UID: $uid\n";
        
        // Set custom claim 'admin' => true
        $auth->setCustomUserClaims($uid, ['admin' => true]);
        echo "Custom claim 'admin' set for user: $adminEmail\n";
        
    } catch (\Kreait\Firebase\Exception\Auth\UserNotFound $e) {
        echo "User with email '$adminEmail' not found in Firebase Auth.\n";
        echo "   Please sign up first via the login page, then re-run this script.\n";
    } catch (Exception $e) {
        echo " Error: " . $e->getMessage() . "\n";
    }
}

// ==================== 4. Optional: Seed some transactions ====================
$sales = [
    [
        'product' => 'Wireless Mouse',
        'type' => 'Sale',
        'qty' => 5,
        'by' => 'Admin User',
        'date' => (new DateTime('-2 days'))->format('Y-m-d H:i:s'),
        'notes' => 'Initial seed',
    ],
    [
        'product' => 'USB-C Hub 7-in-1',
        'type' => 'Sale',
        'qty' => 2,
        'by' => 'Admin User',
        'date' => (new DateTime('-1 day'))->format('Y-m-d H:i:s'),
        'notes' => 'Initial seed',
    ],
    [
        'product' => 'Mechanical Keyboard',
        'type' => 'Return',
        'qty' => 1,
        'by' => 'Admin User',
        'date' => (new DateTime('-5 hours'))->format('Y-m-d H:i:s'),
        'notes' => 'Defective return',
    ],
];

$salesCollection = $db->collection('sales');
$salesCount = 0;
foreach ($sales as $sale) {
    $sale['timestamp'] = new \Google\Cloud\Firestore\Timestamp(new DateTime($sale['date']));
    $salesCollection->add($sale);
    $salesCount++;
}
echo "✅ Added $salesCount sample sales records.\n";

$adjustments = [
    [
        'product' => 'HDMI Cable 2m',
        'type' => 'Add',
        'change' => '+100',
        'reason' => 'New Shipment Received',
        'by' => 'Admin User',
        'date' => (new DateTime('-3 days'))->format('Y-m-d H:i:s'),
    ],
    [
        'product' => 'Monitor Stand Riser',
        'type' => 'Set',
        'change' => '=42',
        'reason' => 'Inventory Count Correction',
        'by' => 'Admin User',
        'date' => (new DateTime('-1 day'))->format('Y-m-d H:i:s'),
    ],
];

$adjustmentCollection = $db->collection('adjustments');
$adjCount = 0;
foreach ($adjustments as $adj) {
    $adj['timestamp'] = new \Google\Cloud\Firestore\Timestamp(new DateTime($adj['date']));
    $adjustmentCollection->add($adj);
    $adjCount++;
}
echo " Added $adjCount sample stock adjustments.\n";

echo "\n Seeding completed successfully!\n";
echo "You can now log in with the admin email and use the dashboard.\n";