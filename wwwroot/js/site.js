/**
 * GIVE-AID NGO WELFARE & DONATION MANAGEMENT SYSTEM
 * MASTER JAVASCRIPT & 3D INTERACTIVE EXPERIENCE ENGINE (2026 EDITION)
 */

document.addEventListener("DOMContentLoaded", function () {
    initThemeSwitcher();
    enhancePaymentBadges();
    initScrollProgressBar();
    initBackToTop();
    initCounterAnimation();
    init3DCardTilt();
    initImpactCalculator();
    initQuickDonateWidget();
    initLuhnAndCardValidator();
    initGalleryFilter();
    initFaqSearch();
    initActiveNavHighlight();
    initClipboardCopy();
    initMobileMenuAutoClose();
    initFormSubmitSpinners();
});

/* ==========================================================================
   Scroll progress, back-to-top, counters
   ========================================================================== */
function initScrollProgressBar() {
    const bar = document.getElementById("scrollProgressBar");
    if (!bar) return;

    const update = () => {
        const doc = document.documentElement;
        const scrollTop = doc.scrollTop || document.body.scrollTop;
        const height = doc.scrollHeight - doc.clientHeight;
        const pct = height > 0 ? (scrollTop / height) * 100 : 0;
        bar.style.width = pct + "%";
    };

    window.addEventListener("scroll", update, { passive: true });
    update();
}

function initBackToTop() {
    const btn = document.getElementById("backToTopBtn");
    if (!btn) return;

    const toggle = () => {
        btn.classList.toggle("visible", window.scrollY > 420);
    };

    window.addEventListener("scroll", toggle, { passive: true });
    btn.addEventListener("click", function (e) {
        e.preventDefault();
        window.scrollTo({ top: 0, behavior: "smooth" });
    });
    toggle();
}

function initCounterAnimation() {
    const counters = document.querySelectorAll(".counter-value[data-target]");
    if (!counters.length) return;

    const animate = (el) => {
        const target = parseFloat(el.getAttribute("data-target")) || 0;
        const prefix = el.getAttribute("data-prefix") || "";
        const suffix = el.getAttribute("data-suffix") || "";
        const duration = 1400;
        const start = performance.now();

        const tick = (now) => {
            const t = Math.min(1, (now - start) / duration);
            const eased = 1 - Math.pow(1 - t, 3);
            const value = Math.round(target * eased);
            el.textContent = prefix + value.toLocaleString() + suffix;
            if (t < 1) requestAnimationFrame(tick);
        };

        requestAnimationFrame(tick);
    };

    if ("IntersectionObserver" in window) {
        const io = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) return;
                animate(entry.target);
                io.unobserve(entry.target);
            });
        }, { threshold: 0.35 });
        counters.forEach((c) => io.observe(c));
    } else {
        counters.forEach(animate);
    }
}

/* ==========================================================================
   3. Rotating Earth / Globe visualizer removed — replaced by cinematic
      frame-sequence scroll animation (wwwroot/js/frame-sequence.js)
   ========================================================================== */

/* ==========================================================================
   4. Premium 3D Card Tilt Engine with Real-time Specular Glare & Parallax
   ========================================================================== */
function init3DCardTilt() {
    const tiltElements = document.querySelectorAll(".card-hover, .step-card, .stat-box-modern, .quick-donate-card, .program-card, .ngo-card, .cause-card, .partner-card, .about-card, .tilt-card-3d");
    if (!tiltElements.length) return;

    tiltElements.forEach(el => {
        el.style.transformStyle = "preserve-3d";
        el.style.willChange = "transform";
        el.style.transition = "transform 0.5s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.5s ease";

        // Create glare layer if not existing
        let glare = el.querySelector(".tilt-glare-layer");
        if (!glare) {
            glare = document.createElement("div");
            glare.className = "tilt-glare-layer";
            el.appendChild(glare);
        }

        el.addEventListener("mouseenter", function () {
            this.style.transition = "transform 0.1s ease-out, box-shadow 0.2s ease";
        });

        el.addEventListener("mousemove", function (e) {
            const rect = this.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            
            const centerX = rect.width / 2;
            const centerY = rect.height / 2;

            const rotateX = ((y - centerY) / centerY) * -7;
            const rotateY = ((x - centerX) / centerX) * 7;

            this.style.transform = `perspective(1200px) rotateX(${rotateX.toFixed(2)}deg) rotateY(${rotateY.toFixed(2)}deg) translateY(-6px) scale3d(1.02, 1.02, 1.02)`;

            if (glare) {
                const percentX = (x / rect.width) * 100;
                const percentY = (y / rect.height) * 100;
                glare.style.background = `radial-gradient(circle at ${percentX}% ${percentY}%, rgba(255, 255, 255, 0.16), transparent 65%)`;
            }
        });

        el.addEventListener("mouseleave", function () {
            this.style.transition = "transform 0.6s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.6s ease";
            this.style.transform = "perspective(1200px) rotateX(0deg) rotateY(0deg) translateY(0px) scale3d(1, 1, 1)";
        });
    });
}

/* ==========================================================================
   5. Interactive Impact Calculator
   ========================================================================== */
function initImpactCalculator() {
    const slider = document.getElementById("impactAmountSlider");
    const amountDisplay = document.getElementById("calcAmountDisplay");
    const mealsCount = document.getElementById("calcMealsCount");
    const waterDays = document.getElementById("calcWaterDays");
    const kitsCount = document.getElementById("calcKitsCount");
    const ctaDonate = document.getElementById("calcDonateBtn");

    if (!slider || !amountDisplay) return;

    function calculateImpact(amount) {
        amountDisplay.innerText = `PKR ${amount.toLocaleString("en-US")}`;
        
        // 1 Meal ≈ PKR 250
        if (mealsCount) mealsCount.innerText = `${Math.floor(amount / 250)} Hot Meals`;
        // 1 Clean Water Day ≈ PKR 100
        if (waterDays) waterDays.innerText = `${Math.floor(amount / 100)} Days Clean Water`;
        // 1 Child School Kit ≈ PKR 1,500
        if (kitsCount) kitsCount.innerText = `${Math.max(1, Math.floor(amount / 1500))} School Kits`;

        if (ctaDonate) {
            ctaDonate.href = `/Donation/Donate?customAmount=${amount}`;
        }
    }

    slider.addEventListener("input", function () {
        calculateImpact(parseInt(this.value, 10));
    });

    calculateImpact(parseInt(slider.value, 10));
}

/* ==========================================================================
   6. Quick Donation Widget & Amount Chips
   ========================================================================== */
function initQuickDonateWidget() {
    const forms = document.querySelectorAll("#quickDonateHeroForm, #quickDonateMobileForm");

    forms.forEach(form => {
        const chips = form.querySelectorAll(".amount-chip");
        const amountInput = form.querySelector('input[name="customAmount"]');
        if (!chips.length || !amountInput) return;

        chips.forEach(chip => {
            chip.addEventListener("click", function () {
                chips.forEach(c => c.classList.remove("active"));
                this.classList.add("active");
                amountInput.value = this.getAttribute("data-amount");
            });
        });

        amountInput.addEventListener("input", function () {
            chips.forEach(c => c.classList.remove("active"));
        });
    });
}

/* ==========================================================================
   7. Luhn Checksum & Real-time Card Brand Visualizer
   ========================================================================== */
function initLuhnAndCardValidator() {
    const cardInput = document.getElementById("cardNumberInput");
    const cardBadge = document.getElementById("cardBrandBadge");
    const feedback = document.getElementById("luhnFeedback");

    if (!cardInput) return;

    cardInput.addEventListener("input", function () {
        let val = this.value.replace(/\D/g, "");
        let formatted = val.match(/.{1,4}/g)?.join(" ") || val;
        this.value = formatted;

        if (cardBadge) {
            if (val.startsWith("4")) {
                cardBadge.innerHTML = '<span class="badge bg-primary">VISA</span>';
            } else if (/^5[1-5]/.test(val) || /^2[2-7]/.test(val)) {
                cardBadge.innerHTML = '<span class="badge bg-warning text-dark">Mastercard</span>';
            } else if (/^(6011|65|64[4-9])/.test(val)) {
                cardBadge.innerHTML = '<span class="badge bg-info text-dark">Discover</span>';
            } else {
                cardBadge.innerHTML = '<i class="bi bi-credit-card"></i>';
            }
        }

        if (feedback && val.length >= 13) {
            if (verifyLuhn(val)) {
                feedback.innerHTML = '<span class="text-success"><i class="bi bi-check-circle-fill"></i> Valid Card Checksum</span>';
            } else {
                feedback.innerHTML = '<span class="text-danger"><i class="bi bi-exclamation-circle-fill"></i> Invalid Card Number</span>';
            }
        } else if (feedback) {
            feedback.innerHTML = '';
        }
    });

    function verifyLuhn(cardNo) {
        let sum = 0;
        let alternate = false;
        for (let i = cardNo.length - 1; i >= 0; i--) {
            let n = parseInt(cardNo.charAt(i), 10);
            if (alternate) {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return (sum % 10 === 0);
    }
}

/* ==========================================================================
   8. Filter & Search Functions (Gallery & FAQs)
   ========================================================================== */
function initGalleryFilter() {
    const filterButtons = document.querySelectorAll(".gallery-filter-btn");
    const items = document.querySelectorAll(".gallery-card-item");

    if (!filterButtons.length || !items.length) return;

    filterButtons.forEach(btn => {
        btn.addEventListener("click", function () {
            filterButtons.forEach(b => b.classList.remove("active", "btn-primary"));
            filterButtons.forEach(b => b.classList.add("btn-outline-secondary"));
            this.classList.add("active", "btn-primary");
            this.classList.remove("btn-outline-secondary");

            const filter = this.getAttribute("data-filter");

            items.forEach(item => {
                const category = item.getAttribute("data-category");
                if (filter === "all" || category === filter) {
                    item.style.display = "block";
                } else {
                    item.style.display = "none";
                }
            });
        });
    });
}

function initFaqSearch() {
    const searchInput = document.getElementById("faqSearchInput");
    const faqItems = document.querySelectorAll(".faq-accordion-item");

    if (!searchInput || !faqItems.length) return;

    searchInput.addEventListener("input", function () {
        const query = this.value.toLowerCase().trim();

        faqItems.forEach(item => {
            const question = item.querySelector(".accordion-button")?.innerText.toLowerCase() || "";
            const answer = item.querySelector(".accordion-body")?.innerText.toLowerCase() || "";

            if (question.includes(query) || answer.includes(query)) {
                item.style.display = "block";
            } else {
                item.style.display = "none";
            }
        });
    });
}

/* ==========================================================================
   9. Active Navigation Route Highlighting & Parent Dropdown Indicators
   ========================================================================== */
function initActiveNavHighlight() {
    const currentPath = window.location.pathname.toLowerCase();
    const currentSearch = window.location.search.toLowerCase();
    const navLinks = document.querySelectorAll(
        ".navbar-giveaid .nav-link, .navbar-giveaid .dropdown-item, .admin-sidebar .nav-link, .admin-offcanvas .nav-link, .offcanvas-giveaid .nav-link"
    );

    let bestAdminMatch = null;
    let bestAdminScore = -1;

    navLinks.forEach(link => {
        const href = link.getAttribute("href");
        if (!href || href === "#") return;

        let linkPath = href;
        let linkSearch = "";
        try {
            const url = new URL(href, window.location.origin);
            linkPath = url.pathname.toLowerCase();
            linkSearch = url.search.toLowerCase();
        } catch (e) {
            linkPath = href.toLowerCase().split("?")[0];
            linkSearch = href.includes("?") ? "?" + href.toLowerCase().split("?")[1] : "";
        }

        const isExact = linkPath === currentPath;
        const isPrefix = linkPath !== "/" && currentPath.startsWith(linkPath);
        if (!isExact && !isPrefix) return;

        const isAdminNav = link.closest(".admin-sidebar, .admin-offcanvas");
        if (isAdminNav) {
            let score = linkPath.length;
            if (linkSearch && currentSearch === linkSearch) score += 100;
            else if (linkSearch && currentSearch.includes(linkSearch.replace("?", ""))) score += 40;
            else if (!linkSearch && !currentSearch) score += 20;
            else if (!linkSearch && currentSearch) score += 5;

            if (score > bestAdminScore) {
                bestAdminScore = score;
                bestAdminMatch = link;
            }
            return;
        }

        link.classList.add("active");
        const parentDropdown = link.closest(".dropdown");
        if (parentDropdown) {
            const toggle = parentDropdown.querySelector(".dropdown-toggle");
            if (toggle) toggle.classList.add("active");
        }
    });

    if (bestAdminMatch) {
        const matchHref = bestAdminMatch.getAttribute("href");
        document.querySelectorAll(".admin-sidebar .nav-link, .admin-offcanvas .nav-link").forEach(link => {
            if (link.getAttribute("href") === matchHref) {
                link.classList.add("active");
            }
        });
    }
}

/* ==========================================================================
   10. Clipboard Copy Helper with Feedback
   ========================================================================== */
function initClipboardCopy() {
    const copyBtns = document.querySelectorAll(".btn-copy-clipboard");
    copyBtns.forEach(btn => {
        btn.addEventListener("click", function () {
            const textToCopy = this.getAttribute("data-clipboard-text") || "";
            if (navigator.clipboard && textToCopy) {
                navigator.clipboard.writeText(textToCopy).then(() => {
                    const originalHtml = this.innerHTML;
                    this.innerHTML = '<i class="bi bi-check2 me-1"></i> Copied!';
                    this.classList.add("btn-success");
                    setTimeout(() => {
                        this.innerHTML = originalHtml;
                        this.classList.remove("btn-success");
                    }, 2000);
                });
            }
        });
    });
}

/* ==========================================================================
   11. Celebration Confetti Helper
   ========================================================================== */
window.triggerConfetti = function () {
    if (typeof confetti === "function") {
        confetti({
            particleCount: 150,
            spread: 90,
            origin: { y: 0.6 },
            colors: ['#10b981', '#f59e0b', '#00f5a0', '#ffd166', '#34d399']
        });
    }
};

/* ==========================================================================
   12. Mobile Offcanvas Navigation Auto-Dismiss On Link Click
   ========================================================================== */
function initMobileMenuAutoClose() {
    if (typeof bootstrap === 'undefined') return;

    ['mobileOffcanvasNav', 'adminMobileOffcanvas'].forEach(id => {
        const offcanvasEl = document.getElementById(id);
        if (!offcanvasEl) return;

        const links = offcanvasEl.querySelectorAll('a:not([data-bs-toggle])');
        links.forEach(link => {
            link.addEventListener('click', () => {
                const instance = bootstrap.Offcanvas.getInstance(offcanvasEl) || bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl);
                if (instance) {
                    instance.hide();
                }
            });
        });
    });
}

/* ==========================================================================
   13. Form Submission Loading Spinners & Double-Submit Protection
   ========================================================================== */
function initFormSubmitSpinners() {
    const forms = document.querySelectorAll('form:not(.no-auto-spinner)');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            if (this.checkValidity && !this.checkValidity()) return;
            const submitBtn = this.querySelector('button[type="submit"]:not(.no-spinner)');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.disabled = true;
                submitBtn.classList.add('btn-loading');
                const originalText = submitBtn.innerHTML;
                submitBtn.setAttribute('data-original-html', originalText);
                submitBtn.innerHTML = `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Processing...`;
                
                // Release disable timeout after 10s if response is intercepted/prevented
                setTimeout(() => {
                    if (submitBtn.disabled) {
                        submitBtn.disabled = false;
                        submitBtn.classList.remove('btn-loading');
                        submitBtn.innerHTML = originalText;
                    }
                }, 10000);
            }
        });
    });
}

/* ==========================================================================
   14. Toast Notification Helper
   ========================================================================== */
window.showGiveAidToast = function(title, message, type = 'success') {
    let toastContainer = document.getElementById('giveAidToastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'giveAidToastContainer';
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3 z-3';
        toastContainer.style.zIndex = '1090';
        document.body.appendChild(toastContainer);
    }

    const iconClass = type === 'success' ? 'bi-check-circle-fill text-success' :
                      type === 'danger' || type === 'error' ? 'bi-exclamation-triangle-fill text-danger' :
                      type === 'warning' ? 'bi-exclamation-circle-fill text-warning' : 'bi-info-circle-fill text-info';

    const toastEl = document.createElement('div');
    toastEl.className = 'toast align-items-center border-0 shadow-lg mb-2 bg-dark text-white';
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body d-flex align-items-center gap-2">
                <i class="bi ${iconClass} fs-5"></i>
                <div>
                    <strong class="d-block text-white">${title || 'Give-AID Notification'}</strong>
                    <span class="small text-muted">${message}</span>
                </div>
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    toastContainer.appendChild(toastEl);
    if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        const bsToast = new bootstrap.Toast(toastEl, { delay: 4500 });
        bsToast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }
};

/* ==========================================================================
   15. Dynamic 3D Theme Switcher Engine
   ========================================================================== */
function initThemeSwitcher() {
    const themeButtons = document.querySelectorAll(".theme-switch-btn");
    const currentTheme = document.documentElement.getAttribute("data-theme") || localStorage.getItem("giveaid_theme") || "light";

    const themeNames = {
        emerald: "Cyber Emerald",
        indigo: "Midnight Indigo",
        gold: "Royal Gold",
        ocean: "Neon Oceanic",
        light: "Clean Light"
    };

    function applyTheme(theme) {
        const safeTheme = themeNames[theme] ? theme : "light";
        document.documentElement.setAttribute("data-theme", safeTheme);
        try {
            localStorage.setItem("giveaid_theme", safeTheme);
        } catch (e) { /* private mode */ }

        document.querySelectorAll(".theme-name-label").forEach(label => {
            label.textContent = themeNames[safeTheme] || "Theme";
        });

        themeButtons.forEach(btn => {
            const isActive = btn.getAttribute("data-theme") === safeTheme;
            btn.classList.toggle("active", isActive);
            btn.setAttribute("aria-pressed", isActive ? "true" : "false");
        });

        window.dispatchEvent(new CustomEvent("themeChanged", { detail: { theme: safeTheme } }));
    }

    themeButtons.forEach(btn => {
        btn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            const theme = this.getAttribute("data-theme");
            if (theme) {
                applyTheme(theme);
            }
        });
    });

    applyTheme(currentTheme);
}

function enhancePaymentBadges() {
    document.querySelectorAll(".badge").forEach(el => {
        if (el.classList.contains("payment-badge") || el.classList.contains("payment-easypaisa") ||
            el.classList.contains("payment-jazzcash") || el.classList.contains("payment-card") ||
            el.classList.contains("payment-bank")) {
            return;
        }

        const text = (el.textContent || "").replace(/\s+/g, " ").trim().toLowerCase();
        if (!text) return;

        let cls = null;
        if (text.includes("easypaisa") || text === "easy paisa") cls = "payment-easypaisa";
        else if (text.includes("jazzcash") || text === "jazz cash") cls = "payment-jazzcash";
        else if (text === "card" || text.includes("credit") || text.includes("debit") ||
                 text.includes("visa") || text.includes("master")) cls = "payment-card";
        else if (text.includes("bank") || text.includes("manual")) cls = "payment-bank";

        if (cls) {
            el.classList.add("payment-badge", cls);
        }
    });
}

