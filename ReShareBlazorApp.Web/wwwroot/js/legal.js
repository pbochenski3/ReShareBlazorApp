window.scrollToLegalSection = function (id) {
    var el = document.getElementById(id);
    if (!el) return;

    // Height of sticky navbar + sticky legal sub-nav
    var navOffset = 140;
    var elementPosition = el.getBoundingClientRect().top;
    var offsetPosition = elementPosition + window.pageYOffset - navOffset;

    window.scrollTo({
        top: Math.max(0, offsetPosition),
        behavior: 'smooth'
    });

    // Visual pulse effect on arrival
    el.classList.remove('legal-heading-highlight');
    void el.offsetWidth; // trigger reflow
    el.classList.add('legal-heading-highlight');

    setTimeout(function () {
        el.classList.remove('legal-heading-highlight');
    }, 1500);
};

window.scrollToTop = function () {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
};

window.copyLegalLink = function (id) {
    var url = window.location.origin + window.location.pathname + '#' + id;
    if (navigator.clipboard) {
        navigator.clipboard.writeText(url);
    }
};

window.initLegalScrollspy = function () {
    var headings = document.querySelectorAll('.legal-article h2[id]');
    if (!headings.length) return;

    var observerOptions = {
        root: null,
        rootMargin: '-135px 0px -70% 0px',
        threshold: 0
    };

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                var currentId = entry.target.id;
                var links = document.querySelectorAll('.legal-toc-link');
                links.forEach(function (link) {
                    var dataTarget = link.getAttribute('data-section');
                    if (dataTarget === currentId) {
                        link.classList.add('active');
                    } else if (dataTarget) {
                        link.classList.remove('active');
                    }
                });
            }
        });
    }, observerOptions);

    headings.forEach(function (h) {
        observer.observe(h);
    });
};

// Global scroll listener for Back To Top floating button
window.addEventListener('scroll', function () {
    var btn = document.getElementById('legalBackToTop');
    if (btn) {
        if (window.pageYOffset > 380) {
            btn.classList.add('visible');
        } else {
            btn.classList.remove('visible');
        }
    }
});

// Global click handler to capture any anchor links pointing to sections
document.addEventListener('click', function (e) {
    var anchor = e.target.closest('a[href*="#"]');
    if (!anchor) return;

    var href = anchor.getAttribute('href');
    if (href && (href.startsWith('#') || href.indexOf('#') !== -1)) {
        var hash = href.substring(href.indexOf('#') + 1);
        if (hash) {
            var el = document.getElementById(hash);
            if (el) {
                e.preventDefault();
                window.scrollToLegalSection(hash);
            }
        }
    }
});
