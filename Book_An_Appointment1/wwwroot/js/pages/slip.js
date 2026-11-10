// Appointment slip page: Print button, and ?print=1 (Print from the booked popup) opens the print dialog once
(function () {
    'use strict';
    var script = document.currentScript;

    function onReady(fn) {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
    }

    onReady(function () {
        document.querySelectorAll('[data-print]').forEach(function (btn) {
            btn.addEventListener('click', function () { window.print(); });
        });

        if (script && script.getAttribute('data-autoprint') === '1') {
            // drop ?print=1 so a reload / shared link does not print again
            if (window.history && history.replaceState) history.replaceState(null, '', location.pathname);
            setTimeout(function () { window.print(); }, 400);   // let fonts settle first
        }
    });
})();
