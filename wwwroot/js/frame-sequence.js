/**
 * Give-AID — Cinematic Frame-Sequence Scroll Animation
 * Canvas + preloaded images + GSAP ScrollTrigger scrub
 *
 * Frame URL pattern: /frames/frame-0001.jpg … frame-0072.jpg
 * Scroll progress maps linearly to frame index (0 → last).
 */
(function (window, document) {
    'use strict';

    var FRAME_COUNT = 72;
    var FRAME_BASE = '/frames/frame-';
    var FRAME_EXT = '.jpg';
    var PAD = 4;

    /** @type {HTMLImageElement[]} */
    var frames = [];
    var loadedCount = 0;
    var currentFrame = -1;
    var pendingFrame = 0;
    var rafId = 0;
    var needsRender = false;

    /** @type {HTMLCanvasElement|null} */
    var canvas = null;
    /** @type {CanvasRenderingContext2D|null} */
    var ctx = null;
    /** @type {HTMLElement|null} */
    var section = null;
    /** @type {HTMLElement|null} */
    var stage = null;
    /** @type {HTMLElement|null} */
    var loadingEl = null;
    /** @type {HTMLElement|null} */
    var progressEl = null;

    var scrollTriggerInstance = null;
    var frameProxy = { frame: 0 };
    var reducedMotion = false;
    var firstFrameReady = false;
    var destroyed = false;

    function frameUrl(index1Based) {
        var n = String(index1Based);
        while (n.length < PAD) n = '0' + n;
        return FRAME_BASE + n + FRAME_EXT;
    }

    function prefersReducedMotion() {
        return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    /**
     * Cover-fit draw: preserve aspect ratio, fill canvas, crop edges if needed.
     */
    function drawCover(img) {
        if (!ctx || !canvas || !img || !img.complete || !img.naturalWidth) return;

        var cw = canvas.width;
        var ch = canvas.height;
        var iw = img.naturalWidth;
        var ih = img.naturalHeight;
        var scale = Math.max(cw / iw, ch / ih);
        var dw = iw * scale;
        var dh = ih * scale;
        var dx = (cw - dw) / 2;
        var dy = (ch - dh) / 2;

        ctx.clearRect(0, 0, cw, ch);
        ctx.drawImage(img, dx, dy, dw, dh);
    }

    function renderFrame(index) {
        var safe = Math.max(0, Math.min(FRAME_COUNT - 1, index | 0));
        if (safe === currentFrame) return;

        var img = frames[safe];
        if (!img || !img.complete || !img.naturalWidth) {
            // Seek nearest loaded frame so scrub never blanks the canvas
            var fallback = findNearestLoaded(safe);
            if (fallback < 0) return;
            safe = fallback;
            img = frames[safe];
        }

        currentFrame = safe;
        drawCover(img);
    }

    function findNearestLoaded(target) {
        if (frames[target] && frames[target].complete && frames[target].naturalWidth) return target;
        for (var d = 1; d < FRAME_COUNT; d++) {
            var a = target - d;
            var b = target + d;
            if (a >= 0 && frames[a] && frames[a].complete && frames[a].naturalWidth) return a;
            if (b < FRAME_COUNT && frames[b] && frames[b].complete && frames[b].naturalWidth) return b;
        }
        return -1;
    }

    function scheduleRender(index) {
        pendingFrame = index;
        needsRender = true;
        if (!rafId) {
            rafId = window.requestAnimationFrame(function () {
                rafId = 0;
                if (!needsRender || destroyed) return;
                needsRender = false;
                renderFrame(pendingFrame);
            });
        }
    }

    /**
     * HiDPI-aware canvas sizing from the stage element's CSS box.
     */
    function resizeCanvas() {
        if (!canvas || !stage || !ctx) return;

        var rect = stage.getBoundingClientRect();
        var cssW = Math.max(1, Math.floor(rect.width));
        var cssH = Math.max(1, Math.floor(rect.height));
        var dpr = Math.min(window.devicePixelRatio || 1, 2);

        var pixelW = Math.round(cssW * dpr);
        var pixelH = Math.round(cssH * dpr);

        if (canvas.width !== pixelW || canvas.height !== pixelH) {
            canvas.width = pixelW;
            canvas.height = pixelH;
            canvas.style.width = cssW + 'px';
            canvas.style.height = cssH + 'px';
            // Reset transform after size change; draw in device pixels
            ctx.setTransform(1, 0, 0, 1, 0, 0);
            // Force redraw of current frame at new size
            var idx = currentFrame >= 0 ? currentFrame : 0;
            currentFrame = -1;
            renderFrame(idx);
        }
    }

    function updateLoadingUI() {
        if (!loadingEl) return;
        var pct = Math.round((loadedCount / FRAME_COUNT) * 100);
        if (progressEl) {
            progressEl.style.width = pct + '%';
            progressEl.setAttribute('aria-valuenow', String(pct));
        }
        var label = loadingEl.querySelector('[data-frame-loading-label]');
        if (label) {
            label.textContent = firstFrameReady
                ? 'Loading sequence… ' + pct + '%'
                : 'Preparing cinematic sequence…';
        }

        if (loadedCount >= FRAME_COUNT) {
            loadingEl.classList.add('is-done');
            window.setTimeout(function () {
                if (loadingEl) loadingEl.setAttribute('hidden', '');
            }, 400);
        }
    }

    function markHeroReady() {
        if (!section) return;
        section.classList.add('is-ready');
        section.classList.remove('is-loading');
    }

    /**
     * Priority-load frame 0, then progressively fill the rest in batches
     * so the page stays interactive during preload.
     */
    function preloadFrames() {
        frames = new Array(FRAME_COUNT);

        function loadOne(i) {
            return new Promise(function (resolve) {
                if (destroyed) {
                    resolve();
                    return;
                }
                var img = new Image();
                img.decoding = 'async';
                img.onload = function () {
                    frames[i] = img;
                    loadedCount++;
                    updateLoadingUI();
                    if (i === 0 && !firstFrameReady) {
                        firstFrameReady = true;
                        markHeroReady();
                        resizeCanvas();
                        currentFrame = -1;
                        renderFrame(0);
                    }
                    resolve();
                };
                img.onerror = function () {
                    // Keep slot empty; nearest-frame fallback handles scrub gaps
                    loadedCount++;
                    updateLoadingUI();
                    resolve();
                };
                img.src = frameUrl(i + 1);
            });
        }

        // First frame immediately, then remaining in small concurrent batches
        return loadOne(0).then(function () {
            var batchSize = 4;
            var chain = Promise.resolve();
            var next = 1;

            function runBatch() {
                if (destroyed || next >= FRAME_COUNT) return Promise.resolve();
                var batch = [];
                for (var b = 0; b < batchSize && next < FRAME_COUNT; b++, next++) {
                    batch.push(loadOne(next));
                }
                return Promise.all(batch).then(runBatch);
            }

            return runBatch();
        });
    }

    /**
     * Scroll distance scales with frame count — cinematic but not endless.
     * ~28px per frame, clamped for mobile/desktop comfort.
     */
    function scrollDistancePx() {
        var perFrame = window.innerWidth < 768 ? 22 : 30;
        var raw = FRAME_COUNT * perFrame;
        return Math.max(1600, Math.min(2800, raw));
    }

    function setupScrollTrigger() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') {
            console.warn('[FrameSequence] GSAP/ScrollTrigger missing — static first frame only.');
            return;
        }

        gsap.registerPlugin(ScrollTrigger);

        frameProxy.frame = 0;

        scrollTriggerInstance = ScrollTrigger.create({
            trigger: section,
            start: 'top top',
            end: function () {
                return '+=' + scrollDistancePx();
            },
            pin: true,
            scrub: 0.6,
            anticipatePin: 1,
            invalidateOnRefresh: true,
            onUpdate: function (self) {
                var idx = Math.round(self.progress * (FRAME_COUNT - 1));
                frameProxy.frame = idx;
                scheduleRender(idx);

                // Hide scroll hint once the user starts scrubbing
                var hint = section.querySelector('.cinematic-sequence__scroll-hint');
                if (hint) {
                    if (self.progress > 0.04) hint.classList.add('is-hidden');
                    else hint.classList.remove('is-hidden');
                }
            }
        });

        // Minimal copy fades out so frames stay primary during scrub
        var overlay = section.querySelector('.cinematic-sequence__copy');
        if (overlay) {
            gsap.fromTo(
                overlay,
                { opacity: 1, y: 0 },
                {
                    opacity: 0,
                    y: -40,
                    ease: 'none',
                    scrollTrigger: {
                        trigger: section,
                        start: 'top top',
                        end: function () {
                            return '+=' + (scrollDistancePx() * 0.45);
                        },
                        scrub: 0.5
                    }
                }
            );
        }

        setupDonationEntrance();
    }

    /**
     * Subtle fade/slide when the post-cinematic Quick Giving section enters view.
     */
    function setupDonationEntrance() {
        var donateSection = document.getElementById('quickGivingSection');
        if (!donateSection) return;
        var target = donateSection.querySelector('[data-donation-entrance]');
        if (!target) return;

        if (prefersReducedMotion()) {
            target.classList.add('is-visible');
            return;
        }

        ScrollTrigger.create({
            trigger: donateSection,
            start: 'top 82%',
            once: true,
            onEnter: function () {
                target.classList.add('is-visible');
            }
        });
    }

    function setupReducedMotion() {
        // Show first frame only; no pin / scrub
        section.classList.add('is-reduced-motion');
        markHeroReady();
        loadOneStatic(0);
        var donateEntrance = document.querySelector('#quickGivingSection [data-donation-entrance]');
        if (donateEntrance) donateEntrance.classList.add('is-visible');
    }

    function loadOneStatic(i) {
        var img = new Image();
        img.onload = function () {
            frames[i] = img;
            firstFrameReady = true;
            resizeCanvas();
            currentFrame = -1;
            renderFrame(i);
            if (loadingEl) {
                loadingEl.classList.add('is-done');
                loadingEl.setAttribute('hidden', '');
            }
        };
        img.src = frameUrl(i + 1);
        frames = new Array(FRAME_COUNT);
    }

    function onResize() {
        resizeCanvas();
        if (scrollTriggerInstance) {
            ScrollTrigger.refresh();
        }
    }

    function destroy() {
        destroyed = true;
        window.removeEventListener('resize', onResize);
        if (rafId) {
            window.cancelAnimationFrame(rafId);
            rafId = 0;
        }
        if (scrollTriggerInstance) {
            scrollTriggerInstance.kill();
            scrollTriggerInstance = null;
        }
        if (typeof ScrollTrigger !== 'undefined') {
            ScrollTrigger.getAll().forEach(function (st) {
                if (st.trigger === section) st.kill();
            });
        }
    }

    function init() {
        section = document.getElementById('cinematicSequence');
        if (!section) return;

        canvas = document.getElementById('frameSequenceCanvas');
        stage = section.querySelector('.cinematic-sequence__stage');
        loadingEl = section.querySelector('.cinematic-sequence__loading');
        progressEl = section.querySelector('[data-frame-loading-bar]');

        if (!canvas || !stage) return;

        ctx = canvas.getContext('2d', { alpha: false });
        if (!ctx) return;

        reducedMotion = prefersReducedMotion();
        section.classList.add('is-loading');

        window.addEventListener('resize', onResize, { passive: true });
        window.addEventListener('pagehide', destroy, { once: true });

        resizeCanvas();

        if (reducedMotion) {
            setupReducedMotion();
            return;
        }

        preloadFrames().then(function () {
            if (destroyed) return;
            setupScrollTrigger();
            resizeCanvas();
            if (typeof ScrollTrigger !== 'undefined') {
                ScrollTrigger.refresh();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.GiveAidFrameSequence = {
        destroy: destroy,
        renderFrame: renderFrame,
        frameCount: FRAME_COUNT
    };
})(window, document);
