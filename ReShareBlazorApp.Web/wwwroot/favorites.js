// Gwiazdki ulubionych: przełączanie bez przeładowania strony.
// Wszystko działa na delegacji zdarzeń, więc także po nawigacji Blazora.
(function () {
    function toast(message) {
        var el = document.createElement('div');
        el.className = 'fav-toast';
        el.textContent = message;
        document.body.appendChild(el);
        requestAnimationFrame(function () { el.classList.add('show'); });
        setTimeout(function () {
            el.classList.remove('show');
            setTimeout(function () { el.remove(); }, 300);
        }, 6000);
    }

    function setState(button, isFav) {
        button.classList.toggle('is-fav', isFav);              // żółta, dopóki nie odznaczymy
        button.setAttribute('aria-pressed', isFav ? 'true' : 'false');

        var title = isFav ? button.dataset.titleOn : button.dataset.titleOff;
        if (title) { button.title = title; button.setAttribute('aria-label', title); }

        var label = button.querySelector('.fav-label');
        if (label) label.textContent = isFav ? button.dataset.textOn : button.dataset.textOff;

        if (isFav) {                                           // mała animacja po dodaniu
            button.classList.remove('fav-pop');
            void button.offsetWidth;
            button.classList.add('fav-pop');
        }
    }

    document.addEventListener('submit', async function (e) {
        var form = e.target;
        if (!form.classList || !form.classList.contains('fav-form')) return;
        e.preventDefault();

        var button = form.querySelector('button');
        if (button.disabled) return;
        button.disabled = true;

        try {
            var response = await fetch(form.action, {
                method: 'POST',
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin',
                body: new URLSearchParams(new FormData(form))
            });

            var data = null;
            try { data = await response.json(); } catch (_) { }

            if (data && data.needsLogin) {                     // niezalogowany -> logowanie
                window.location.href = data.loginUrl || '/login';
                return;
            }
            if (!response.ok || !data || typeof data.isFavorite !== 'boolean') {
                toast((data && data.error) || ('Nie udało się zapisać ulubionych (błąd ' + response.status + ').'));
                return;
            }

            setState(button, data.isFavorite);

            // w zakładce Profil -> Ulubione odznaczone ogłoszenie znika z listy
            var list = form.closest('[data-remove-on-unfav]');
            var card = form.closest('.home-card');
            if (list && card && !data.isFavorite) {
                card.classList.add('fav-removing');
                setTimeout(function () { card.remove(); }, 300);
            }
        } catch (err) {
            toast('Brak połączenia ze stroną: ' + err.message);
        } finally {
            button.disabled = false;
        }
    });
})();
