// === Chart bars in dashboard mockup ===
        (function() {
            const chartContainer = document.getElementById('chartBars');
            const inboundData = [65, 78, 55, 90, 82, 45, 70];
            const outboundData = [50, 60, 42, 75, 68, 35, 55];
            const maxVal = Math.max(...inboundData, ...outboundData);

            inboundData.forEach((val, i) => {
                const group = document.createElement('div');
                group.className = 'flex-1 flex items-end gap-0.5';
                group.style.height = '100%';

                const barIn = document.createElement('div');
                barIn.className = 'flex-1 rounded-t-sm bar-animate';
                barIn.style.background = 'linear-gradient(180deg, #16b364, #0a9150)';
                barIn.style.height = (val / maxVal * 100) + '%';
                barIn.style.animationDelay = (i * 0.1) + 's';
                barIn.title = 'Inbound: ' + val;

                const barOut = document.createElement('div');
                barOut.className = 'flex-1 rounded-t-sm bar-animate';
                barOut.style.background = 'linear-gradient(180deg, rgba(139,148,158,0.4), rgba(139,148,158,0.15))';
                barOut.style.height = (outboundData[i] / maxVal * 100) + '%';
                barOut.style.animationDelay = (i * 0.1 + 0.05) + 's';
                barOut.title = 'Outbound: ' + outboundData[i];

                group.appendChild(barIn);
                group.appendChild(barOut);
                chartContainer.appendChild(group);
            });
        })();

        // === Scroll reveal ===
        const revealElements = document.querySelectorAll('.reveal');
        const revealObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('active');
                }
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });
        revealElements.forEach(el => revealObserver.observe(el));

        // === Navbar scroll effect ===
        const navbar = document.getElementById('navbar');
        const tickerHeight = 38;
        let lastScroll = 0;

        window.addEventListener('scroll', () => {
            const scrollY = window.scrollY;

            if (scrollY > 20) {
                navbar.classList.add('nav-scrolled');
                navbar.style.top = '0';
            } else {
                navbar.classList.remove('nav-scrolled');
                navbar.style.top = tickerHeight + 'px';
            }
            lastScroll = scrollY;
        }, { passive: true });

        // === Stat counter animation ===
        const statNumbers = document.querySelectorAll('.stat-number[data-target]');
        const statObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !entry.target.dataset.animated) {
                    entry.target.dataset.animated = 'true';
                    animateCounter(entry.target);
                }
            });
        }, { threshold: 0.5 });
        statNumbers.forEach(el => statObserver.observe(el));

        function animateCounter(el) {
            const target = parseFloat(el.dataset.target);
            const suffix = el.dataset.suffix || '';
            const decimals = parseInt(el.dataset.decimals) || 0;
            const duration = 2000;
            const startTime = performance.now();

            function update(now) {
                const elapsed = now - startTime;
                const progress = Math.min(elapsed / duration, 1);
                // Ease out cubic
                const eased = 1 - Math.pow(1 - progress, 3);
                const current = eased * target;

                if (decimals > 0) {
                    el.textContent = current.toFixed(decimals) + suffix;
                } else {
                    el.textContent = Math.floor(current).toLocaleString() + suffix;
                }

                if (progress < 1) {
                    requestAnimationFrame(update);
                }
            }
            requestAnimationFrame(update);
        }

        // === Mobile menu ===
        const menuBtn = document.getElementById('menuBtn');
        const closeMenuBtn = document.getElementById('closeMenu');
        const mobileMenu = document.getElementById('mobileMenu');

        menuBtn.addEventListener('click', () => mobileMenu.classList.add('open'));
        closeMenuBtn.addEventListener('click', closeMobileMenu);

        function closeMobileMenu() {
            mobileMenu.classList.remove('open');
        }

        // === Smooth scroll for anchor links ===
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function(e) {
                const target = document.querySelector(this.getAttribute('href'));
                if (target) {
                    e.preventDefault();
                    const offset = 80;
                    const top = target.getBoundingClientRect().top + window.scrollY - offset;
                    window.scrollTo({ top, behavior: 'smooth' });
                }
            });
        });

        // === Hover effects on footer links ===
        document.querySelectorAll('footer a').forEach(link => {
            link.addEventListener('mouseenter', function() {
                this.style.color = 'var(--accent)';
            });
            link.addEventListener('mouseleave', function() {
                this.style.color = 'var(--fg-muted)';
            });
        });

        // === Social icon hover ===
        document.querySelectorAll('footer .rounded-lg[aria-label]').forEach(icon => {
            icon.addEventListener('mouseenter', function() {
                this.style.background = 'var(--accent)';
                this.style.color = 'white';
                this.style.borderColor = 'var(--accent)';
            });
            icon.addEventListener('mouseleave', function() {
                this.style.background = 'var(--bg-card)';
                this.style.color = 'var(--fg-muted)';
                this.style.borderColor = 'var(--border)';
            });
        });

        // === Parallax orbs on mouse move ===
        document.addEventListener('mousemove', (e) => {
            const x = (e.clientX / window.innerWidth - 0.5) * 2;
            const y = (e.clientY / window.innerHeight - 0.5) * 2;

            document.querySelectorAll('.orb').forEach((orb, i) => {
                const factor = (i + 1) * 15;
                orb.style.transform = `translate(${x * factor}px, ${y * factor}px)`;
            });
        }, { passive: true });