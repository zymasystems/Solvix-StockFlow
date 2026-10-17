<?php
// admin-dashboard.php – Complete, fully functional version
require_once './includes/auth-check.php';
requireAdmin();
$pageTitle = 'Admin Dashboard - StockFlow';

$userName = htmlspecialchars($_SESSION['name'] ?? 'Admin User');
$userEmail = $_SESSION['email'] ?? '';
$userRole = 'admin';
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
            role: "admin"
        };
    </script>
    
    <style>
        /* (Same styles as before – keep your existing CSS) */
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
        .loading-spinner { display: inline-block; width: 20px; height: 20px; border: 2px solid #e2e8f0; border-top-color: #6366f1; border-radius: 50%; animation: spin 0.6s linear infinite; }
        @keyframes spin { to { transform: rotate(360deg); } }
    </style>
</head>
<body class="bg-gray-50">
    <div id="dashboardScreen" class="min-h-screen">
        <!-- Mobile Header -->
        <div class="lg:hidden fixed top-0 left-0 right-0 bg-white shadow-sm z-40 px-4 py-3 flex items-center justify-between mobile-menu-btn">
            <button onclick="toggleSidebar()" class="text-gray-600 text-xl"><i class="fas fa-bars"></i></button>
            <div class="flex items-center gap-2"><div class="w-8 h-8 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-lg flex items-center justify-center"><i class="fas fa-boxes-stacked text-white text-sm"></i></div><span class="font-bold text-gray-800">StockFlow</span></div>
            <div class="relative cursor-pointer" onclick="showNotifications()"><i class="fas fa-bell text-gray-500"></i><span class="notification-badge">3</span></div>
        </div>
        <div id="sidebarOverlay" class="hidden fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden" onclick="toggleSidebar()"></div>

        <!-- Sidebar (unchanged) -->
        <aside id="sidebar" class="sidebar fixed left-0 top-0 h-full w-64 bg-white shadow-lg z-50 flex flex-col transition-transform duration-300">
            <div class="p-6 border-b border-gray-100"><div class="flex items-center gap-3"><div class="w-10 h-10 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-md"><i class="fas fa-boxes-stacked text-white"></i></div><div><h1 class="font-bold text-gray-900 text-lg">StockFlow</h1><p class="text-xs text-gray-400">Admin Portal</p></div></div></div>
            <nav class="flex-1 py-4 overflow-y-auto">
                <p class="px-6 text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">Main</p>
                <a onclick="navigateTo('dashboard')" class="sidebar-link active flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="dashboard"><i class="fas fa-chart-pie w-5"></i> Dashboard</a>
                <a onclick="navigateTo('products')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="products"><i class="fas fa-box w-5"></i> Products</a>
                <a onclick="navigateTo('suppliers')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="suppliers"><i class="fas fa-truck w-5"></i> Suppliers</a>
                <a onclick="navigateTo('sales')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="sales"><i class="fas fa-cart-shopping w-5"></i> Sales / Stock Out</a>
                <a onclick="navigateTo('stock')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="stock"><i class="fas fa-warehouse w-5"></i> Stock Adjustment</a>
                <p class="px-6 text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2 mt-4">Analytics</p>
                <a onclick="navigateTo('reports')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="reports"><i class="fas fa-chart-bar w-5"></i> Reports</a>
                <p class="px-6 text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2 mt-4">Administration</p>
                <a onclick="navigateTo('users')" class="sidebar-link flex items-center gap-3 px-6 py-3 text-sm text-gray-600 cursor-pointer" data-page="users"><i class="fas fa-users-gear w-5"></i> Manage Users</a>
            </nav>
            <div class="p-4 border-t border-gray-100"><div class="flex items-center gap-3 p-2"><div id="userAvatar" class="w-10 h-10 bg-gradient-to-br from-indigo-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold text-sm">AD</div><div class="flex-1 min-w-0"><p id="userName" class="text-sm font-semibold text-gray-800 truncate"><?php echo $userName; ?></p><p id="userRole" class="text-xs text-gray-400 flex items-center gap-1"><span class="w-2 h-2 bg-green-400 rounded-full pulse-dot"></span> Admin</p></div><a href="logout.php" class="text-gray-400 hover:text-red-500 transition-colors tooltip" data-tip="Logout"><i class="fas fa-right-from-bracket"></i></a></div></div>
        </aside>

        <!-- Main Content -->
        <main id="mainContent" class="main-content lg:ml-64 min-h-screen pt-0 lg:pt-0" style="margin-left:256px">
            <div class="lg:hidden h-14"></div>
            <header class="bg-white shadow-sm px-6 py-4 flex items-center justify-between sticky top-0 z-30">
                <div><h2 id="pageTitle" class="text-xl font-bold text-gray-800">Dashboard</h2><p id="pageSubtitle" class="text-sm text-gray-400">Overview of your inventory</p></div>
                <div class="flex items-center gap-4"><div class="hidden sm:block relative"><i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm"></i><input type="text" placeholder="Search..." class="pl-9 pr-4 py-2 bg-gray-50 border border-gray-200 rounded-lg text-sm w-56 focus:w-72 transition-all"></div><div class="relative cursor-pointer hidden lg:block" onclick="showNotifications()"><i class="fas fa-bell text-gray-500 text-lg"></i><span class="notification-badge">3</span></div><div id="roleBadge" class="badge bg-indigo-100 text-indigo-700 hidden sm:inline">ADMIN</div></div>
            </header>

            <div class="p-6">
                <!-- Dashboard Page (same structure as before) -->
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
                    <div class="grid grid-cols-1 xl:grid-cols-2 gap-6"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Recent Activity</h3><div id="recentActivityContainer" class="space-y-4"></div></div><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Low Stock Alerts</h3><div id="lowStockContainer" class="space-y-3"></div></div></div>
                </div>

                <!-- Products Page (same as before) -->
                <div id="page-products" class="page-content hidden fade-in">
                    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6"><div class="flex items-center gap-3 w-full sm:w-auto"><div class="relative flex-1 sm:flex-none"><i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm"></i><input type="text" id="productSearch" placeholder="Search products..." class="pl-9 pr-4 py-2 bg-white border border-gray-200 rounded-lg text-sm w-full sm:w-64" oninput="filterProducts()"></div><select id="categoryFilter" class="py-2 px-3 bg-white border border-gray-200 rounded-lg text-sm text-gray-500" onchange="filterProducts()"><option value="">All Categories</option><option>Electronics</option><option>Peripherals</option><option>Accessories</option><option>Audio</option></select></div><button onclick="openProductModal()" class="btn-primary text-white px-4 py-2 rounded-lg text-sm font-medium whitespace-nowrap"><i class="fas fa-plus mr-2"></i>Add Product</button></div>
                    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden"><div class="overflow-x-auto"><table class="w-full"><thead><tr class="bg-gray-50 border-b border-gray-100"><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Product</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">SKU</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Category</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Price</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Stock</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Status</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Actions</th></tr></thead><tbody id="productsTableBody"></tbody></table></div></div>
                </div>

                <!-- Suppliers Page -->
                <div id="page-suppliers" class="page-content hidden fade-in">
                    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6"><div class="relative flex-1 sm:flex-none"><i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm"></i><input type="text" id="supplierSearch" placeholder="Search suppliers..." class="pl-9 pr-4 py-2 bg-white border border-gray-200 rounded-lg text-sm w-full sm:w-64" oninput="renderSuppliers()"></div><button onclick="openSupplierModal()" class="btn-primary text-white px-4 py-2 rounded-lg text-sm font-medium whitespace-nowrap"><i class="fas fa-plus mr-2"></i>Add Supplier</button></div>
                    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6" id="suppliersGrid"></div>
                </div>

                <!-- Sales Page -->
                <div id="page-sales" class="page-content hidden fade-in">
                    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6"><div class="xl:col-span-1"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4"><i class="fas fa-cart-shopping mr-2 text-indigo-500"></i>Record Sale / Stock Out</h3><div class="space-y-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Product</label><select id="saleProduct" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="">Select Product</option></select></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Quantity</label><input type="number" id="saleQty" min="1" value="1" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Type</label><select id="saleType" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="sale">Sale</option><option value="return">Return/Damaged</option><option value="transfer">Transfer</option></select></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Notes</label><textarea id="saleNotes" rows="2" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm" placeholder="Optional notes..."></textarea></div><button onclick="recordSale()" class="w-full btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Record Transaction</button></div></div></div><div class="xl:col-span-2"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Recent Transactions</h3><div class="overflow-x-auto"><table class="w-full"><thead><tr class="bg-gray-50 border-b border-gray-100"><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Date</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Product</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Type</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Qty</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">By</th></tr></thead><tbody id="salesTableBody"></tbody></table></div></div></div></div>
                </div>

                <!-- Stock Adjustment Page -->
                <div id="page-stock" class="page-content hidden fade-in">
                    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6"><div class="xl:col-span-1"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4"><i class="fas fa-sliders mr-2 text-indigo-500"></i>Adjust Stock Level</h3><div class="space-y-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Product</label><select id="adjustProduct" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="">Select Product</option></select></div><div id="currentStockDisplay" class="p-3 bg-blue-50 rounded-lg border border-blue-100 hidden"><p class="text-sm text-blue-700">Current Stock: <span id="currentStockVal" class="font-bold">0</span> units</p></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Adjustment Type</label><select id="adjustType" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option value="add">Add Stock (Restock)</option><option value="subtract">Remove Stock</option><option value="set">Set Exact Amount</option></select></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Quantity</label><input type="number" id="adjustQty" min="0" value="0" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Reason</label><select id="adjustReason" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option>New Shipment Received</option><option>Inventory Count Correction</option><option>Damaged Goods</option><option>Return from Customer</option><option>Other</option></select></div><button onclick="adjustStock()" class="w-full btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Apply Adjustment</button></div></div></div><div class="xl:col-span-2"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Adjustment History</h3><div class="overflow-x-auto"><table class="w-full"><thead><tr class="bg-gray-50 border-b border-gray-100"><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Date</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Product</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Type</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Change</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Reason</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">By</th></tr></thead><tbody id="adjustmentTableBody"></tbody></table></div></div></div></div>
                </div>

                <!-- Reports Page -->
                <div id="page-reports" class="page-content hidden fade-in">
                    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6"><div class="flex items-center gap-3"><select id="reportPeriod" class="py-2 px-3 bg-white border border-gray-200 rounded-lg text-sm text-gray-500"><option value="7">Last 7 Days</option><option value="30">Last 30 Days</option><option value="90">Last 90 Days</option><option value="365">This Year</option></select></div><button id="exportBtn" onclick="exportReport()" class="btn-primary text-white px-4 py-2 rounded-lg text-sm font-medium whitespace-nowrap"><i class="fas fa-download mr-2"></i>Export Report</button></div>
                    <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-6"><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Total Sales</p><p id="reportTotalSales" class="text-2xl font-bold text-gray-800">$0</p><p class="text-xs text-green-500 mt-1"><i class="fas fa-arrow-up"></i> 15% vs last period</p></div><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Items Sold</p><p id="reportItemsSold" class="text-2xl font-bold text-gray-800">0</p><p class="text-xs text-green-500 mt-1"><i class="fas fa-arrow-up"></i> 8% vs last period</p></div><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Stock Received</p><p id="reportStockReceived" class="text-2xl font-bold text-gray-800">0</p><p class="text-xs text-blue-500 mt-1"><i class="fas fa-arrow-right"></i> On track</p></div><div class="bg-white rounded-xl p-5 shadow-sm border border-gray-100"><p class="text-sm text-gray-400 mb-1">Avg Order Value</p><p id="reportAvgOrder" class="text-2xl font-bold text-gray-800">$0</p><p class="text-xs text-red-500 mt-1"><i class="fas fa-arrow-down"></i> 3% vs last period</p></div></div>
                    <div class="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-6"><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Revenue Trend</h3><canvas id="revenueChart" height="160"></canvas></div><div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"><h3 class="font-semibold text-gray-800 mb-4">Top Selling Products</h3><div id="topProductsContainer" class="space-y-3"></div></div></div>
                </div>

                <!-- Users Page -->
                <div id="page-users" class="page-content hidden fade-in">
                    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6"><div class="relative flex-1 sm:flex-none"><i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm"></i><input type="text" id="userSearch" placeholder="Search users..." class="pl-9 pr-4 py-2 bg-white border border-gray-200 rounded-lg text-sm w-full sm:w-64" oninput="renderUsers()"></div><button onclick="openUserModal()" class="btn-primary text-white px-4 py-2 rounded-lg text-sm font-medium whitespace-nowrap"><i class="fas fa-user-plus mr-2"></i>Add User</button></div>
                    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden"><div class="overflow-x-auto"><table class="w-full"><thead><tr class="bg-gray-50 border-b border-gray-100"><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">User</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Email</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Role</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Status</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Last Active</th><th class="text-left py-3 px-4 text-xs font-semibold text-gray-500 uppercase">Actions</th></tr></thead><tbody id="usersTableBody"></tbody></table></div></div>
                </div>
            </div>
        </main>
    </div>

    <!-- Modal & Toast -->
    <div id="modal" class="hidden fixed inset-0 z-[100] flex items-center justify-center p-4"><div class="modal-overlay absolute inset-0" onclick="closeModal()"></div><div class="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto fade-in"><div class="flex items-center justify-between p-6 border-b border-gray-100"><h3 id="modalTitle" class="text-lg font-bold text-gray-800">Modal Title</h3><button onclick="closeModal()" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors"><i class="fas fa-times"></i></button></div><div id="modalBody" class="p-6"></div></div></div>
    <div id="toast" class="hidden fixed bottom-6 right-6 z-[200] bg-gray-900 text-white px-5 py-3 rounded-xl shadow-lg text-sm font-medium flex items-center gap-3 fade-in"><i id="toastIcon" class="fas fa-check-circle text-green-400"></i><span id="toastMsg">Success!</span></div>

    <script>
        // ==================== GLOBAL STATE ====================
        let currentRole = 'admin';
        let currentPage = 'dashboard';
        let products = [], suppliers = [], appUsers = [], salesHistory = [], adjustmentHistory = [];
        let salesChart, categoryChart, revenueChart;
        let isLoading = false;

        // ==================== AUTH & INIT ====================
        auth.onAuthStateChanged(async (user) => {
            if (!user) { window.location.href = 'login.php'; return; }
            await loadAllData();
            navigateTo('dashboard');
            initCharts();
            refreshDashboardData();
            populateSelects();
        });

        // ==================== FIRESTORE CRUD ====================
        async function loadAllData() { await Promise.all([loadProducts(), loadSuppliers(), loadUsers(), loadSales(), loadAdjustments()]); }
        async function loadProducts() { const snap = await db.collection('products').orderBy('name').get(); products = snap.docs.map(d => ({ id: d.id, ...d.data() })); renderProducts(); updateDashboardStats(); }
        async function loadSuppliers() { const snap = await db.collection('suppliers').orderBy('name').get(); suppliers = snap.docs.map(d => ({ id: d.id, ...d.data() })); renderSuppliers(); }
        async function loadUsers() { const snap = await db.collection('users').get(); appUsers = snap.docs.map(d => ({ id: d.id, ...d.data() })); renderUsers(); }
        async function loadSales() { const snap = await db.collection('sales').orderBy('date', 'desc').limit(50).get(); salesHistory = snap.docs.map(d => ({ id: d.id, ...d.data() })); renderSales(); refreshReportStats(); }
        async function loadAdjustments() { const snap = await db.collection('adjustments').orderBy('date', 'desc').limit(50).get(); adjustmentHistory = snap.docs.map(d => ({ id: d.id, ...d.data() })); renderAdjustments(); }

async function addOrUpdateProduct(data, id = null) {
    try {
        if (id) {
            await db.collection('products').doc(id).update(data);
        } else {
            await db.collection('products').add(data);
        }
        await loadProducts();
        return { success: true };
    } catch (err) {
        console.error('Firestore product error:', err);
        showToast('Failed to save product: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function deleteProductDoc(id) {
    try {
        await db.collection('products').doc(id).delete();
        await loadProducts();
        return { success: true };
    } catch (err) {
        console.error('Delete product error:', err);
        showToast('Failed to delete product: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function addOrUpdateSupplier(data, id = null) {
    try {
        if (id) {
            await db.collection('suppliers').doc(id).update(data);
        } else {
            await db.collection('suppliers').add(data);
        }
        await loadSuppliers();
        return { success: true };
    } catch (err) {
        console.error('Firestore supplier error:', err);
        showToast('Failed to save supplier: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function deleteSupplierDoc(id) {
    try {
        await db.collection('suppliers').doc(id).delete();
        await loadSuppliers();
        return { success: true };
    } catch (err) {
        console.error('Delete supplier error:', err);
        showToast('Failed to delete supplier: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function addOrUpdateUser(data, id = null) {
    try {
        if (id) {
            await db.collection('users').doc(id).update(data);
        } else {
            await db.collection('users').add(data);
        }
        await loadUsers();
        return { success: true };
    } catch (err) {
        console.error('Firestore user error:', err);
        showToast('Failed to save user: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function deleteUserDoc(id) {
    try {
        await db.collection('users').doc(id).delete();
        await loadUsers();
        return { success: true };
    } catch (err) {
        console.error('Delete user error:', err);
        showToast('Failed to delete user: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function addSale(data) {
    try {
        await db.collection('sales').add(data);
        await loadSales();
        refreshDashboardData();
        return { success: true };
    } catch (err) {
        console.error('Sale error:', err);
        showToast('Failed to record sale: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function addAdjustment(data) {
    try {
        await db.collection('adjustments').add(data);
        await loadAdjustments();
        refreshDashboardData();
        return { success: true };
    } catch (err) {
        console.error('Adjustment error:', err);
        showToast('Failed to adjust stock: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}

async function updateProductStock(productId, newStock, minStock) {
    try {
        const status = getStockStatus(newStock, minStock);
        await db.collection('products').doc(productId).update({
            stock: newStock,
            status: status
        });
        await loadProducts();
        return { success: true };
    } catch (err) {
        console.error('Stock update error:', err);
        showToast('Failed to update stock: ' + err.message, 'error');
        return { success: false, error: err.message };
    }
}
        function getStockStatus(stock, minStock) { if(stock===0) return 'Out of Stock'; if(stock<=minStock) return 'Low Stock'; return 'In Stock'; }
        function updateDashboardStats() {
            document.getElementById('statTotalProducts').innerText = products.length;
            const totalValue = products.reduce((s,p)=> s + (p.price * p.stock), 0);
            document.getElementById('statInventoryValue').innerText = '$' + totalValue.toLocaleString();
            document.getElementById('statLowStock').innerText = products.filter(p=>p.status==='Low Stock').length;
            document.getElementById('statOutOfStock').innerText = products.filter(p=>p.status==='Out of Stock').length;
        }

        // ==================== UI RENDERING ====================
        function renderProducts() {
            const tbody = document.getElementById('productsTableBody');
            const search = document.getElementById('productSearch')?.value?.toLowerCase() || '';
            const cat = document.getElementById('categoryFilter')?.value || '';
            const filtered = products.filter(p => (p.name.toLowerCase().includes(search) || p.sku.toLowerCase().includes(search)) && (!cat || p.category===cat));
            tbody.innerHTML = filtered.map(p => `<tr class="table-row border-b border-gray-50">
                <td class="py-3 px-4"><div class="flex items-center gap-3"><div class="w-9 h-9 bg-indigo-50 rounded-lg flex items-center justify-center"><i class="fas fa-box text-indigo-400 text-sm"></i></div><span class="text-sm font-medium text-gray-800">${escapeHtml(p.name)}</span></div></td>
                <td class="py-3 px-4 text-sm text-gray-500 font-mono">${escapeHtml(p.sku)}</td>
                <td class="py-3 px-4 text-sm text-gray-500">${escapeHtml(p.category)}</td>
                <td class="py-3 px-4 text-sm font-semibold text-gray-700">$${p.price.toFixed(2)}</td>
                <td class="py-3 px-4 text-sm font-semibold text-gray-700">${p.stock}</td>
                <td class="py-3 px-4"><span class="badge ${p.status==='In Stock'?'bg-green-50 text-green-700':p.status==='Low Stock'?'bg-amber-50 text-amber-700':'bg-red-50 text-red-700'}">${p.status}</span></td>
                <td class="py-3 px-4"><div class="flex items-center gap-1"><button onclick="openProductModal('${p.id}')" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-blue-50 text-gray-400 hover:text-blue-500"><i class="fas fa-pen text-xs"></i></button><button onclick="deleteProduct('${p.id}')" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-500"><i class="fas fa-trash-alt text-xs"></i></button></div></td>
             </tr>`).join('');
        }

        function renderSuppliers() {
            const grid = document.getElementById('suppliersGrid');
            const search = document.getElementById('supplierSearch')?.value?.toLowerCase() || '';
            const filtered = suppliers.filter(s => s.name.toLowerCase().includes(search) || s.contact.toLowerCase().includes(search));
            grid.innerHTML = filtered.map(s => `<div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6 card-hover">
                <div class="flex items-start justify-between mb-4"><div class="flex items-center gap-3"><div class="w-12 h-12 bg-gradient-to-br from-indigo-400 to-purple-500 rounded-xl flex items-center justify-center text-white font-bold text-sm">${(s.name[0]||'')+(s.name[1]||'')}</div><div><h4 class="font-semibold text-gray-800">${escapeHtml(s.name)}</h4><p class="text-xs text-gray-400">${escapeHtml(s.contact)}</p></div></div><span class="badge ${s.status==='Active'?'bg-green-50 text-green-700':'bg-gray-100 text-gray-500'}">${s.status}</span></div>
                <div class="space-y-2 mb-4"><p class="text-sm text-gray-500 flex items-center gap-2"><i class="fas fa-envelope w-4 text-gray-400 text-xs"></i>${escapeHtml(s.email)}</p><p class="text-sm text-gray-500 flex items-center gap-2"><i class="fas fa-phone w-4 text-gray-400 text-xs"></i>${escapeHtml(s.phone)}</p><p class="text-sm text-gray-500 flex items-center gap-2"><i class="fas fa-box w-4 text-gray-400 text-xs"></i>${s.products || 0} products supplied</p></div>
                <div class="flex items-center justify-between pt-3 border-t border-gray-100"><div class="flex items-center gap-1"><span class="text-amber-400 text-sm">${'★'.repeat(Math.floor(s.rating))}${s.rating%1>=0.5?'½':''}</span><span class="text-sm text-gray-500 ml-1">${s.rating}</span></div><div class="flex items-center gap-1"><button onclick="openSupplierModal('${s.id}')" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-blue-50 text-gray-400 hover:text-blue-500"><i class="fas fa-pen text-xs"></i></button><button onclick="deleteSupplier('${s.id}')" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-500"><i class="fas fa-trash-alt text-xs"></i></button></div></div>
            </div>`).join('');
        }

        function renderUsers() {
            const tbody = document.getElementById('usersTableBody');
            const search = document.getElementById('userSearch')?.value?.toLowerCase() || '';
            const filtered = appUsers.filter(u => (u.firstName+' '+u.lastName).toLowerCase().includes(search) || u.email.toLowerCase().includes(search));
            tbody.innerHTML = filtered.map(u => `<tr class="table-row border-b border-gray-50">
                <td class="py-3 px-4"><div class="flex items-center gap-3"><div class="w-9 h-9 bg-gradient-to-br from-indigo-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold text-xs">${(u.firstName?.[0]||'')+(u.lastName?.[0]||'')}</div><span class="text-sm font-medium text-gray-800">${escapeHtml(u.firstName||'')} ${escapeHtml(u.lastName||'')}</span></div></td>
                <td class="py-3 px-4 text-sm text-gray-500">${escapeHtml(u.email)}</td>
                <td class="py-3 px-4"><span class="badge ${(u.role||'staff')==='admin'?'bg-indigo-50 text-indigo-700':'bg-blue-50 text-blue-700'}">${u.role||'staff'}</span></td>
                <td class="py-3 px-4"><span class="badge ${(u.status||'Active')==='Active'?'bg-green-50 text-green-700':'bg-gray-100 text-gray-500'}">${u.status||'Active'}</span></td>
                <td class="py-3 px-4 text-sm text-gray-400">${u.lastActive || 'Never'}</td>
                <td class="py-3 px-4"><div class="flex items-center gap-1"><button onclick="openUserModal('${u.id}')" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-blue-50 text-gray-400 hover:text-blue-500"><i class="fas fa-pen text-xs"></i></button><button onclick="deleteUser('${u.id}')" class="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-500"><i class="fas fa-trash-alt text-xs"></i></button></div></td>
             </tr>`).join('');
        }

        function renderSales() {
            const tbody = document.getElementById('salesTableBody');
            tbody.innerHTML = salesHistory.map(s => `<tr class="table-row border-b border-gray-50"><td class="py-3 px-4 text-sm text-gray-500">${s.date}</td><td class="py-3 px-4 text-sm font-medium text-gray-800">${escapeHtml(s.product)}</td><td class="py-3 px-4"><span class="badge ${s.type==='Sale'?'bg-green-50 text-green-700':s.type==='Return'?'bg-amber-50 text-amber-700':'bg-blue-50 text-blue-700'}">${s.type}</span></td><td class="py-3 px-4 text-sm font-semibold text-gray-700">${s.qty}</td><td class="py-3 px-4 text-sm text-gray-500">${escapeHtml(s.by)}</td></tr>`).join('');
        }

        function renderAdjustments() {
            const tbody = document.getElementById('adjustmentTableBody');
            tbody.innerHTML = adjustmentHistory.map(a => `<tr class="table-row border-b border-gray-50"><td class="py-3 px-4 text-sm text-gray-500">${a.date}</td><td class="py-3 px-4 text-sm font-medium text-gray-800">${escapeHtml(a.product)}</td><td class="py-3 px-4"><span class="badge ${a.type==='Add'?'bg-green-50 text-green-700':a.type==='Subtract'?'bg-red-50 text-red-700':'bg-blue-50 text-blue-700'}">${a.type}</span></td><td class="py-3 px-4 text-sm font-bold ${a.change.startsWith('+')?'text-green-600':a.change.startsWith('-')?'text-red-600':'text-blue-600'}">${a.change}</td><td class="py-3 px-4 text-sm text-gray-500">${escapeHtml(a.reason)}</td><td class="py-3 px-4 text-sm text-gray-500">${escapeHtml(a.by)}</td></tr>`).join('');
        }

        function refreshDashboardData() {
            const recent = [...salesHistory].slice(0,5);
            document.getElementById('recentActivityContainer').innerHTML = recent.map(s => `<div class="flex items-start gap-3"><div class="w-8 h-8 bg-green-50 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5"><i class="fas fa-cart-shopping text-green-500 text-xs"></i></div><div><p class="text-sm text-gray-700"><span class="font-medium">${s.type}</span> - ${s.qty} units of "${escapeHtml(s.product)}"</p><p class="text-xs text-gray-400 mt-0.5">${s.date}</p></div></div>`).join('');
            const lowStockItems = products.filter(p => p.status === 'Low Stock' || p.status === 'Out of Stock');
            document.getElementById('lowStockContainer').innerHTML = lowStockItems.map(p => `<div class="flex items-center justify-between p-3 bg-amber-50 rounded-lg border border-amber-100"><div class="flex items-center gap-3"><div class="w-10 h-10 bg-white rounded-lg flex items-center justify-center shadow-sm"><i class="fas fa-box text-gray-500"></i></div><div><p class="text-sm font-medium text-gray-800">${escapeHtml(p.name)}</p><p class="text-xs text-gray-400">SKU: ${escapeHtml(p.sku)}</p></div></div><div class="text-right"><p class="text-sm font-bold text-amber-500">${p.stock} left</p><p class="text-xs text-gray-400">Min: ${p.minStock}</p></div></div>`).join('');
            updateDashboardStats();
        }

        function refreshReportStats() {
            const totalSales = salesHistory.reduce((sum, s) => s.type==='Sale' ? sum + (s.qty * (products.find(p=>p.name===s.product)?.price||0)) : sum, 0);
            const itemsSold = salesHistory.reduce((sum, s) => s.type==='Sale' ? sum + s.qty : sum, 0);
            const stockReceived = adjustmentHistory.reduce((sum, a) => a.type==='Add' ? sum + parseInt(a.change.substring(1)) : sum, 0);
            document.getElementById('reportTotalSales').innerText = '$' + totalSales.toLocaleString();
            document.getElementById('reportItemsSold').innerText = itemsSold;
            document.getElementById('reportStockReceived').innerText = stockReceived;
            const avgOrder = itemsSold ? (totalSales / itemsSold) : 0;
            document.getElementById('reportAvgOrder').innerText = '$' + avgOrder.toFixed(2);
        }

        // ==================== PRODUCT MODAL ====================
        window.openProductModal = (id=null) => {
            const p = id ? products.find(x=>x.id===id) : null;
            document.getElementById('modalTitle').innerText = p ? 'Edit Product' : 'Add New Product';
            document.getElementById('modalBody').innerHTML = `
                <div class="space-y-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Product Name</label><input id="pName" type="text" value="${p?.name||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div class="grid grid-cols-2 gap-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">SKU</label><input id="pSku" type="text" value="${p?.sku||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Category</label><select id="pCat" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option ${p?.category==='Electronics'?'selected':''}>Electronics</option><option ${p?.category==='Peripherals'?'selected':''}>Peripherals</option><option ${p?.category==='Accessories'?'selected':''}>Accessories</option><option ${p?.category==='Audio'?'selected':''}>Audio</option></select></div></div>
                <div class="grid grid-cols-2 gap-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Price ($)</label><input id="pPrice" type="number" step="0.01" value="${p?.price||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Stock Quantity</label><input id="pStock" type="number" value="${p?.stock||0}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div></div>
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Min Stock Level</label><input id="pMinStock" type="number" value="${p?.minStock||0}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div class="flex gap-3 pt-2"><button onclick="saveProduct('${id||''}')" class="flex-1 btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Save Product</button><button onclick="closeModal()" class="px-4 py-2.5 border border-gray-200 rounded-lg text-sm text-gray-600 hover:bg-gray-50">Cancel</button></div></div>`;
            document.getElementById('modal').classList.remove('hidden');
        };
window.saveProduct = async (id) => {
    const name = document.getElementById('pName').value;
    const sku = document.getElementById('pSku').value;
    const cat = document.getElementById('pCat').value;
    const price = parseFloat(document.getElementById('pPrice').value);
    const stock = parseInt(document.getElementById('pStock').value);
    const minStock = parseInt(document.getElementById('pMinStock').value);

    if (!name || !sku) {
        return showToast('Please fill required fields.', 'error');
    }

    const status = stock === 0 ? 'Out of Stock' : stock <= minStock ? 'Low Stock' : 'In Stock';

    const result = await addOrUpdateProduct(
        { name, sku, category: cat, price, stock, minStock, status },
        id && id !== 'null' && id !== '' ? id : null
    );

    if (result.success) {
        closeModal();
        showToast(id ? 'Product updated!' : 'Product added!');
    }
    // If failed, error toast already shown by addOrUpdateProduct
};
        window.deleteProduct = (id) => { confirmAction('Delete product?', async ()=> { await deleteProductDoc(id); showToast('Product deleted.','warning'); }); };

        // ==================== SUPPLIER MODAL ====================
        window.openSupplierModal = (id=null) => {
            const s = id ? suppliers.find(x=>x.id===id) : null;
            document.getElementById('modalTitle').innerText = s ? 'Edit Supplier' : 'Add New Supplier';
            document.getElementById('modalBody').innerHTML = `
                <div class="space-y-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Company Name</label><input id="sName" type="text" value="${s?.name||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Contact Person</label><input id="sContact" type="text" value="${s?.contact||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div class="grid grid-cols-2 gap-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Email</label><input id="sEmail" type="email" value="${s?.email||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Phone</label><input id="sPhone" type="text" value="${s?.phone||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div></div>
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Rating</label><input id="sRating" type="number" step="0.1" min="0" max="5" value="${s?.rating||4.5}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Status</label><select id="sStatus" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option ${s?.status==='Active'?'selected':''}>Active</option><option ${s?.status==='Inactive'?'selected':''}>Inactive</option></select></div>
                <div class="flex gap-3 pt-2"><button onclick="saveSupplier('${id||''}')" class="flex-1 btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Save Supplier</button><button onclick="closeModal()" class="px-4 py-2.5 border border-gray-200 rounded-lg text-sm text-gray-600 hover:bg-gray-50">Cancel</button></div></div>`;
            document.getElementById('modal').classList.remove('hidden');
        };
        window.saveSupplier = async (id) => {
            const name = document.getElementById('sName').value, contact = document.getElementById('sContact').value, email = document.getElementById('sEmail').value, phone = document.getElementById('sPhone').value, rating = parseFloat(document.getElementById('sRating').value), status = document.getElementById('sStatus').value;
            if(!name) return showToast('Please enter company name.','error');
            if(id && id!=='null') await addOrUpdateSupplier({name,contact,email,phone,rating,status, products:0}, id);
            else await addOrUpdateSupplier({name,contact,email,phone,rating,status, products:0});
            closeModal(); showToast(id?'Supplier updated!':'Supplier added!');
        };
        window.deleteSupplier = (id) => { confirmAction('Delete supplier?', async ()=> { await deleteSupplierDoc(id); showToast('Supplier deleted.','warning'); }); };

        // ==================== USER MODAL ====================
        window.openUserModal = (id=null) => {
            const u = id ? appUsers.find(x=>x.id===id) : null;
            document.getElementById('modalTitle').innerText = u ? 'Edit User' : 'Add New User';
            document.getElementById('modalBody').innerHTML = `
                <div class="space-y-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">First Name</label><input id="uFName" type="text" value="${u?.firstName||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Last Name</label><input id="uLName" type="text" value="${u?.lastName||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                <div><label class="block text-sm font-medium text-gray-700 mb-1">Email</label><input id="uEmail" type="email" value="${u?.email||''}" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"></div>
                ${!u ? '<div><label class="block text-sm font-medium text-gray-700 mb-1">Password</label><input id="uPass" type="password" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm" placeholder="Set password (not stored in Firestore)"></div>' : ''}
                <div class="grid grid-cols-2 gap-4"><div><label class="block text-sm font-medium text-gray-700 mb-1">Role</label><select id="uRole" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option ${u?.role==='admin'?'selected':''}>admin</option><option ${u?.role==='staff'?'selected':''}>staff</option></select></div><div><label class="block text-sm font-medium text-gray-700 mb-1">Status</label><select id="uStatus" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm"><option ${u?.status==='Active'?'selected':''}>Active</option><option ${u?.status==='Inactive'?'selected':''}>Inactive</option></select></div></div>
                <div class="flex gap-3 pt-2"><button onclick="saveUser('${id||''}')" class="flex-1 btn-primary text-white py-2.5 rounded-lg text-sm font-medium"><i class="fas fa-check mr-2"></i>Save User</button><button onclick="closeModal()" class="px-4 py-2.5 border border-gray-200 rounded-lg text-sm text-gray-600 hover:bg-gray-50">Cancel</button></div></div>`;
            document.getElementById('modal').classList.remove('hidden');
        };
        window.saveUser = async (id) => {
            const firstName = document.getElementById('uFName').value, lastName = document.getElementById('uLName').value, email = document.getElementById('uEmail').value, role = document.getElementById('uRole').value, status = document.getElementById('uStatus').value;
            if(!firstName || !email) return showToast('Please fill required fields.','error');
            if(id && id!=='null') await addOrUpdateUser({firstName,lastName,email,role,status, lastActive: new Date().toLocaleString()}, id);
            else {
                // For new user, we only store in Firestore; Firebase Auth must be created separately (via registration).
                await addOrUpdateUser({firstName,lastName,email,role,status, lastActive: 'Just now'});
            }
            closeModal(); showToast(id?'User updated!':'User added!');
        };
        window.deleteUser = (id) => { confirmAction('Delete user?', async ()=> { await deleteUserDoc(id); showToast('User deleted.','warning'); }); };

        // ==================== SALES & STOCK ====================
        function populateSelects() {
            const opts = products.map(p => `<option value="${p.id}">${escapeHtml(p.name)} (${p.stock} in stock)</option>`).join('');
            document.getElementById('saleProduct').innerHTML = '<option value="">Select Product</option>' + opts;
            document.getElementById('adjustProduct').innerHTML = '<option value="">Select Product</option>' + opts;
            document.getElementById('adjustProduct').onchange = function() {
                const p = products.find(x=>x.id === this.value);
                const div = document.getElementById('currentStockDisplay');
                if(p){ div.classList.remove('hidden'); document.getElementById('currentStockVal').innerText = p.stock; }
                else div.classList.add('hidden');
            };
        }

        window.recordSale = async () => {
            const productId = document.getElementById('saleProduct').value;
            const qty = parseInt(document.getElementById('saleQty').value);
            const type = document.getElementById('saleType').value;
            if(!productId || !qty || qty<=0) return showToast('Select product and valid quantity','error');
            const product = products.find(p=>p.id===productId);
            if(!product) return showToast('Product not found','error');
            if(type==='sale' && product.stock < qty) return showToast('Insufficient stock!','error');
            let newStock = product.stock;
            if(type==='sale' || type==='return' || type==='transfer') newStock -= qty;
            await updateProductStock(productId, newStock, product.minStock);
            const dateStr = new Date().toISOString().replace('T',' ').substring(0,19);
            await addSale({ product: product.name, type: type==='sale'?'Sale':type==='return'?'Return':'Transfer', qty, by: currentUser.name, date: dateStr, notes: document.getElementById('saleNotes').value });
            showToast('Transaction recorded!');
        };

        window.adjustStock = async () => {
            const productId = document.getElementById('adjustProduct').value;
            const type = document.getElementById('adjustType').value;
            const qty = parseInt(document.getElementById('adjustQty').value);
            const reason = document.getElementById('adjustReason').value;
            if(!productId || qty<0) return showToast('Select product and valid quantity','error');
            const product = products.find(p=>p.id===productId);
            if(!product) return showToast('Product not found','error');
            let newStock = product.stock, changeStr = '';
            if(type==='add'){ newStock += qty; changeStr = `+${qty}`; }
            else if(type==='subtract'){ newStock = Math.max(0, newStock - qty); changeStr = `-${qty}`; }
            else{ newStock = qty; changeStr = `=${qty}`; }
            await updateProductStock(productId, newStock, product.minStock);
            const dateStr = new Date().toISOString().replace('T',' ').substring(0,19);
            await addAdjustment({ product: product.name, type: type==='add'?'Add':type==='subtract'?'Subtract':'Set', change: changeStr, reason, by: currentUser.name, date: dateStr });
            showToast('Stock adjusted!');
        };

        // ==================== NAVIGATION, MODAL, TOAST ====================
        window.navigateTo = (page) => {
            if(page==='users' && currentRole!=='admin') return showToast('Access denied','error');
            currentPage = page;
            document.querySelectorAll('.page-content').forEach(p=>p.classList.add('hidden'));
            document.getElementById(`page-${page}`).classList.remove('hidden');
            document.querySelectorAll('.sidebar-link').forEach(l=>l.classList.remove('active'));
            document.querySelector(`[data-page="${page}"]`).classList.add('active');
            const titles = { dashboard:'Dashboard', products:'Products', suppliers:'Suppliers', sales:'Sales / Stock Out', stock:'Stock Adjustment', reports:'Reports', users:'Manage Users' };
            document.getElementById('pageTitle').innerText = titles[page];
            document.getElementById('pageSubtitle').innerText = page==='dashboard'?'Overview of your inventory':page==='reports'?'Inventory analytics and insights':'';
            if(page==='dashboard') refreshDashboardData();
            if(page==='reports') { refreshReportStats(); initReportChart(); }
            if(window.innerWidth<1024){ toggleSidebar(); }
        };
        window.toggleSidebar = () => { document.getElementById('sidebar').classList.toggle('open'); document.getElementById('sidebarOverlay').classList.toggle('hidden'); };
        window.showNotifications = () => { alert('Notifications panel would appear here (mock).'); };
        window.exportReport = () => { showToast('Report exported as CSV (mock).','success'); };
        window.closeModal = () => { document.getElementById('modal').classList.add('hidden'); };
        window.showToast = (msg, type='success') => {
            const toast = document.getElementById('toast'), icon = document.getElementById('toastIcon'), msgEl = document.getElementById('toastMsg');
            msgEl.innerText = msg;
            icon.className = type==='success'?'fas fa-check-circle text-green-400':type==='error'?'fas fa-times-circle text-red-400':'fas fa-exclamation-circle text-amber-400';
            toast.classList.remove('hidden');
            setTimeout(()=>toast.classList.add('hidden'),3000);
        };
        function confirmAction(msg, onConfirm){
            document.getElementById('modalTitle').innerText = 'Confirm';
            document.getElementById('modalBody').innerHTML = `<div class="text-center py-4"><div class="w-16 h-16 bg-red-50 rounded-full flex items-center justify-center mx-auto mb-4"><i class="fas fa-question-circle text-red-500 text-2xl"></i></div><p class="text-gray-700 mb-6">${msg}</p><div class="flex gap-3 justify-center"><button onclick="closeModal(); onConfirm()" class="btn-danger text-white px-6 py-2.5 rounded-lg text-sm font-medium">Yes</button><button onclick="closeModal()" class="px-6 py-2.5 border border-gray-200 rounded-lg text-sm text-gray-600 hover:bg-gray-50">Cancel</button></div></div>`;
            window.onConfirm = onConfirm;
            document.getElementById('modal').classList.remove('hidden');
        }

        // ==================== CHARTS ====================
        function initCharts() {
            const ctx1 = document.getElementById('salesChart').getContext('2d');
            if(salesChart) salesChart.destroy();
            salesChart = new Chart(ctx1, { type:'line', data:{ labels:['Mon','Tue','Wed','Thu','Fri','Sat','Sun'], datasets:[{ label:'Sales', data:[65,78,52,91,84,56,73], borderColor:'#6366f1', backgroundColor:'rgba(99,102,241,0.1)', fill:true, tension:0.4 },{ label:'Stock In', data:[40,55,80,45,70,60,50], borderColor:'#10b981', backgroundColor:'rgba(16,185,129,0.1)', fill:true, tension:0.4 }] }, options:{ responsive:true, plugins:{ legend:{ position:'top' } }, scales:{ y:{ beginAtZero:true } } } });
            const ctx2 = document.getElementById('categoryChart').getContext('2d');
            if(categoryChart) categoryChart.destroy();
            categoryChart = new Chart(ctx2, { type:'doughnut', data:{ labels:['Electronics','Peripherals','Accessories','Audio'], datasets:[{ data:[25,35,28,12], backgroundColor:['#6366f1','#8b5cf6','#a78bfa','#c4b5fd'], borderWidth:0 }] }, options:{ responsive:true, cutout:'65%', plugins:{ legend:{ position:'bottom' } } } });
        }
        function initReportChart() {
            const ctx = document.getElementById('revenueChart').getContext('2d');
            if(revenueChart) revenueChart.destroy();
            revenueChart = new Chart(ctx, { type:'bar', data:{ labels:['Jan','Feb','Mar','Apr','May','Jun','Jul'], datasets:[{ label:'Revenue ($)', data:[12400,15800,11200,18600,14200,19800,16400], backgroundColor:'rgba(99,102,241,0.8)', borderRadius:8, barThickness:32 }] }, options:{ responsive:true, plugins:{ legend:{ display:false } }, scales:{ y:{ beginAtZero:true, ticks:{ callback:v=>'$'+v.toLocaleString() } } } } });
            const topProducts = [...products].sort((a,b)=>b.stock - a.stock).slice(0,5);
            document.getElementById('topProductsContainer').innerHTML = topProducts.map((p,i)=>`<div class="flex items-center justify-between"><div class="flex items-center gap-3 flex-1"><span class="text-sm font-medium text-gray-700 w-32 truncate">${escapeHtml(p.name)}</span><div class="flex-1 bg-gray-100 rounded-full h-2.5"><div class="bg-indigo-500 h-2.5 rounded-full progress-bar" style="width:${Math.min(100, (p.stock/200)*100)}%"></div></div></div><span class="text-sm font-semibold text-gray-600 ml-3">${p.stock}</span></div>`).join('');
        }

        function escapeHtml(str) { if(!str) return ''; return str.replace(/[&<>]/g, function(m){ if(m==='&') return '&amp;'; if(m==='<') return '&lt;'; if(m==='>') return '&gt;'; return m; }); }
        window.filterProducts = () => renderProducts();
        document.getElementById('chartPeriod')?.addEventListener('change', initCharts);
        document.getElementById('reportPeriod')?.addEventListener('change', initReportChart);
    </script>
</body>
</html>