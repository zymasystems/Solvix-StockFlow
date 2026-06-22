<?php
session_start();
// Landing page - no authentication required
$pageTitle = 'Solvix StockFlow - Intelligent Inventory Management';
$showTicker = true; // Show ticker on landing page
?>
<!-- Ticker bar (only on landing) -->
<div class="ticker-bar relative z-10">
    <div class="marquee-track">
        <span><i class="fa-solid fa-circle-check accent"></i> Trusted by 12,000+ businesses worldwide</span>
        <span><i class="fa-solid fa-bolt accent"></i> Real-time sync across all locations</span>
        <span><i class="fa-solid fa-shield-halved accent"></i> SOC 2 Type II Certified</span>
        <span><i class="fa-solid fa-chart-line accent"></i> Average 34% reduction in carrying costs</span>
        <span><i class="fa-solid fa-clock accent"></i> 99.99% uptime SLA</span>
        <span><i class="fa-solid fa-plug accent"></i> 200+ integrations available</span>
        <span><i class="fa-solid fa-circle-check accent"></i> Trusted by 12,000+ businesses worldwide</span>
        <span><i class="fa-solid fa-bolt accent"></i> Real-time sync across all locations</span>
        <span><i class="fa-solid fa-shield-halved accent"></i> SOC 2 Type II Certified</span>
        <span><i class="fa-solid fa-chart-line accent"></i> Average 34% reduction in carrying costs</span>
        <span><i class="fa-solid fa-clock accent"></i> 99.99% uptime SLA</span>
        <span><i class="fa-solid fa-plug accent"></i> 200+ integrations available</span>
    </div>
</div>

<?php
include './includes/landing-header.php';
?>

<!-- Hero Section -->
<header class="relative z-10 pt-40 pb-20 md:pt-48 md:pb-32">
    <div class="max-w-7xl mx-auto px-6 text-center">
        <div class="reveal">
            <div class="inline-flex items-center gap-2 px-4 py-2 rounded-full text-sm font-medium mb-8" style="background: var(--accent-glow); color: var(--accent); border: 1px solid rgba(22,179,100,0.2);">
                <span class="w-2 h-2 rounded-full pulse-ring" style="background: var(--accent);"></span>
                Now with AI-powered demand forecasting
            </div>
        </div>
        <h1 class="font-display font-bold text-4xl sm:text-5xl md:text-6xl lg:text-7xl leading-tight max-w-4xl mx-auto reveal reveal-delay-1">
            Inventory that<br>
            <span style="background: linear-gradient(135deg, #3b82f6, #60a5fa, #93c5fd); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text;">thinks ahead</span>
        </h1>
        <p class="mt-6 text-lg md:text-xl max-w-2xl mx-auto leading-relaxed reveal reveal-delay-2" style="color: var(--fg-muted);">
            Stop guessing, start knowing. Solvix StockFlow gives you real-time visibility, predictive insights, and automated workflows across every warehouse and channel.
        </p>
        <div class="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4 reveal reveal-delay-3">
            <a href="login.php" class="btn-primary text-base w-full sm:w-auto inline-block text-center">
                <i class="fa-solid fa-rocket mr-2"></i> Start 14-Day Free Trial
            </a>
            <button class="btn-secondary text-base w-full sm:w-auto" onclick="alert('Demo video would play here')">
                <i class="fa-solid fa-play mr-2"></i> Watch Demo
            </button>
        </div>
        <p class="mt-5 text-sm reveal reveal-delay-4" style="color: var(--fg-muted);">
            No credit card required &middot; Setup in 5 minutes &middot; Cancel anytime
        </p>

        <!-- Dashboard mockup -->
        <div class="mt-16 md:mt-20 max-w-5xl mx-auto reveal reveal-delay-4">
            <div class="dashboard-mock">
                <div class="dash-topbar">
                    <div class="dot dot-r"></div>
                    <div class="dot dot-y"></div>
                    <div class="dot dot-g"></div>
                    <div class="flex-1 mx-4">
                        <div class="max-w-xs mx-auto h-6 rounded-md flex items-center justify-center text-xs" style="background: var(--bg); color: var(--fg-muted);">
                            <i class="fa-solid fa-lock mr-1.5 text-xs" style="color: var(--accent);"></i>
                            app.solvixstockflow.io/dashboard
                        </div>
                    </div>
                </div>
                <div class="p-4 md:p-6">
                    <div class="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
                        <div class="rounded-xl p-3 md:p-4" style="background: var(--bg-card); border: 1px solid var(--border);">
                            <div class="text-xs font-medium mb-1" style="color: var(--fg-muted);">Total SKUs</div>
                            <div class="font-display font-bold text-lg md:text-xl">24,831</div>
                            <div class="text-xs mt-1" style="color: var(--accent);"><i class="fa-solid fa-arrow-up mr-1"></i>12.3%</div>
                        </div>
                        <div class="rounded-xl p-3 md:p-4" style="background: var(--bg-card); border: 1px solid var(--border);">
                            <div class="text-xs font-medium mb-1" style="color: var(--fg-muted);">In Stock</div>
                            <div class="font-display font-bold text-lg md:text-xl">21,452</div>
                            <div class="text-xs mt-1" style="color: var(--accent);"><i class="fa-solid fa-arrow-up mr-1"></i>8.7%</div>
                        </div>
                        <div class="rounded-xl p-3 md:p-4" style="background: var(--bg-card); border: 1px solid var(--border);">
                            <div class="text-xs font-medium mb-1" style="color: var(--fg-muted);">Low Stock</div>
                            <div class="font-display font-bold text-lg md:text-xl" style="color: #fbbf24;">2,156</div>
                            <div class="text-xs mt-1" style="color: #ef4444;"><i class="fa-solid fa-arrow-up mr-1"></i>3.1%</div>
                        </div>
                        <div class="rounded-xl p-3 md:p-4" style="background: var(--bg-card); border: 1px solid var(--border);">
                            <div class="text-xs font-medium mb-1" style="color: var(--fg-muted);">Out of Stock</div>
                            <div class="font-display font-bold text-lg md:text-xl" style="color: #ef4444;">1,223</div>
                            <div class="text-xs mt-1" style="color: var(--accent);"><i class="fa-solid fa-arrow-down mr-1"></i>5.2%</div>
                        </div>
                    </div>
                    <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
                        <div class="md:col-span-2 rounded-xl p-4" style="background: var(--bg-card); border: 1px solid var(--border);">
                            <div class="flex items-center justify-between mb-4">
                                <div class="text-sm font-semibold font-display">Stock Movement</div>
                                <div class="flex gap-1">
                                    <span class="text-xs px-2.5 py-1 rounded-md font-medium" style="background: var(--accent-glow); color: var(--accent);">7D</span>
                                    <span class="text-xs px-2.5 py-1 rounded-md font-medium" style="color: var(--fg-muted);">30D</span>
                                    <span class="text-xs px-2.5 py-1 rounded-md font-medium" style="color: var(--fg-muted);">90D</span>
                                </div>
                            </div>
                            <div class="flex items-end gap-2 h-32" id="chartBars"></div>
                            <div class="flex justify-between mt-2">
                                <span class="text-xs" style="color: var(--fg-muted);">Mon</span>
                                <span class="text-xs" style="color: var(--fg-muted);">Tue</span>
                                <span class="text-xs" style="color: var(--fg-muted);">Wed</span>
                                <span class="text-xs" style="color: var(--fg-muted);">Thu</span>
                                <span class="text-xs" style="color: var(--fg-muted);">Fri</span>
                                <span class="text-xs" style="color: var(--fg-muted);">Sat</span>
                                <span class="text-xs" style="color: var(--fg-muted);">Sun</span>
                            </div>
                        </div>
                        <div class="rounded-xl p-4" style="background: var(--bg-card); border: 1px solid var(--border);">
                            <div class="text-sm font-semibold font-display mb-3">Recent Activity</div>
                            <div class="space-y-3">
                                <div class="flex items-start gap-2.5">
                                    <div class="w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0 mt-0.5" style="background: rgba(22,179,100,0.15);"><i class="fa-solid fa-arrow-down text-xs" style="color: var(--accent);"></i></div>
                                    <div><div class="text-xs font-medium">Inbound: 450 units</div><div class="text-xs" style="color: var(--fg-muted);">SKU-8821 &middot; 2m ago</div></div>
                                </div>
                                <div class="flex items-start gap-2.5">
                                    <div class="w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0 mt-0.5" style="background: rgba(251,191,36,0.15);"><i class="fa-solid fa-triangle-exclamation text-xs" style="color: #fbbf24;"></i></div>
                                    <div><div class="text-xs font-medium">Low stock alert</div><div class="text-xs" style="color: var(--fg-muted);">SKU-3390 &middot; 15m ago</div></div>
                                </div>
                                <div class="flex items-start gap-2.5">
                                    <div class="w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0 mt-0.5" style="background: rgba(99,102,241,0.15);"><i class="fa-solid fa-arrow-right-arrow-left text-xs" style="color: #818cf8;"></i></div>
                                    <div><div class="text-xs font-medium">Transfer: 120 units</div><div class="text-xs" style="color: var(--fg-muted);">WH-02 &rarr; WH-05 &middot; 1h</div></div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</header>

<!-- Logos Section -->
<section class="relative z-10 py-16 border-y" style="border-color: var(--border);">
    <div class="max-w-7xl mx-auto px-6">
        <p class="text-center text-sm font-medium mb-10" style="color: var(--fg-muted);">Powering inventory for forward-thinking teams</p>
        <div class="flex flex-wrap items-center justify-center gap-x-12 gap-y-6 opacity-40">
            <span class="font-display font-bold text-xl tracking-tight">Shopify</span>
            <span class="font-display font-bold text-xl tracking-tight">Stripe</span>
            <span class="font-display font-bold text-xl tracking-tight">Notion</span>
            <span class="font-display font-bold text-xl tracking-tight">Vercel</span>
            <span class="font-display font-bold text-xl tracking-tight">Linear</span>
            <span class="font-display font-bold text-xl tracking-tight">Figma</span>
        </div>
    </div>
</section>

<!-- Features Section -->
<section id="features" class="relative z-10 py-24 md:py-32">
    <div class="max-w-7xl mx-auto px-6">
        <div class="text-center max-w-2xl mx-auto mb-16">
            <div class="reveal"><span class="text-sm font-semibold tracking-wider uppercase" style="color: var(--accent);">Features</span></div>
            <h2 class="font-display font-bold text-3xl md:text-5xl mt-4 reveal reveal-delay-1">Everything you need to control stock</h2>
            <p class="mt-5 text-lg reveal reveal-delay-2" style="color: var(--fg-muted);">From receiving to shipping, StockFlow covers the entire inventory lifecycle with precision and ease.</p>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            <div class="feature-card reveal"><div class="feature-icon"><i class="fa-solid fa-satellite-dish"></i></div><h3 class="font-display font-semibold text-lg mb-2">Real-Time Tracking</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">See stock levels update instantly across all warehouses, stores, and fulfillment centers.</p></div>
            <div class="feature-card reveal reveal-delay-1"><div class="feature-icon"><i class="fa-solid fa-brain"></i></div><h3 class="font-display font-semibold text-lg mb-2">AI Demand Forecasting</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Machine learning models analyze seasonality and trends to predict demand with 94% accuracy.</p></div>
            <div class="feature-card reveal reveal-delay-2"><div class="feature-icon"><i class="fa-solid fa-bell"></i></div><h3 class="font-display font-semibold text-lg mb-2">Smart Alerts</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Configurable thresholds trigger instant notifications via email, Slack, or SMS.</p></div>
            <div class="feature-card reveal"><div class="feature-icon"><i class="fa-solid fa-barcode"></i></div><h3 class="font-display font-semibold text-lg mb-2">Barcode & RFID</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Scan items in and out with any barcode scanner or RFID reader.</p></div>
            <div class="feature-card reveal reveal-delay-1"><div class="feature-icon"><i class="fa-solid fa-arrows-spin"></i></div><h3 class="font-display font-semibold text-lg mb-2">Multi-Warehouse Sync</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Manage unlimited locations with automatic stock transfers and routing rules.</p></div>
            <div class="feature-card reveal reveal-delay-2"><div class="feature-icon"><i class="fa-solid fa-plug"></i></div><h3 class="font-display font-semibold text-lg mb-2">200+ Integrations</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Connect to Shopify, WooCommerce, Amazon, QuickBooks, and more.</p></div>
        </div>
    </div>
</section>

<!-- Stats Section -->
<section class="relative z-10 py-20 border-y" style="border-color: var(--border); background: var(--bg-elevated);">
    <div class="max-w-6xl mx-auto px-6">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div class="stat-item reveal"><div class="stat-number" data-target="12000" data-suffix="+">0</div><div class="mt-2 text-sm font-medium" style="color: var(--fg-muted);">Active Businesses</div></div>
            <div class="stat-item reveal reveal-delay-1"><div class="stat-number" data-target="34" data-suffix="%">0</div><div class="mt-2 text-sm font-medium" style="color: var(--fg-muted);">Cost Reduction</div></div>
            <div class="stat-item reveal reveal-delay-2"><div class="stat-number" data-target="99.99" data-suffix="%" data-decimals="2">0</div><div class="mt-2 text-sm font-medium" style="color: var(--fg-muted);">Uptime SLA</div></div>
            <div class="stat-item reveal reveal-delay-3"><div class="stat-number" data-target="200" data-suffix="+">0</div><div class="mt-2 text-sm font-medium" style="color: var(--fg-muted);">Integrations</div></div>
        </div>
    </div>
</section>

<!-- How It Works -->
<section id="how-it-works" class="relative z-10 py-24 md:py-32">
    <div class="max-w-7xl mx-auto px-6">
        <div class="text-center max-w-2xl mx-auto mb-16">
            <div class="reveal"><span class="text-sm font-semibold tracking-wider uppercase" style="color: var(--accent);">How It Works</span></div>
            <h2 class="font-display font-bold text-3xl md:text-5xl mt-4 reveal reveal-delay-1">Up and running in minutes</h2>
            <p class="mt-5 text-lg reveal reveal-delay-2" style="color: var(--fg-muted);">No complex setup. No consultants. Just three steps to total inventory clarity.</p>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-5">
            <div class="step-card reveal"><div class="step-number">1</div><h3 class="font-display font-semibold text-xl mb-3">Import Your Data</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Upload a CSV, connect your existing system via API, or start fresh.</p><div class="mt-5 flex items-center gap-2 text-sm font-medium" style="color: var(--accent);"><i class="fa-solid fa-file-csv"></i> CSV, XLSX, API, Manual</div></div>
            <div class="step-card reveal reveal-delay-1"><div class="step-number">2</div><h3 class="font-display font-semibold text-xl mb-3">Configure Workflows</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Set reorder points, alert thresholds, approval chains, and routing rules.</p><div class="mt-5 flex items-center gap-2 text-sm font-medium" style="color: var(--accent);"><i class="fa-solid fa-wand-magic-sparkles"></i> Templates + Custom</div></div>
            <div class="step-card reveal reveal-delay-2"><div class="step-number">3</div><h3 class="font-display font-semibold text-xl mb-3">Track & Optimize</h3><p class="text-sm leading-relaxed" style="color: var(--fg-muted);">Monitor real-time dashboards, let AI suggest optimizations, and watch stockouts drop.</p><div class="mt-5 flex items-center gap-2 text-sm font-medium" style="color: var(--accent);"><i class="fa-solid fa-chart-line"></i> Real-Time + AI Insights</div></div>
        </div>
    </div>
</section>

<!-- Testimonials -->
<section id="testimonials" class="relative z-10 py-24 md:py-32 border-t" style="border-color: var(--border); background: var(--bg-elevated);">
    <div class="max-w-7xl mx-auto px-6">
        <div class="text-center max-w-2xl mx-auto mb-16">
            <div class="reveal"><span class="text-sm font-semibold tracking-wider uppercase" style="color: var(--accent);">Testimonials</span></div>
            <h2 class="font-display font-bold text-3xl md:text-5xl mt-4 reveal reveal-delay-1">Loved by operations teams</h2>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-5">
            <div class="testimonial-card reveal"><div class="flex gap-1 mb-4"><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i></div><p class="text-sm leading-relaxed mb-6" style="color: var(--fg-muted);">"We cut our stockout rate from 8% to under 1% in the first quarter. The demand forecasting alone paid for the entire year's subscription."</p><div class="flex items-center gap-3"><img src="https://picsum.photos/seed/person1/80/80.jpg" alt="Sarah Chen" class="w-10 h-10 rounded-full object-cover"><div><div class="text-sm font-semibold">Sarah Chen</div><div class="text-xs" style="color: var(--fg-muted);">Head of Ops, TechNova</div></div></div></div>
            <div class="testimonial-card reveal reveal-delay-1"><div class="flex gap-1 mb-4"><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i></div><p class="text-sm leading-relaxed mb-6" style="color: var(--fg-muted);">"Managing 5 warehouses used to be a nightmare of spreadsheets. StockFlow gave us one source of truth."</p><div class="flex items-center gap-3"><img src="https://picsum.photos/seed/person2/80/80.jpg" alt="Marcus Rivera" class="w-10 h-10 rounded-full object-cover"><div><div class="text-sm font-semibold">Marcus Rivera</div><div class="text-xs" style="color: var(--fg-muted);">Supply Chain Director, BuildRight</div></div></div></div>
            <div class="testimonial-card reveal reveal-delay-2"><div class="flex gap-1 mb-4"><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star text-sm" style="color: #fbbf24;"></i><i class="fa-solid fa-star-half-stroke text-sm" style="color: #fbbf24;"></i></div><p class="text-sm leading-relaxed mb-6" style="color: var(--fg-muted);">"The Shopify integration is flawless. Orders sync in real-time and customer complaints about stock issues dropped to near zero."</p><div class="flex items-center gap-3"><img src="https://picsum.photos/seed/person3/80/80.jpg" alt="Aisha Patel" class="w-10 h-10 rounded-full object-cover"><div><div class="text-sm font-semibold">Aisha Patel</div><div class="text-xs" style="color: var(--fg-muted);">E-commerce Lead, LuxeHome</div></div></div></div>
        </div>
    </div>
</section>

<!-- Pricing Section -->
<section id="pricing" class="relative z-10 py-24 md:py-32">
    <div class="max-w-7xl mx-auto px-6">
        <div class="text-center max-w-2xl mx-auto mb-16">
            <div class="reveal"><span class="text-sm font-semibold tracking-wider uppercase" style="color: var(--accent);">Pricing</span></div>
            <h2 class="font-display font-bold text-3xl md:text-5xl mt-4 reveal reveal-delay-1">Simple, transparent pricing</h2>
            <p class="mt-5 text-lg reveal reveal-delay-2" style="color: var(--fg-muted);">Start free, scale as you grow. No hidden fees, no surprise charges.</p>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 max-w-5xl mx-auto">
            <div class="pricing-card reveal"><div class="text-sm font-semibold font-display" style="color: var(--fg-muted);">Starter</div><div class="mt-3 flex items-end gap-1"><span class="price-value">R0</span><span class="text-sm mb-2" style="color: var(--fg-muted);">/month</span></div><p class="mt-3 text-sm" style="color: var(--fg-muted);">Perfect for small businesses.</p><ul class="mt-6 space-y-3"><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> Up to 500 SKUs</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> 1 warehouse location</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> Basic alerts</li></ul><a href="login.php" class="btn-secondary w-full mt-8 text-sm inline-block text-center">Get Started Free</a></div>
            <div class="pricing-card featured reveal reveal-delay-1"><div class="text-sm font-semibold font-display" style="color: var(--accent);">Professional</div><div class="mt-3 flex items-end gap-1"><span class="price-value">R599</span><span class="text-sm mb-2" style="color: var(--fg-muted);">/month</span></div><p class="mt-3 text-sm" style="color: var(--fg-muted);">For growing teams.</p><ul class="mt-6 space-y-3"><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> Up to 25,000 SKUs</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> 10 warehouse locations</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> AI demand forecasting</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> Barcode & RFID scanning</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> 50+ integrations</li></ul><a href="login.php" class="btn-primary w-full mt-8 text-sm inline-block text-center">Start 14-Day Trial</a></div>
            <div class="pricing-card reveal reveal-delay-2"><div class="text-sm font-semibold font-display" style="color: var(--fg-muted);">Enterprise</div><div class="mt-3 flex items-end gap-1"><span class="price-value">Custom</span></div><p class="mt-3 text-sm" style="color: var(--fg-muted);">For large operations.</p><ul class="mt-6 space-y-3"><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> Unlimited SKUs</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> Unlimited locations</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> Custom AI models</li><li class="flex items-center text-sm"><i class="fa-solid fa-check check-icon"></i> 200+ integrations</li></ul><button class="btn-secondary w-full mt-8 text-sm" onclick="alert('Contact sales: sales@stockflow.com')">Contact Sales</button></div>
        </div>
    </div>
</section>

<!-- CTA Section -->
<section class="relative z-10 py-24 md:py-32">
    <div class="max-w-4xl mx-auto px-6">
        <div class="reveal rounded-3xl p-12 md:p-16 text-center relative overflow-hidden" style="background: linear-gradient(135deg, rgba(59, 130, 246, 0.1), rgba(59, 130, 246, 0.02)); border: 1px solid rgba(59, 130, 246, 0.2);">
            <div class="absolute inset-0" style="background: radial-gradient(circle at 30% 50%, rgba(59, 130, 246, 0.08), transparent 60%);"></div>
            <div class="relative z-10">
                <h2 class="font-display font-bold text-3xl md:text-5xl">Ready to take control of your inventory?</h2>
                <p class="mt-5 text-lg max-w-xl mx-auto" style="color: var(--fg-muted);">Join 12,000+ businesses that trust StockFlow to keep their stock accurate, optimized, and always moving.</p>
                <div class="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
                    <a href="login.php" class="btn-primary text-base w-full sm:w-auto inline-block text-center" style="padding: 14px 32px;">Start Your Free Trial</a>
                    <button class="btn-secondary text-base w-full sm:w-auto" style="padding: 14px 32px;" onclick="alert('Schedule a demo: demo@stockflow.com')">Schedule a Demo</button>
                </div>
            </div>
        </div>
    </div>
</section>

<?php include './includes/landing-footer.php'; ?>