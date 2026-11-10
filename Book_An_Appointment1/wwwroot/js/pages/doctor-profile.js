// Doctor profile: photo fallback (initials) and Share (phone share sheet, else copy link)
(function () {
    'use strict';

    function onReady(fn) {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
    }

    onReady(function () {
        var img = document.querySelector('[data-photo]');
        if (img) {
            var fail = function () { img.parentNode.classList.add('no-photo'); img.remove(); };
            if (img.complete && img.naturalWidth === 0) fail();
            else img.addEventListener('error', fail);
        }

        var btn = document.querySelector('[data-share]');
        if (!btn) return;
        var label = btn.querySelector('[data-share-label]');
        var original = label.textContent;

        btn.addEventListener('click', function () {
            var url = btn.getAttribute('data-url');
            if (navigator.share) {
                navigator.share({ title: btn.getAttribute('data-title'), url: url }).catch(function () { });
                return;
            }
            copy(url).then(function () {
                label.textContent = t('Js.LinkCopied');
                setTimeout(function () { label.textContent = original; }, 2000);
            }, function () { window.prompt(t('Js.CopyLink'), url); });
        });
    });

    function copy(text) {
        if (navigator.clipboard && window.isSecureContext) return navigator.clipboard.writeText(text);
        return new Promise(function (resolve, reject) {
            var area = document.createElement('textarea');
            area.value = text;
            area.setAttribute('readonly', '');
            area.style.position = 'fixed';
            area.style.opacity = '0';
            document.body.appendChild(area);
            area.select();
            try { document.execCommand('copy') ? resolve() : reject(); } catch (e) { reject(e); }
            area.remove();
        });
    }
})();
