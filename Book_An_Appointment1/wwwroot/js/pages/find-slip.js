// Find my slip (/find-slip): mobile → OTP → slips issued for that mobile
(function () {
    'use strict';
    var cfg = window.FindSlipConfig || {};
    var $ = function (id) { return document.getElementById(id); };
    var timer = null;

    function token() { var el = document.querySelector('input[name="__RequestVerificationToken"]'); return el ? el.value : ''; }

    function post(handler, body) {
        return fetch('?handler=' + handler, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body: JSON.stringify(body)
        }).then(function (r) { return r.json(); });
    }

    function error(msg) { $('fsError').textContent = msg || ''; $('fsError').hidden = !msg; }

    function digitsOnly(el) { el.addEventListener('input', function () { el.value = el.value.replace(/\D/g, ''); error(''); }); }

    function startTimer() {
        var s = 30;
        clearInterval(timer);
        $('fsResend').disabled = true;
        $('fsTimer').textContent = t('Js.ResendIn', '0:' + s);
        timer = setInterval(function () {
            s--;
            $('fsTimer').textContent = s > 0 ? t('Js.ResendIn', '0:' + String(s).padStart(2, '0')) : '';
            if (s <= 0) { clearInterval(timer); $('fsResend').disabled = false; }
        }, 1000);
    }

    function send() {
        var mobile = $('fsMobile').value.trim();
        if (!/^\d{10}$/.test(mobile)) { error(t('Validation.MobileInvalid')); $('fsMobile').focus(); return; }
        error('');
        $('fsSend').disabled = true;
        $('fsSend').textContent = t('Js.Sending');
        post('FindSlipSendOtp', { mobileNo: mobile }).then(function (res) {
            if (!res.success) { reset(); error(res.message || t('Errors.OtpSendFailed')); return; }
            $('fsSend').textContent = t('Js.OtpSent');
            $('fsOtpBox').hidden = false;
            $('fsOtp').value = '';
            $('fsOtp').focus();
            startTimer();
        }).catch(function () { reset(); error(t('Js.SomethingWrong')); });
    }

    function reset() { $('fsSend').disabled = false; $('fsSend').textContent = t('Js.SendOtp'); }

    function verify() {
        var mobile = $('fsMobile').value.trim();
        var otp = $('fsOtp').value.trim();
        if (!new RegExp('^\\d{' + (cfg.otpLength || 6) + '}$').test(otp)) { error(t('Errors.InvalidOtp')); $('fsOtp').focus(); return; }
        error('');
        $('fsVerifyBtn').disabled = true;
        post('FindSlipVerify', { mobileNo: mobile, otpNo: otp }).then(function (res) {
            $('fsVerifyBtn').disabled = false;
            if (!res.success) { error(res.message || t('Errors.InvalidOtpRetry')); $('fsOtp').select(); return; }
            showSlips(res.slips || []);
        }).catch(function () { $('fsVerifyBtn').disabled = false; error(t('Js.SomethingWrong')); });
    }

    function el(tag, cls, text) { var e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; }

    function showSlips(slips) {
        clearInterval(timer);
        $('fsVerify').hidden = true;
        $('fsResult').hidden = false;
        $('fsCount').textContent = slips.length ? t('Js.SlipsFound', slips.length) : t('Js.NoSlipsFound');
        var list = $('fsList');
        list.innerHTML = '';
        slips.forEach(function (s) {
            var card = el('div', 'fs-slip' + (s.upcoming ? '' : ' fs-past'));
            var info = el('div', 'fs-slip-info');

            // Whose slip — patient name + UHID first (family members share one mobile)
            var who = el('div', 'fs-slip-who');
            who.appendChild(el('span', 'fs-slip-name', s.patientName || t('Js.Patient')));
            if (s.uhid != null && String(s.uhid).trim() !== '') who.appendChild(el('span', 'fs-chip', t('Js.Uhid') + ' ' + s.uhid));
            if (!s.upcoming) who.appendChild(el('span', 'fs-badge', t('Js.PastAppointment')));
            info.appendChild(who);

            info.appendChild(el('div', 'fs-slip-when', [s.date, s.time].filter(Boolean).join('  ·  ')));
            info.appendChild(el('div', 'fs-slip-doc', [s.doctorName, s.specialisation].filter(Boolean).join('  ·  ')));
            info.appendChild(el('div', 'fs-slip-id', t('Js.AppointmentIdLabel') + ' ' + s.appointmentId));
            card.appendChild(info);

            var actions = el('div', 'fs-slip-actions');
            var pdf = el('a', 'fs-btn fs-btn-primary', '⬇ ' + t('Js.DownloadPdf'));
            pdf.href = s.pdfUrl; pdf.setAttribute('download', '');
            actions.appendChild(pdf);
            if (cfg.viewSlip) {
                var view = el('a', 'fs-btn', t('Js.ViewSlip'));
                view.href = s.slipUrl; view.target = '_blank'; view.rel = 'noopener';
                actions.appendChild(view);
            }
            card.appendChild(actions);
            list.appendChild(card);
        });
    }

    function otherNumber() {
        $('fsResult').hidden = true;
        $('fsVerify').hidden = false;
        $('fsOtpBox').hidden = true;
        $('fsMobile').value = '';
        reset();
        $('fsMobile').focus();
    }

    digitsOnly($('fsMobile'));
    digitsOnly($('fsOtp'));
    $('fsSend').addEventListener('click', send);
    $('fsResend').addEventListener('click', send);
    $('fsVerifyBtn').addEventListener('click', verify);
    $('fsOther').addEventListener('click', otherNumber);
    $('fsMobile').addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); send(); } });
    $('fsOtp').addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); verify(); } });
    $('fsMobile').addEventListener('input', function () {          // number changed → OTP has to be sent again
        if (!$('fsOtpBox').hidden) { $('fsOtpBox').hidden = true; reset(); clearInterval(timer); }
    });
})();
