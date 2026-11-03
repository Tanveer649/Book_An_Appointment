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

        var bookedModal = document.getElementById('bookedModal');
        var errorBox = document.getElementById('paymentError');
        var statusBox = document.getElementById('paymentStatus');
        var opener = null;
        var busy = false;                                                // a CreateAppointment call is in flight

        function focusables() {
            return Array.prototype.slice.call(modal.querySelectorAll('button:not([disabled])'));
        }

        function openPaymentModal() {
            opener = document.activeElement;
            showError('');
            modal.hidden = false;
            document.body.style.overflow = 'hidden';
            var items = focusables();
            // first payment option if there is one, otherwise the close button
            (modal.querySelector('.rc-payment-option') || items[0] || modal).focus();
        }

        function closePaymentModal() {
            if (modal.hidden || busy) return;                            // never close while booking
            modal.hidden = true;
            document.body.style.overflow = '';
            if (opener && opener.focus) opener.focus();
        }

        // ── Create appointment ───────────────────────────────
        function token() {
            var el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        function setBusy(on) {
            busy = on;
            modal.querySelectorAll('[data-payment], #paymentModalClose').forEach(function (b) { b.disabled = on; });
            if (statusBox) statusBox.hidden = !on;
        }

        function showError(msg) {
            if (!errorBox) return;
            errorBox.textContent = msg || '';
            errorBox.hidden = !msg;
        }

        // ── Client Side Validation (server validates again) ──
        function validateBooking(type) {
            if (type !== 'arrival' && type !== 'now')
                return t('Js.ChoosePaymentOption');

            // page may have been open for a while — slot time must still be in the future
            var slot = new Date(String(modal.getAttribute('data-slot-start') || '').replace(' ', 'T'));
            // unknown format → leave it to the server check instead of blocking the booking
            if (!isNaN(slot.getTime()) && slot <= new Date())
                return t('Errors.SlotPassed');

            return '';
        }

        function selectPayment(type) {          // 'arrival' | 'now'
            if (busy) return;
            var error = validateBooking(type);
            showError(error);
            if (error) return;
            setBusy(true);

            fetch('?handler=CreateAppointment', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify({ paymentType: type })
            })
                .then(function (r) {
                    if (!r.ok) throw new Error('HTTP ' + r.status);
                    return r.json();
                })
                .then(function (res) {
                    if (res.success && res.redirectUrl) {                // Pay now → payment gateway (stay busy while leaving)
                        window.location.href = res.redirectUrl;
                        return;
                    }
                    if (res.success && res.appointment) {               // Pay on arrival → booked popup
                        setBusy(false);
                        closePaymentModal();
                        showBooked(res.appointment);
                        return;
                    }
                    setBusy(false);
                    showError(res.message || t('Errors.BookingFailed'));
                    if (res.redirectUrl) {                               // session expired → back to page 1
                        setTimeout(function () { window.location.href = res.redirectUrl; }, 2000);
                    }
                })
                .catch(function () {
                    setBusy(false);
                    showError(t('Js.SomethingWrong'));
                });
        }

        // ── Booked popup ─────────────────────────────────────
        function showBooked(a) {
            if (!bookedModal) return;
            bookedModal.querySelectorAll('[data-booked]').forEach(function (el) {
                el.textContent = a[el.getAttribute('data-booked')] || '';
            });
            // hide rows/tiles the API and session did not fill
            bookedModal.querySelectorAll('[data-booked-row]').forEach(function (row) {
                row.hidden = !Array.prototype.some.call(row.querySelectorAll('[data-booked]'),
                    function (el) { return el.textContent.trim() !== ''; });
            });
            confirmBtns.forEach(function (b) { b.disabled = true; });    // already booked, no second booking
            bookedModal.hidden = false;
            document.body.style.overflow = 'hidden';
            var done = document.getElementById('bookedDone');
            if (done) done.focus();
        }

        confirmBtns.forEach(function (btn) { btn.addEventListener('click', openPaymentModal); });
        document.getElementById('paymentModalClose').addEventListener('click', closePaymentModal);
        modal.addEventListener('click', function (e) { if (e.target === modal) closePaymentModal(); });

        modal.querySelectorAll('[data-payment]').forEach(function (btn) {
            btn.addEventListener('click', function () { selectPayment(btn.getAttribute('data-payment')); });
        });

        document.addEventListener('keydown', function (e) {
            if (bookedModal && !bookedModal.hidden) {                    // booked popup has a single action
                if (e.key === 'Tab') { e.preventDefault(); document.getElementById('bookedDone').focus(); }
                return;
            }
            if (modal.hidden) return;
            if (e.key === 'Escape') { closePaymentModal(); return; }
            if (e.key === 'Tab') {                       // keep focus inside the dialog
                var items = focusables();
                if (!items.length) { e.preventDefault(); return; }
                var first = items[0], last = items[items.length - 1];
                if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
                else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            }
        });

        // Back/forward cache (e.g. Back from the payment gateway): never come back with the dialog still open
        window.addEventListener('pageshow', function () {
            setBusy(false);
            closePaymentModal();
        });

        window.ReviewConfirm = { open: openPaymentModal, close: closePaymentModal };
    });
})();
