(function () {
    'use strict';

    function onReady(fn) {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
    }

    onReady(function () {
        var modal = document.getElementById('paymentModal');
        var confirmBtns = document.querySelectorAll('[data-confirm]');   // desktop card button + mobile bottom bar
        if (!modal || !confirmBtns.length) return;

        var opener = null;

        function focusables() {
            return Array.prototype.slice.call(modal.querySelectorAll('button:not([disabled])'));
        }

        function openPaymentModal() {
            opener = document.activeElement;
            modal.hidden = false;
            document.body.style.overflow = 'hidden';
            var items = focusables();
            // first payment option if there is one, otherwise the close button
            (modal.querySelector('.rc-payment-option') || items[0] || modal).focus();
        }

        function closePaymentModal() {
            if (modal.hidden) return;
            modal.hidden = true;
            document.body.style.overflow = '';
            if (opener && opener.focus) opener.focus();
        }

        function selectPayment(type) {
            // Extension point for the payment step: listen for this event to start the real flow.
            document.dispatchEvent(new CustomEvent('payment:selected', { detail: { type: type } }));
            closePaymentModal();
        }

        confirmBtns.forEach(function (btn) { btn.addEventListener('click', openPaymentModal); });
        document.getElementById('paymentModalClose').addEventListener('click', closePaymentModal);
        modal.addEventListener('click', function (e) { if (e.target === modal) closePaymentModal(); });

        modal.querySelectorAll('[data-payment]').forEach(function (btn) {
            btn.addEventListener('click', function () { selectPayment(btn.getAttribute('data-payment')); });
        });

        document.addEventListener('keydown', function (e) {
            if (modal.hidden) return;
            if (e.key === 'Escape') { closePaymentModal(); return; }
            if (e.key === 'Tab') {                       // keep focus inside the dialog
                var items = focusables();
                if (!items.length) return;
                var first = items[0], last = items[items.length - 1];
                if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
                else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            }
        });

        // Back/forward cache: never come back with the dialog still open
        window.addEventListener('pageshow', closePaymentModal);

        window.ReviewConfirm = { open: openPaymentModal, close: closePaymentModal };
    });
})();
