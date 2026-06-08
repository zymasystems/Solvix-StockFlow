<?php
// staff-dashboard.php
require_once __DIR__ . '/includes/auth-check.php';
requireStaff();
$pageTitle = 'Staff Dashboard - StockFlow';
$userName = htmlspecialchars($_SESSION['name'] ?? 'Staff User');
$userEmail = $_SESSION['email'] ?? '';
$userRole = 'staff';

?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo $pageTitle; ?></title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
    
    <!-- Firebase SDKs -->
    <script src="https://www.gstatic.com/firebasejs/9.22.2/firebase-app-compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/9.22.2/firebase-auth-compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/9.22.2/firebase-firestore-compat.js"></script>
    
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
        const db = firebase.firestore();
        
        const currentUser = {
            uid: "<?php echo $_SESSION['user_id']; ?>",
            email: "<?php echo $userEmail; ?>",
            name: "<?php echo $userName; ?>",
            role: "staff"
        };
    </script>
    
    <style>
        /* Same styles as admin-dashboard – copy from previous */
        * { margin:0; padding:0; box-sizing:border-box; }
        body { font-family:'Inter',sans-serif; background:#f1f5f9; }
        ::-webkit-scrollbar { width:6px; }
        ::-webkit-scrollbar-track { background:#f1f5f9; }
        ::-webkit-scrollbar-thumb { background:#c7d2fe; border-radius:3px; }
        .sidebar-link { transition: all 0.2s ease; }
        .sidebar-link:hover, .sidebar-link.active { background: rgba(99,102,241,0.1); color:#6366f1; }
        .sidebar-link.active { border-right:3px solid #6366f1; }
        .card-hover { transition: all 0.3s ease; }
        .card-hover:hover { transform:translateY(-4px); box-shadow:0 12px 24px rgba(0,0,0,0.1); }
        .fade-in { animation: fadeIn 0.4s ease; }
        @keyframes fadeIn { from{opacity:0;transform:translateY(10px)} to{opacity:1;transform:translateY(0)} }
        .modal-overlay { background: rgba(0,0,0,0.5); backdrop-filter:blur(4px); }
        .pulse-dot { animation: pulse 2s infinite; }
        @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.5} }
        .badge { font-size:0.65rem; padding:2px 8px; border-radius:9999px; font-weight:600; text-transform:uppercase; letter-spacing:0.05em; }
        .table-row { transition: background 0.15s ease; }
        .table-row:hover { background:#f8fafc; }
        .btn-primary { background: linear-gradient(135deg, #6366f1, #4f46e5); transition: all 0.2s ease; }
        .btn-primary:hover { background: linear-gradient(135deg, #4f46e5, #3730a3); transform:translateY(-1px); box-shadow:0 4px 12px rgba(99,102,241,0.4); }
        .btn-danger { background: linear-gradient(135deg, #ef4444, #dc2626); transition: all 0.2s ease; }
        .btn-danger:hover { transform:translateY(-1px); box-shadow:0 4px 12px rgba(239,68,68,0.4); }
        .notification-badge { position:absolute; top:-4px; right:-4px; width:18px; height:18px; background:#ef4444; border-radius:50%; font-size:10px; display:flex; align-items:center; justify-content:center; color:white; font-weight:700; }
        .mobile-menu-btn { display:none; }
        @media(max-width:1024px) {
            .sidebar { transform:translateX(-100%); position:fixed !important; z-index:50; transition:transform 0.3s ease; }
            .sidebar.open { transform:translateX(0); }
            .mobile-menu-btn { display:flex; }
            .main-content { margin-left:0 !important; }
        }
        .tooltip { position:relative; }
        .tooltip:hover::after { content:attr(data-tip); position:absolute; bottom:100%; left:50%; transform:translateX(-50%); background:#1e293b; color:white; padding:4px 8px; border-radius:4px; font-size:12px; white-space:nowrap; z-index:100; }
    </style>
</head>
<body class="bg-gray-50">
    <div id="dashboardScreen" class="min-h-screen">
        <!-- Mobile Header -->
        <div class="lg:hidden fixed top-0 left-0 right-0 bg-white shadow-sm z-40 px-4 py-3 flex items-center justify-between mobile-menu-btn">
            <button onclick="toggleSidebar()" class="text-gray-600 text-xl"><i class="fas fa-bars"></i></button>
            <div class="flex items-center gap-2">
                <div class="w-8 h-8 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-lg flex items-center justify-center">
                    <i class="fas fa-boxes-stacked text-white text-sm"></i>
                </div>
                <span class="font-bold text-gray-800">StockFlow</span>
            </div>
            <div class="relative cursor-pointer" onclick="showNotifications()">
                <i class="fas fa-bell text-gray-500"></i>
                <span class="notification-badge">3</span>
            </div>
        </div>

        <div id="sidebarOverlay" class="hidden fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden" onclick="toggleSidebar()"></div>

        <!-- Sidebar (no Users link) -->
        <aside id="sidebar" class="sidebar fixed left-0 top-0 h-full w-64 bg-white shadow-lg z-50 flex flex-col transition-transform duration-300">
            <div class="p-6 border-b border-gray-100">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-md">
                        <i class="fas fa-boxes-stacked text-white"></i>
                    </div>
                    <div>
                        <h1 class="font-bold text-gray-900 text-lg">StockFlow</h1>
                        <p class="text-xs text-gray-400">Staff Portal</p>
                    </div>
                </div>
            </div>
            <nav class="flex-1 py-4 overflow-y-auto">
                <p class="px-6 text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">Main</p>
                <a onclick="navigateTo('dashboard')" class="sidebar-link active flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="dashboard">
                    <i class="fas fa-chart-pie w-5 text-center"></i> Dashboard
                </a>
                <a onclick="navigateTo('products')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="products">
                    <i class="fas fa-box w-5 text-center"></i> Products
                </a>
                <a onclick="navigateTo('suppliers')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="suppliers">
                    <i class="fas fa-truck w-5 text-center"></i> Suppliers
                </a>
                <a onclick="navigateTo('sales')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="sales">
                    <i class="fas fa-cart-shopping w-5 text-center"></i> Sales / Stock Out
                </a>
                <a onclick="navigateTo('stock')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="stock">
                    <i class="fas fa-warehouse w-5 text-center"></i> Stock Adjustment
                </a>
                <p class="px-6 text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2 mt-4">Analytics</p>
                <a onclick="navigateTo('reports')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="reports">
                    <i class="fas fa-chart-bar w-5 text-center"></i> Reports
                </a>
                <!-- No Administration section -->
            </nav>
            <div class="p-4 border-t border-gray-100">
                <div class="flex items-center gap-3 p-2">
                    <div id="userAvatar" class="w-10 h-10 bg-gradient-to-br from-indigo-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold text-sm">ST</div>
                    <div class="flex-1 min-w-0">
                        <p id="userName" class="text-sm font-semibold text-gray-800 truncate"><?php echo $userName; ?></p>
                        <p id="userRole" class="text-xs text-gray-400 flex items-center gap-1">
                            <span class="w-2 h-2 bg-green-400 rounded-full pulse-dot"></span> Staff
                        </p>
                    </div>
                    <a href="logout.php" class="text-gray-400 hover:text-red-500 transition-colors tooltip" data-tip="Logout">
                        <i class="fas fa-right-from-bracket"></i>
                    </a>
                </div>
            </div>
        </aside>

        <!-- Main Content -->
        <main id="mainContent" class="main-content lg:ml-64 min-h-screen pt-0 lg:pt-0" style="margin-left:256px">
            <div class="lg:hidden h-14"></div>
            <header class="bg-white shadow-sm px-6 py-4 flex items-center justify-between sticky top-0 z-30">
                <div>
                    <h2 id="pageTitle" class="text-xl font-bold text-gray-800">Dashboard</h2>
                    <p id="pageSubtitle" class="text-sm text-gray-400">Overview of your inventory</p>
                </div>
                <div class="flex items-center gap-4">
                    <div class="hidden sm:block relative">
                        <i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm"></i>
                        <input type="text" placeholder="Search..." class="pl-9 pr-4 py-2 bg-gray-50 border border-gray-200 rounded-lg text-sm w-56 focus:w-72 transition-all">
                    </div>
                    <div class="relative cursor-pointer hidden lg:block" onclick="showNotifications()">
                        <i class="fas fa-bell text-gray-500 text-lg"></i>
                        <span class="notification-badge">3</span>
                    </div>
                    <div id="roleBadge" class="badge bg-emerald-100 text-emerald-700 hidden sm:inline">STAFF</div>
                </div>
            </header>

            <div class="p-6">
                <!-- Dashboard Page -->
                <div id="page-dashboard" class="page-content fade-in">
                    <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-6 mb-6">
                        <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100"><div class="flex items-center justify-between mb-4"><div class="w-12 h-12 bg-blue-50 rounded-xl flex items-center justify-center"><i class="fas fa-box text-blue-500 text-xl"></i></div><span class="text-xs font-medium text-green-500 bg-green-50 px-2 py-1 rounded-full"><i class="fas fa-arrow-up mr-1"></i>12%</span></div><p id="statTotalProducts" class="text-2xl font-bold text-gray-800">0</p><p class="text-sm text-gray-400 mt-1">Total Products</p></div>
                        <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100"><div class="flex items-center justify-between mb-4"><div class="w-12 h-12 bg-green-50 rounded-xl flex items-center justify-center"><i class="fas fa-dollar-sign text-green-500 text-xl"></i></div><span class="text-xs font-medium text-green-500 bg-green-50 px-2 py-1 rounded-full"><i class="fas fa-arrow-up mr-1"></i>8%</span></div><p id="statInventoryValue" class="text-2xl font-bold text-gray-800">$0</p><p class="text-sm text-gray-400 mt-1">Inventory Value</p></div>
                        <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100"><div class="flex items-center justify-between mb-4"><div class="w-12 h-12 bg-amber-50 rounded-xl flex items-center justify-center"><i class="fas fa-triangle-exclamation text-amber-500 text-xl"></i></div><span class="text-xs font-medium text-red-500 bg-red-50 px-2 py-1 rounded-full"><i class="fas fa-arrow-up mr-1"></i>5</span></div><p id="statLowStock" class="text-2xl font-bold text-gray-800">0</p><p class="text-sm text-gray-400 mt-1">Low Stock Items</p></div>
                        <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100"><div class="flex items-center justify-between mb-4"><div class="w-12 h-12 bg-red-50 rounded-xl flex items-center justify-center"><i class="fas fa-ban text-red-500 text-xl"></i></div><span class="text-xs font-medium text-red-500 bg-red-50 px-2 py-1 rounded-full"><i class="fas fa-arrow-up mr-1"></i>2</span></div><p id="statOutOfStock" class="text-2xl font-bold text-gray-800">0</p><p class="text-sm text-gray-400 mt-1">Out of Stock</p></div>
                    </div>
                    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6 mb-6">
                        <div class="xl:col-span-2 bg-white rounded-xl shadow-sm border border-gray-100 p-6"><div class="flex items-center justify-between mb-6"><h3 class="font-semibold text-gray-800">Sales & Stock Movement</h3><select id="chartPeriod" class="text-sm border border-gray-200 rounded-lg px-3 py-1.5 text-gray-500"><option value="7">Last 7 Days</option><option value="30">Last 30 Days</option><option value="90">Last 90 Days</option></select></div><canvas id="salesChart" height="120"></canvas></div>
                        <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-6">Category Distribution</h3><canvas id="categoryChart" height="200"></canvas></div>
                    </div>
                    <div class="grid grid-cols-1 xl:grid-cols-2 gap-6">
                        <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Recent Activity</h3><div id="recentActivityContainer" class="space-y-4"></div></div>
                        <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Low Stock Alerts</h3><div id="lowStockContainer" class="space-y-3"></div></div>
                    </div>
                </div>

                <!-- Products Page (no delete buttons) -->
                <div id="page-products" class="page-content hidden fade-in">
                    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6">
                        <div class="flex items-center gap-3 w-full sm:w-auto"><div class="relative flex-1 sm:flex-none"><i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm"></i><input type="text" id="productSearch" placeholder="Search products..." class="pl-9 pr-4 py-2 bg-white border border-gray-200 rounded-lg text-sm w-full sm:w-64" oninput="filterProducts()"></div><select id="categoryFilter" class="py-2 px-3 bg-white border border-gray-200 rounded-lg text-sm text-gray-500" onchange="filterProducts()"><option value="">All Categories</option><option>Electronics</option><option>Peripherals</option><option>Accessories</option><option>Audio</option></select></div>
                        <!-- Add Product button removed for staff? Actually staff can add products but not delete. Let's keep add button but remove delete in table. -->
                        <button onclick="openProductModal()" class="btn-primary text-white px-4 py-2 rounded-lg text-sm font-medium whitespace-nowrap"><i class="fas fa-plus mr-2"></i>Add Product</button>
                    </div>
                    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden"><div class="overflow-x-auto"><table class="w-full"><thead><tr class="bg-gray-50 border-b border-gray-100"><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Product</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">SKU</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Category</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Price</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Stock</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Status</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Actions</th></tr></thead><tbody id="productsTableBody"></tbody></table></div></div>
                </div>

                <!-- Suppliers Page (no add supplier button, no delete) -->
                <div id="page-suppliers" class="page-content hidden fade-in">
                    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6"><div class="relative flex-1 sm:flex-none"><i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm"></i><input type="text" id="supplierSearch" placeholder="Search suppliers..." class="pl-9 pr-4 py-2 bg-white border border-gray-200 rounded-lg text-sm w-full sm:w-64" oninput="renderSuppliers()"></div></div>
                    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6" id="suppliersGrid"></div>
                </div>

                <!-- Sales Page -->
                <div id="page-sales" class="page-content hidden fade-in">
                    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6"><div class="xl:col-span-1"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4"><i class="fas fa-cart-shopping mr-2 text-indigo-500"></i>Record Sale / Stock Out</h3><div class="space-y-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Product</label><select id="saleProduct" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="">Select Product</option></select></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Quantity</label><input type="number" id="saleQty" min="1" value="1" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Type</label><select id="saleType" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="sale">Sale</option><option value="return">Return/Damaged</option><option value="transfer">Transfer</option></select></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Notes</label><textarea id="saleNotes" rows="2" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm" placeholder="Optional notes..."></textarea></div><button onclick="recordSale()" class="w-full btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Record Transaction</button></div></div></div><div class="xl:col-span-2"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Recent Transactions</h3><div class="overflow-x-auto"><table class="w-full"><thead><tr class="bg-gray-50 border-b border-gray-100"><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Date</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Product</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Type</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Qty</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">By</th><tr></thead><tbody id="salesTableBody"></tbody></table></div></div></div></div>
                </div>

                <!-- Stock Adjustment Page -->
                <div id="page-stock" class="page-content hidden fade-in">
                    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6"><div class="xl:col-span-1"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4"><i class="fas fa-sliders mr-2 text-indigo-500"></i>Adjust Stock Level</h3><div class="space-y-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Product</label><select id="adjustProduct" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="">Select Product</option></select></div><div id="currentStockDisplay" class="p-3 bg-blue-50 rounded-lg border border-blue-100 hidden"><p class="text-sm text-blue-700">Current Stock: <span id="currentStockVal" class="font-bold">0</span> units</p></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Adjustment Type</label><select id="adjustType" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="add">Add Stock (Restock)</option><option value="subtract">Remove Stock</option><option value="set">Set Exact Amount</option></select></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Quantity</label><input type="number" id="adjustQty" min="0" value="0" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Reason</label><select id="adjustReason" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option>New Shipment Received</option><option>Inventory Count Correction</option><option>Damaged Goods</option><option>Return from Customer</option><option>Other</option></select></div><button onclick="adjustStock()" class="w-full btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Apply Adjustment</button></div></div></div><div class="xl:col-span-2"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Adjustment History</h3><div class="overflow-x-auto"><table class="w-full"><thead><tr class="bg-gray-50 border-b border-gray-100"><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Date</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Product</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Type</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Change</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Reason</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">By</th></tr></thead><tbody id="adjustmentTableBody"></tbody></table></div></div></div></div>
                </div>

                <!-- Reports Page (no export button) -->
                <div id="page-reports" class="page-content hidden fade-in">
                    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6">
                        <div class="flex items-center gap-3"><select id="reportPeriod" class="py-2 px-3 bg-white border border-gray-200 rounded-lg text-sm text-gray-500"><option value="7">Last 7 Days</option><option value="30">Last 30 Days</option><option value="90">Last 90 Days</option><option value="365">This Year</option></select></div>
                        <!-- No export button for staff -->
                    </div>
                    <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-6"><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Total Sales</p><p id="reportTotalSales" class="text-2xl font-bold text-gray-800">$0</p><p class="text-xs text-green-500 mt-1"><i class="fas fa-arrow-up"></i> 15% vs last period</p></div><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Items Sold</p><p id="reportItemsSold" class="text-2xl font-bold text-gray-800">0</p><p class="text-xs text-green-500 mt-1"><i class="fas fa-arrow-up"></i> 8% vs last period</p></div><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Stock Received</p><p id="reportStockReceived" class="text-2xl font-bold text-gray-800">0</p><p class="text-xs text-blue-500 mt-1"><i class="fas fa-arrow-right"></i> On track</p></div><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Avg Order Value</p><p id="reportAvgOrder" class="text-2xl font-bold text-gray-800">$0</p><p class="text-xs text-red-500 mt-1"><i class="fas fa-arrow-down"></i> 3% vs last period</p></div></div>
                    <div class="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-6"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Revenue Trend</h3><canvas id="revenueChart" height="160"></canvas></div><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Top Selling Products</h3><div id="topProductsContainer" class="space-y-3"></div></div></div>
                </div>
            </div>
        </main>
    </div>

    <!-- Modal and Toast same as admin -->
    <div id="modal" class="hidden fixed inset-0 z-[100] flex items-center justify-center p-4"><div class="modal-overlay absolute inset-0" onclick="closeModal()"></div><div class="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto fade-in"><div class="flex items-center justify-between p-6 border-b border-gray-100"><h3 id="modalTitle" class="text-lg font-bold text-gray-800">Modal Title</h3><button onclick="closeModal()" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors"><i class="fas fa-times"></i></button></div><div id="modalBody" class="p-6"></div></div></div>
    <div id="toast" class="hidden fixed bottom-6 right-6 z-[200] bg-gray-900 text-white px-5 py-3 rounded-xl shadow-lg text-sm font-medium flex items-center gap-3 fade-in"><i id="toastIcon" class="fas fa-check-circle text-green-400"></i><span id="toastMsg">Success!</span></div>

    <script>
        let currentRole = 'staff';
        let currentPage = 'dashboard';
        let products = [];
        let suppliers = [];
        let salesHistory = [];
        let adjustmentHistory = [];
        let salesChart, categoryChart, revenueChart;

        auth.onAuthStateChanged(async (user) => {
            if (!user) {
                window.location.href = 'login.php';
                return;
            }
            await loadAllData();
            navigateTo('dashboard');
            initCharts();
            refreshDashboardData();
            populateSelects();
        });

        async function loadAllData() {
            await Promise.all([loadProducts(), loadSuppliers(), loadSales(), loadAdjustments()]);
        }

        async function loadProducts() {
            const snapshot = await db.collection('products').orderBy('name').get();
            products = snapshot.docs.map(doc => ({ id: doc.id, ...doc.data() }));
            renderProducts();
            updateDashboardStats();
        }

        async function loadSuppliers() {
            const snapshot = await db.collection('suppliers').orderBy('name').get();
            suppliers = snapshot.docs.map(doc => ({ id: doc.id, ...doc.data() }));
            renderSuppliers();
        }

        async function loadSales() {
            const snapshot = await db.collection('sales').orderBy('date', 'desc').limit(50).get();
            salesHistory = snapshot.docs.map(doc => ({ id: doc.id, ...doc.data() }));
            renderSales();
        }

        async function loadAdjustments() {
            const snapshot = await db.collection('adjustments').orderBy('date', 'desc').limit(50).get();
            adjustmentHistory = snapshot.docs.map(doc => ({ id: doc.id, ...doc.data() }));
            renderAdjustments();
        }

        async function addOrUpdateProduct(productData, id = null) {
            if (id) await db.collection('products').doc(id).update(productData);
            else await db.collection('products').add(productData);
            await loadProducts();
        }

        // NO deleteProduct function for staff

        async function addSale(saleData) {
            await db.collection('sales').add(saleData);
            await loadSales();
        }

        async function addAdjustment(adjustmentData) {
            await db.collection('adjustments').add(adjustmentData);
            await loadAdjustments();
        }

        async function updateProductStock(productId, newStock) {
            const product = products.find(p => p.id === productId);
            const status = newStock === 0 ? 'Out of Stock' : newStock <= product.minStock ? 'Low Stock' : 'In Stock';
            await db.collection('products').doc(productId).update({ stock: newStock, status });
            await loadProducts();
        }

        function updateDashboardStats() {
            document.getElementById('statTotalProducts').innerText = products.length;
            const totalValue = products.reduce((sum, p) => sum + (p.price * p.stock), 0);
            document.getElementById('statInventoryValue').innerText = '$' + totalValue.toLocaleString();
            const lowStockCount = products.filter(p => p.status === 'Low Stock').length;
            const outOfStockCount = products.filter(p => p.status === 'Out of Stock').length;
            document.getElementById('statLowStock').innerText = lowStockCount;
            document.getElementById('statOutOfStock').innerText = outOfStockCount;
        }

        function renderProducts() {
            const tbody = document.getElementById('productsTableBody');
            const search = document.getElementById('productSearch')?.value?.toLowerCase() || '';
            const cat = document.getElementById('categoryFilter')?.value || '';
            const filtered = products.filter(p => {
                const matchSearch = p.name.toLowerCase().includes(search) || p.sku.toLowerCase().includes(search);
                const matchCat = !cat || p.category === cat;
                return matchSearch && matchCat;
            });
            tbody.innerHTML = filtered.map(p => {
                const statusClass = p.status === 'In Stock' ? 'bg-green-50 text-green-700' : p.status === 'Low Stock' ? 'bg-amber-50 text-amber-700' : 'bg-red-50 text-red-700';
                return `<tr class="table-row border-b border-gray-50">
                    <td class="py-3 px-4"><div class="flex items-center gap-3"><div class="w-9 h-9 bg-indigo-50 rounded-lg flex items-center justify-center"><i class="fas fa-box text-indigo-400 text-sm"></i></div><span class="text-sm font-medium text-gray-800">${p.name}</span></div></td>
                    <td class="py-3 px-4 text-sm text-gray-500 font-mono">${p.sku}</td>
                    <td class="py-3 px-4 text-sm text-gray-500">${p.category}</td>
                    <td class="py-3 px-4 text-sm font-semibold text-gray-700">$${p.price.toFixed(2)}</td>
                    <td class="py-3 px-4 text-sm font-semibold text-gray-700">${p.stock}</td>
                    <td class="py-3 px-4"><span class="badge ${statusClass}">${p.status}</span></td>
                    <td class="py-3 px-4"><div class="flex items-center gap-1">
                        <button onclick="openProductModal('${p.id}')" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-blue-50 text-gray-400 hover:text-blue-500 transition-colors" title="Edit"><i class="fas fa-pen text-xs"></i></button>
                        <!-- No delete button for staff -->
                    </div></td>
                </tr>`;
            }).join('');
        }

        function renderSuppliers() {
            const grid = document.getElementById('suppliersGrid');
            const search = document.getElementById('supplierSearch')?.value?.toLowerCase() || '';
            const filtered = suppliers.filter(s => s.name.toLowerCase().includes(search) || s.contact.toLowerCase().includes(search));
            grid.innerHTML = filtered.map(s => {
                const stars = '★'.repeat(Math.floor(s.rating)) + (s.rating % 1 >= 0.5 ? '½' : '');
                return `<div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6 card-hover">
                    <div class="flex items-start justify-between mb-4"><div class="flex items-center gap-3"><div class="w-12 h-12 bg-gradient-to-br from-indigo-400 to-purple-500 rounded-xl flex items-center justify-center text-white font-bold text-sm">${s.name.split(' ').map(w=>w[0]).join('').slice(0,2)}</div><div><h4 class="font-semibold text-gray-800">${s.name}</h4><p class="text-xs text-gray-400">${s.contact}</p></div></div><span class="badge ${s.status==='Active'?'bg-green-50 text-green-700':'bg-gray-100 text-gray-500'}">${s.status}</span></div>
                    <div class="space-y-2 mb-4"><p class="text-sm text-gray-500 flex items-center gap-2"><i class="fas fa-envelope w-4 text-gray-400 text-xs"></i>${s.email}</p><p class="text-sm text-gray-500 flex items-center gap-2"><i class="fas fa-phone w-4 text-gray-400 text-xs"></i>${s.phone}</p><p class="text-sm text-gray-500 flex items-center gap-2"><i class="fas fa-box w-4 text-gray-400 text-xs"></i>${s.products || 0} products supplied</p></div>
                    <div class="flex items-center justify-between pt-3 border-t border-gray-100"><div class="flex items-center gap-1"><span class="text-amber-400 text-sm">${stars}</span><span class="text-sm text-gray-500 ml-1">${s.rating}</span></div><div class="flex items-center gap-1"><!-- No edit/delete buttons for staff --></div></div>
                </div>`;
            }).join('');
        }

        function renderSales() {
            const tbody = document.getElementById('salesTableBody');
            tbody.innerHTML = salesHistory.map(s => {
                const typeClass = s.type === 'Sale' ? 'bg-green-50 text-green-700' : s.type === 'Return' ? 'bg-amber-50 text-amber-700' : 'bg-blue-50 text-blue-700';
                return `<tr class="table-row border-b border-gray-50"><td class="py-3 px-4 text-sm text-gray-500">${s.date}</td><td class="py-3 px-4 text-sm font-medium text-gray-800">${s.product}</td><td class="py-3 px-4"><span class="badge ${typeClass}">${s.type}</span></td><td class="py-3 px-4 text-sm font-semibold text-gray-700">${s.qty}</td><td class="py-3 px-4 text-sm text-gray-500">${s.by}</td></tr>`;
            }).join('');
        }

        function renderAdjustments() {
            const tbody = document.getElementById('adjustmentTableBody');
            tbody.innerHTML = adjustmentHistory.map(a => {
                const typeClass = a.type === 'Add' ? 'bg-green-50 text-green-700' : a.type === 'Subtract' ? 'bg-red-50 text-red-700' : 'bg-blue-50 text-blue-700';
                const changeClass = a.change.startsWith('+') ? 'text-green-600' : a.change.startsWith('-') ? 'text-red-600' : 'text-blue-600';
                return `<tr class="table-row border-b border-gray-50"><td class="py-3 px-4 text-sm text-gray-500">${a.date}</td><td class="py-3 px-4 text-sm font-medium text-gray-800">${a.product}</td><td class="py-3 px-4"><span class="badge ${typeClass}">${a.type}</span></td><td class="py-3 px-4 text-sm font-bold ${changeClass}">${a.change}</td><td class="py-3 px-4 text-sm text-gray-500">${a.reason}</td><td class="py-3 px-4 text-sm text-gray-500">${a.by}</td></tr>`;
            }).join('');
        }

        function refreshDashboardData() {
            const recent = [...salesHistory].slice(0,5);
            document.getElementById('recentActivityContainer').innerHTML = recent.map(s => `<div class="flex items-start gap-3"><div class="w-8 h-8 bg-green-50 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5"><i class="fas fa-cart-shopping text-green-500 text-xs"></i></div><div><p class="text-sm text-gray-700"><span class="font-medium">${s.type}</span> - ${s.qty} units of "${s.product}"</p><p class="text-xs text-gray-400 mt-0.5">${s.date}</p></div></div>`).join('');
            const lowStockItems = products.filter(p => p.status === 'Low Stock' || p.status === 'Out of Stock');
            document.getElementById('lowStockContainer').innerHTML = lowStockItems.map(p => `<div class="flex items-center justify-between p-3 bg-amber-50 rounded-lg border border-amber-100"><div class="flex items-center gap-3"><div class="w-10 h-10 bg-white rounded-lg flex items-center justify-center shadow-sm"><i class="fas fa-box text-gray-500"></i></div><div><p class="text-sm font-medium text-gray-800">${p.name}</p><p class="text-xs text-gray-400">SKU: ${p.sku}</p></div></div><div class="text-right"><p class="text-sm font-bold text-amber-500">${p.stock} left</p><p class="text-xs text-gray-400">Min: ${p.minStock}</p></div></div>`).join('');
            updateDashboardStats();
        }

        // Product modal (edit only, no delete)
        window.openProductModal = async (id = null) => {
            const product = id ? products.find(p => p.id === id) : null;
            document.getElementById('modalTitle').textContent = product ? 'Edit Product' : 'Add New Product';
            document.getElementById('modalBody').innerHTML = `<div class="space-y-4">
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Product Name</label><input id="pName" type="text" value="${product?.name || ''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div class="grid grid-cols-2 gap-4">
                    <div><label class="block text-sm font-medium text-gray-700 mb-1">SKU</label><input id="pSku" type="text" value="${product?.sku || ''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                    <div><label class="block text-sm font-medium text-gray-700 mb-1">Category</label><select id="pCat" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option ${product?.category === 'Electronics' ? 'selected' : ''}>Electronics</option><option ${product?.category === 'Peripherals' ? 'selected' : ''}>Peripherals</option><option ${product?.category === 'Accessories' ? 'selected' : ''}>Accessories</option><option ${product?.category === 'Audio' ? 'selected' : ''}>Audio</option></select></div>
                </div>
                <div class="grid grid-cols-2 gap-4">
                    <div><label class="block text-sm font-medium text-gray-700 mb-1">Price ($)</label><input id="pPrice" type="number" step="0.01" value="${product?.price || ''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                    <div><label class="block text-sm font-medium text-gray-700 mb-1">Stock Quantity</label><input id="pStock" type="number" value="${product?.stock || 0}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                </div>
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Min Stock Level</label><input id="pMinStock" type="number" value="${product?.minStock || 0}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div class="flex gap-3 pt-2">
                    <button onclick="saveProduct('${id}')" class="flex-1 btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Save Product</button>
                    <button onclick="closeModal()" class="px-4 py-2.5 border border-gray-200 rounded-lg text-sm text-gray-600 hover:bg-gray-50">Cancel</button>
                </div>
            </div>`;
            document.getElementById('modal').classList.remove('hidden');
        };

        window.saveProduct = async (id) => {
            const name = document.getElementById('pName').value;
            const sku = document.getElementById('pSku').value;
            const category = document.getElementById('pCat').value;
            const price = parseFloat(document.getElementById('pPrice').value);
            const stock = parseInt(document.getElementById('pStock').value);
            const minStock = parseInt(document.getElementById('pMinStock').value);
            if (!name || !sku) { showToast('Please fill required fields.', 'error'); return; }
            const status = stock === 0 ? 'Out of Stock' : stock <= minStock ? 'Low Stock' : 'In Stock';
            const productData = { name, sku, category, price, stock, minStock, status };
            if (id && id !== 'null') await addOrUpdateProduct(productData, id);
            else await addOrUpdateProduct(productData);
            closeModal();
            showToast(id && id !== 'null' ? 'Product updated!' : 'Product added!');
        };

        // Sales and stock functions (same as admin, but using currentUser.name)
        function populateSelects() {
            const productOptions = products.map(p => `<option value="${p.id}">${p.name} (${p.stock} in stock)</option>`).join('');
            document.getElementById('saleProduct').innerHTML = '<option value="">Select Product</option>' + productOptions;
            document.getElementById('adjustProduct').innerHTML = '<option value="">Select Product</option>' + productOptions;
            document.getElementById('adjustProduct').addEventListener('change', function() {
                const p = products.find(x => x.id === this.value);
                const disp = document.getElementById('currentStockDisplay');
                if(p) { disp.classList.remove('hidden'); document.getElementById('currentStockVal').innerText = p.stock; }
                else disp.classList.add('hidden');
            });
        }

        window.recordSale = async () => {
            const productId = document.getElementById('saleProduct').value;
            const qty = parseInt(document.getElementById('saleQty').value);
            const type = document.getElementById('saleType').value;
            if (!productId || !qty || qty <= 0) { showToast('Please select a product and valid quantity.', 'error'); return; }
            const product = products.find(p => p.id === productId);
            if (!product) { showToast('Product not found.', 'error'); return; }
            if (type === 'sale' && product.stock < qty) { showToast('Insufficient stock!', 'error'); return; }
            let newStock = product.stock;
            if (type === 'sale' || type === 'return' || type === 'transfer') newStock -= qty;
            await updateProductStock(productId, newStock);
            const typeLabels = { sale:'Sale', return:'Return', transfer:'Transfer' };
            const dateStr = new Date().toISOString().replace('T', ' ').substring(0, 19);
            await addSale({ product: product.name, type: typeLabels[type], qty, by: currentUser.name, date: dateStr, notes: document.getElementById('saleNotes').value });
            showToast('Transaction recorded successfully!');
        };

        window.adjustStock = async () => {
            const productId = document.getElementById('adjustProduct').value;
            const type = document.getElementById('adjustType').value;
            const qty = parseInt(document.getElementById('adjustQty').value);
            const reason = document.getElementById('adjustReason').value;
            if (!productId || qty < 0) { showToast('Please select a product and enter a valid quantity.', 'error'); return; }
            const product = products.find(p => p.id === productId);
            if (!product) { showToast('Product not found.', 'error'); return; }
            let newStock = product.stock;
            let changeStr = '';
            if (type === 'add') { newStock += qty; changeStr = `+${qty}`; }
            else if (type === 'subtract') { newStock = Math.max(0, newStock - qty); changeStr = `-${qty}`; }
            else { newStock = qty; changeStr = `=${qty}`; }
            await updateProductStock(productId, newStock);
            const typeLabels = { add:'Add', subtract:'Subtract', set:'Set' };
            const dateStr = new Date().toISOString().replace('T', ' ').substring(0, 19);
            await addAdjustment({ product: product.name, type: typeLabels[type], change: changeStr, reason, by: currentUser.name, date: dateStr });
            showToast('Stock adjusted successfully!');
        };

        // Navigation, modal, toast, charts (same as admin, simplified)
        function navigateTo(page) {
            currentPage = page;
            document.querySelectorAll('.page-content').forEach(p => p.classList.add('hidden'));
            document.getElementById('page-' + page).classList.remove('hidden');
            document.querySelectorAll('.sidebar-link').forEach(l => l.classList.remove('active'));
            document.querySelector(`[data-page="${page}"]`).classList.add('active');
            const titles = { dashboard:'Dashboard', products:'Products', suppliers:'Suppliers', sales:'Sales / Stock Out', stock:'Stock Adjustment', reports:'Reports' };
            document.getElementById('pageTitle').innerText = titles[page];
            if (page === 'products') renderProducts();
            if (page === 'suppliers') renderSuppliers();
            if (page === 'sales') renderSales();
            if (page === 'stock') renderAdjustments();
            if (page === 'dashboard') refreshDashboardData();
            if (page === 'reports') initReportChart();
            if (window.innerWidth < 1024) { document.getElementById('sidebar').classList.remove('open'); document.getElementById('sidebarOverlay').classList.add('hidden'); }
        }

        function toggleSidebar() { document.getElementById('sidebar').classList.toggle('open'); document.getElementById('sidebarOverlay').classList.toggle('hidden'); }
        function showNotifications() { /* same as admin */ }
        function closeModal() { document.getElementById('modal').classList.add('hidden'); }
        function showToast(msg, type='success') { /* same as admin */ }

        function initCharts() { /* same as admin, simplified */ }
        function initReportChart() { /* same as admin */ }

        window.filterProducts = () => renderProducts();
    </script>
</body>
</html>