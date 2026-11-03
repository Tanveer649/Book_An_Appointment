/* ── Validation ───────────────────────────────────────── */
var AppointmentValidation = {
    rules: {
        facility: { selector: '#facilityDropdown', message: 'Validation.SelectFacility' },
        speciality: { selector: '#specialityDropdown', message: 'Validation.SelectSpeciality' },
        doctor: { selector: '#doctorDropdown', message: 'Validation.SelectDoctor' },
        slot: { selector: '#hdnSelectedSlotStart', message: 'Validation.SelectSlot' }
    },
    validate: function () {
        var errors = [];
        $.each(this.rules, function (key, rule) {
            if (!$(rule.selector).val()) errors.push(t(rule.message));
        });
        return errors;
    },
    showErrors: function (errors) {
        var $box = $('#clientValidationError').empty();
        if (!errors.length) return;
        var $ul = $('<ul class="mb-0"></ul>');
        $.each(errors, function (i, e) { $ul.append($('<li></li>').text(e)); });
        $box.append($ul);
        if ($box[0] && $box[0].scrollIntoView) $box[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
    },
    clearErrors: function () { $('#clientValidationError').empty(); }
};

/* ── Pure helpers (no DOM reads) ──────────────────────── */

var AppointmentUI = (function () {
    // Names in the selected portal language (i18n.js)
    var DAYS = AppI18n.days('short');
    var MONTHS = AppI18n.months('short');
    var AMPM = AppI18n.dayPeriods();
    var BUCKETS = ['Morning', 'Afternoon', 'Evening', 'Slots'];   // keys — label via t('Js.Morning') ...

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }
    function pad(n) { return n < 10 ? '0' + n : '' + n; }
    function parseSlot(str) {
        var d = new Date(String(str || '').replace(' ', 'T'));
        return isNaN(d.getTime()) ? null : d;
    }
    function formatTime(d) {
        var h = d.getHours();
        return pad(h % 12 || 12) + ':' + pad(d.getMinutes()) + ' ' + (h < 12 ? AMPM[0] : AMPM[1]);
    }
    function formatDay(d) { return DAYS[d.getDay()] + ', ' + d.getDate() + ' ' + MONTHS[d.getMonth()]; }
    function toDateStr(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); }
    function bucketOf(d) {
        if (!d) return 'Slots';
        return d.getHours() < 12 ? 'Morning' : d.getHours() < 17 ? 'Afternoon' : 'Evening';
    }
    function clean(v) {
        v = (v == null ? '' : String(v)).trim();
        return (!v || /^(n\/?a|null|undefined|-+)$/i.test(v)) ? '' : v;
    }
    function list(v) {
        return clean(v).split(',').map(function (x) { return x.trim(); }).filter(Boolean).join(', ');
    }
    function initials(name) {
        return String(name || '').replace(/^dr\.?\s+/i, '').split(/\s+/).filter(Boolean).slice(0, 2)
            .map(function (w) { return w.charAt(0).toUpperCase(); }).join('') || 'Dr';
    }
    // Icon first, then ImageUrl. http images go through the server handler (browsers block/fail http assets from an https site).
    function photoSrc(doc) {
        var u = clean(doc.icon) || clean(doc.imageUrl);
        if (!u) return '';
        return /^http:\/\//i.test(u) ? '?handler=DoctorImage&url=' + encodeURIComponent(u) : u;
    }

    function groupSlots(slots) {
        var map = {};
        (slots || []).forEach(function (s) {
            var d = parseSlot(s.startTime);
            (map[bucketOf(d)] = map[bucketOf(d)] || []).push({ slot: s, label: d ? formatTime(d) : s.startTime });
        });
        return BUCKETS.filter(function (b) { return map[b]; }).map(function (b) { return { name: b, items: map[b] }; });
    }

    function renderSlots(slots, selectedStart, emptyMsg) {
        var groups = groupSlots(slots);
        if (!groups.length) return '<div class="text-muted">' + esc(emptyMsg || t('Js.NoSlotsForDate')) + '</div>';
        return groups.map(function (g) {
            var btns = g.items.map(function (it) {
                var on = selectedStart && it.slot.startTime === selectedStart;
                return '<button type="button" class="slot-btn' + (on ? ' is-selected' : '') + '" aria-pressed="' + (on ? 'true' : 'false') +
                    '" data-start="' + esc(it.slot.startTime) + '" data-end="' + esc(it.slot.endTime) + '">' + esc(it.label) + '</button>';
            }).join('');
            return '<div class="slot-group"><div class="slot-group-label">' + esc(t('Js.' + g.name)) +
                ' <span class="slot-group-count">' + g.items.length + '</span></div><div class="slot-group-grid">' + btns + '</div></div>';
        }).join('');
    }

    function renderDoctorCard(doc) {
        var src = photoSrc(doc);
        var rows = [];
        if (list(doc.education)) rows.push([t('Js.Education'), list(doc.education)]);
        if (clean(doc.experience)) rows.push([t('Js.Experience'), clean(doc.experience)]);
        rows.push([t('Js.NextAvailable'), clean(doc.timing) || t('Js.NotSpecified')]);
        if (list(doc.treatment)) rows.push([t('Js.Treatments'), list(doc.treatment)]);
        if (clean(doc.hospitalName)) rows.push([t('Js.Hospital'), doc.hospitalName + (clean(doc.hospitalCity) ? ', ' + doc.hospitalCity : '')]);

        return '<div class="doctor-head"><div class="doctor-avatar-col">' +
            '<div class="doctor-avatar-wrap' + (src ? '' : ' no-photo') + '" data-initials="' + esc(initials(doc.name)) + '">' +
            (src ? '<img class="doctor-avatar" src="' + esc(src) + '" alt="" onerror="this.onerror=null; this.parentNode.classList.add(\'no-photo\'); this.remove();" />' : '') + '</div>' +
            '<div class="doctor-rating"><span class="rating-badge">★ ' + esc(clean(doc.rating) || t('Js.NA')) + '</span>' +
            '<span class="review-count">' + esc(t('Js.Reviews', clean(doc.review) || '0')) + '</span></div></div>' +
            '<div class="doctor-info"><h3 class="doctor-name">' + esc(doc.name) + '</h3>' +
            '<p class="doctor-speciality">' + esc(clean(doc.specialisation)) + '</p>' +
            (doc.isTeleconsult ? '<span class="teleconsult-badge">' + esc(t('Js.Teleconsultation')) + '</span>' : '') + '</div></div>' +
            '<div class="fee-block"><span class="fee-label">' + esc(t('Js.ConsultationFee')) + '</span><span class="fee-value">₹ ' + esc(doc.fee) + '</span></div>' +
            '<dl class="doctor-details">' + rows.map(function (r) {
                return '<div class="detail"><dt>' + esc(r[0]) + '</dt><dd>' + esc(r[1]) + '</dd></div>';
            }).join('') + '</dl>';
    }

    function renderBooking(slotsHtml, noteText) {
        return '<div id="selectionSummary" class="selection-banner" aria-live="polite"><span class="selection-label">' + esc(t('Js.YourSlot')) + '</span>' +
            '<span id="selectionValue" class="selection-value">' + esc(t('Js.SelectDateTime')) + '</span></div>' +
            '<div class="booking-grid">' +
            '<div class="calendar-panel"><h5 class="panel-title">' + esc(t('Js.SelectDate')) + '</h5>' +
            '<input type="text" id="slotCalendar" class="form-control" placeholder="' + esc(t('Js.SelectDate')) + '" readonly style="display:none;" />' +
            '<p class="calendar-note">' + esc(noteText || '') + '</p></div>' +
            '<div class="slots-panel"><div class="slots-head"><h5 class="panel-title">' + esc(t('Js.AvailableSlots')) + '</h5><span id="slotDateLabel"></span></div>' +
            '<div id="slotsContainer" class="slots-container">' + slotsHtml + '</div></div></div>' +
            '<div class="booking-actions"><button type="button" id="continueBtn" class="btn-continue">' + esc(t('Js.Continue')) + '</button></div>';
    }

    return {
        esc: esc, parseSlot: parseSlot, formatTime: formatTime, formatDay: formatDay, toDateStr: toDateStr,
        groupSlots: groupSlots, renderSlots: renderSlots, renderDoctorCard: renderDoctorCard, renderBooking: renderBooking, initials: initials, photoSrc: photoSrc
    };
})();

$(document).ready(function () {
    var UI = AppointmentUI;
    var cfg = window.AppointmentConfig || {};
    var token = $('input[name="__RequestVerificationToken"]').val();
    var slotRequestId = 0, doctorRequestId = 0;
    var maxDays = cfg.maxAdvanceDays || 90;
    var currentDate = '';

    var select2Lang = AppI18n.select2Language();
    $('#facilityDropdown').select2({ width: '100%', placeholder: t('Js.SelectFacility'), language: select2Lang });
    $('#specialityDropdown').select2({ width: '100%', placeholder: t('Js.SearchSpeciality'), language: select2Lang });
    $('#doctorDropdown').select2({ width: '100%', placeholder: t('Js.SearchDoctor'), language: select2Lang });

    refreshLockState();

    $('#specialityLockWrap, #doctorLockWrap').on('mouseenter', function () {
        if ($(this).find('select').prop('disabled')) showTooltip($(this), $(this).attr('data-tooltip'));
    }).on('mouseleave', hideTooltip);

    buildCarousel();

    // Page load / back button / server re-render: draw the doctor that Razor selected
    if (cfg.initialDoctor) {
        mountDoctor(cfg.initialDoctor, cfg.facilityId, cfg.doctorId, cfg.savedSlotDate, cfg.savedSlotStart);
    }

    /* ── Tooltip & lock state ─────────────────────────── */
    function showTooltip($wrap, message) {
        hideTooltip();
        $wrap.append($('<div class="dropdown-tooltip"></div>').text(message));
    }
    function hideTooltip() { $('.dropdown-tooltip').remove(); }

    function refreshLockState() {
        var facilityId = $('#facilityDropdown').val();
        var specialityId = $('#specialityDropdown').val();
        var doctorId = $('#doctorDropdown').val();

        if (!facilityId) {
            lockDropdown('#specialityDropdown', '#specialityLockWrap', t('Js.SelectFacilityFirst'));
            lockDropdown('#doctorDropdown', '#doctorLockWrap', t('Js.SelectFacilityFirst'));
        } else if (!specialityId) {
            unlockDropdown('#specialityDropdown', '#specialityLockWrap');
            lockDropdown('#doctorDropdown', '#doctorLockWrap', t('Js.SelectSpecialityFirst'));
        } else {
            unlockDropdown('#specialityDropdown', '#specialityLockWrap');
            unlockDropdown('#doctorDropdown', '#doctorLockWrap');
        }
        var current = !facilityId ? 'facility' : !specialityId ? 'speciality' : !doctorId ? 'doctor' : 'slot';
        var order = ['facility', 'speciality', 'doctor', 'slot'];
        $('#welcomeSteps li').each(function () {
            var idx = order.indexOf($(this).data('step'));
            $(this).toggleClass('done', idx < order.indexOf(current)).toggleClass('current', idx === order.indexOf(current));
        });
    }
    function lockDropdown(sel, wrap, msg) {
        $(sel).prop('disabled', true).trigger('change.select2');
        $(wrap).attr('data-tooltip', msg).addClass('is-locked');
    }
    function unlockDropdown(sel, wrap) {
        $(sel).prop('disabled', false).trigger('change.select2');
        $(wrap).removeClass('is-locked');
    }

    /* ── Hospital carousel (skips images that fail to load) ─ */
    function buildCarousel() {
        var images = cfg.hospitalImages || [];
        var loaded = new Array(images.length), pending = images.length;
        if (!pending) return finish();

        images.forEach(function (img, i) {
            var probe = new Image();
            probe.onload = function () { loaded[i] = img; done(); };
            probe.onerror = function () { done(); };
            probe.src = img.src;
        });
        function done() { if (--pending === 0) finish(); }

        function finish() {
            var ok = loaded.filter(Boolean);
            window._carouselImages = ok;
            if (!ok.length) { $('#hospitalCarousel').hide(); $('#welcomeStage').removeClass('has-media'); return; }

            var $track = $('#carouselTrack').empty(), $dots = $('#carouselDots').empty();
            ok.forEach(function (img, i) {
                var $slide = $('<div class="carousel-slide"></div>').append($('<img>').attr({ src: img.src, alt: img.caption || t('Js.HospitalPhoto') }));
                if (img.caption) $slide.append($('<div class="carousel-caption"></div>').text(img.caption));
                $track.append($slide);
                $dots.append($('<button type="button" class="carousel-dot"></button>')
                    .toggleClass('active', i === 0).attr('aria-label', t('Js.Slide', i + 1))
                    .on('click', function () { goToSlide(i); startCarousel(); }));
            });
            window._carouselIndex = 0;
            $('#welcomeStage').addClass('has-media');
            $('#hospitalCarousel').show();
            if ($('#welcomeStage').css('display') !== 'none') startCarousel();
        }
    }
    function goToSlide(index) {
        var n = (window._carouselImages || []).length;
        if (!n) return;
        window._carouselIndex = (index + n) % n;
        $('#carouselTrack').css('transform', 'translateX(-' + (window._carouselIndex * 100) + '%)');
        $('.carousel-dot').removeClass('active').eq(window._carouselIndex).addClass('active');
    }
    function startCarousel() {
        stopCarousel();
        if ((window._carouselImages || []).length < 2) return;
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        window._carouselInterval = setInterval(function () { goToSlide((window._carouselIndex || 0) + 1); }, 3500);
    }
    function stopCarousel() {
        if (window._carouselInterval) { clearInterval(window._carouselInterval); window._carouselInterval = null; }
    }

    /* ── Stage switching ──────────────────────────────── */
    function showWelcome() {
        doctorRequestId++; slotRequestId++;
        if (window._flatpickrInstance) { window._flatpickrInstance.destroy(); window._flatpickrInstance = null; }
        $('#doctorIdentityCard').empty();
        $('#bookingPanel').empty();
        $('#bookingStage').hide();
        $('#hdnSelectedSlotStart').val('');
        $('#hdnSelectedSlotEnd').val('');
        $('#welcomeStage').show();
        startCarousel();
    }
    function showBookingStage() {
        stopCarousel();
        $('#welcomeStage').hide();
        $('#bookingStage').show();
    }

    /* ── Dropdown cascade ─────────────────────────────── */
    $('#facilityDropdown').on('change', function () {
        var facilityId = parseInt($(this).val());
        resetDropdown('#specialityDropdown', t('Js.SearchSpeciality'));
        resetDropdown('#doctorDropdown', t('Js.SearchDoctor'));
        showWelcome();
        AppointmentValidation.clearErrors();
        refreshLockState();
        if (!facilityId) return;

        $.ajax({
            url: '?handler=GetSpecialities', method: 'POST', contentType: 'application/json',
            headers: { 'RequestVerificationToken': token }, data: JSON.stringify(facilityId),
            success: function (res) {
                if (!res.success || parseInt($('#facilityDropdown').val()) !== facilityId) return;
                populateDropdown('#specialityDropdown', res.data, 'id', 'specialisationName', t('Js.SearchSpeciality'));
                refreshLockState();
            }
        });
    });

    $('#specialityDropdown').on('change', function () {
        var facilityId = parseInt($('#facilityDropdown').val());
        var specialityId = parseInt($(this).val());
        resetDropdown('#doctorDropdown', t('Js.SearchDoctor'));
        showWelcome();
        AppointmentValidation.clearErrors();
        refreshLockState();
        if (!facilityId || !specialityId) return;

        $.ajax({
            url: '?handler=GetDoctors', method: 'POST', contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({ facilityId: facilityId, specialityId: specialityId }),
            success: function (res) {
                if (!res.success || parseInt($('#specialityDropdown').val()) !== specialityId) return;
                populateDropdown('#doctorDropdown', res.data, 'id', 'fullName', t('Js.SearchDoctor'));
                refreshLockState();
            }
        });
    });

    $('#doctorDropdown').on('change', function () {
        var facilityId = parseInt($('#facilityDropdown').val());
        var specialityId = parseInt($('#specialityDropdown').val());
        var doctorId = parseInt($(this).val());
        AppointmentValidation.clearErrors();
        refreshLockState();

        if (!facilityId || !specialityId || !doctorId) { showWelcome(); return; }

        var reqId = ++doctorRequestId;
        showBookingStage();
        $('#hdnSelectedSlotStart').val('');
        $('#hdnSelectedSlotEnd').val('');
        $('#doctorIdentityCard').html('<div class="loading-note">' + UI.esc(t('Js.LoadingDoctor')) + '</div>');
        $('#bookingPanel').empty();

        $.ajax({
            url: '?handler=GetDoctorDetails', method: 'POST', contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({ facilityId: facilityId, specialityId: specialityId, doctorId: doctorId }),
            success: function (res) {
                if (reqId !== doctorRequestId) return;
                if (!res.success) { $('#doctorIdentityCard').html('<div class="text-danger">' + UI.esc(t('Js.DoctorLoadFailed')) + '</div>'); return; }
                mountDoctor(res.data, facilityId, doctorId, '', '');
            },
            error: function () {
                if (reqId !== doctorRequestId) return;
                $('#doctorIdentityCard').html('<div class="text-danger">' + UI.esc(t('Js.SomethingWrong')) + '</div>');
            }
        });
    });

    /* ── Render doctor + booking panel ────────────────── */
    function mountDoctor(doc, facilityId, doctorId, savedDate, savedStart) {
        var slots = doc.slots || [];
        var firstSlot = slots.length ? UI.parseSlot(slots[0].startTime) : null;
        var date = savedDate || (firstSlot ? UI.toDateStr(firstSlot) : UI.toDateStr(new Date()));

        $('#doctorIdentityCard').html(UI.renderDoctorCard(doc));
        $('#bookingPanel').html(UI.renderBooking(UI.renderSlots(slots, savedStart, t('Js.NoSlotsForDate')), noteText()));
        showBookingStage();
        setDateLabel(date);

        var saved = savedStart && slots.filter(function (s) { return s.startTime === savedStart; })[0];
        if (saved) { $('#hdnSelectedSlotStart').val(saved.startTime); $('#hdnSelectedSlotEnd').val(saved.endTime); }
        updateSummary();
        initCalendar(doc.hospitalLocationId, facilityId, doctorId, date);
    }

    function setDateLabel(dateStr) {
        var d = UI.parseSlot(dateStr + 'T00:00:00');
        $('#slotDateLabel').text(d ? UI.formatDay(d) : '');
    }

    function updateSummary() {
        var start = $('#hdnSelectedSlotStart').val();
        var d = start ? UI.parseSlot(start) : null;
        $('#selectionSummary').toggleClass('has-slot', !!d);
        $('#selectionValue').text(d ? UI.formatDay(d) + ' · ' + UI.formatTime(d) : t('Js.SelectDateTime'));
    }

    /* ── Slot select & continue ───────────────────────── */
    $(document).on('click', '.slot-btn', function () {
        $('.slot-btn').removeClass('is-selected').attr('aria-pressed', 'false');
        $(this).addClass('is-selected').attr('aria-pressed', 'true');
        $('#hdnSelectedSlotStart').val($(this).attr('data-start'));
        $('#hdnSelectedSlotEnd').val($(this).attr('data-end'));
        AppointmentValidation.clearErrors();
        updateSummary();
    });

   
    $(document).on('click', '#continueBtn', function () {
        var errors = AppointmentValidation.validate();
        if (errors.length) { AppointmentValidation.showErrors(errors); return; }
        AppointmentValidation.clearErrors();
        openPatientTypeDialog();
    });

    /* ── New / already-registered popup ───────────────── */
    function openPatientTypeDialog() {
        $('.choice-card').prop('disabled', false);
        $('#patientTypeDialog').prop('hidden', false);
        $('.choice-card').first().trigger('focus');
    }
    function closePatientTypeDialog() {
        if ($('#patientTypeDialog').prop('hidden')) return;
        $('#patientTypeDialog').prop('hidden', true);
        $('#continueBtn').trigger('focus');
    }
    $('#patientTypeClose').on('click', closePatientTypeDialog);
    $('#patientTypeDialog').on('click', function (e) { if (e.target === this) closePatientTypeDialog(); });

    $('.choice-card').on('click', function () {
        if ($('.choice-card').first().prop('disabled')) return;   // already submitting
        var type = $(this).attr('data-type') === 'registered' ? 'registered' : 'new';
        $('.choice-card').prop('disabled', true);          // block double submit
        $('#hdnPatientType').val(type);
        $('#hdnFacilityId').val($('#facilityDropdown').val());
        $('#hdnSpecialityId').val($('#specialityDropdown').val());
        $('#hdnDoctorId').val($('#doctorDropdown').val());
        $('#appointmentForm').attr('action', '?handler=Continue').submit();
    });

    /* ── Calendar & slots by date ─────────────────────── */
    function startOfToday() { var d = new Date(); d.setHours(0, 0, 0, 0); return d; }
    function lastBookable() { var d = startOfToday(); d.setDate(d.getDate() + maxDays); return d; }
    function windowText() { return maxDays === 90 ? t('Js.ThreeMonths') : t('Js.Days', maxDays); }
    function noteText() { return t('Js.BookingWindowNote', windowText()); }

    function showLimitDialog() {
        var last = lastBookable();
        $('#limitDialogText').text(t('Js.BookingWindowLimit', windowText(), UI.formatDay(last) + ' ' + last.getFullYear()));
        $('#limitDialog').prop('hidden', false);
        $('#limitDialogClose').trigger('focus');
    }
    function hideLimitDialog() { $('#limitDialog').prop('hidden', true); }
    $('#limitDialogClose').on('click', hideLimitDialog);
    $('#limitDialog').on('click', function (e) { if (e.target === this) hideLimitDialog(); });
    $(document).on('keydown', function (e) { if (e.key === 'Escape') { hideLimitDialog(); closePatientTypeDialog(); } });

    function initCalendar(hospitalLocationId, facilityId, doctorId, date) {
        if (window._flatpickrInstance) window._flatpickrInstance.destroy();
        currentDate = date;
        var today = startOfToday(), max = lastBookable(), $year;

        // No minDate: that would trim the month dropdown. Past days are disabled instead.
        window._flatpickrInstance = flatpickr('#slotCalendar', {
            inline: true, dateFormat: 'Y-m-d', defaultDate: date,
            locale: AppI18n.flatpickrLocale(),
            disable: [function (d) { return d < today; }],
            onReady: function (sel, str, fp) {
                var first = Math.min(today.getFullYear(), fp.currentYear);
                $year = $('<select class="fp-year"></select>').attr('aria-label', t('Js.Year'));
                for (var y = first; y <= today.getFullYear() + 5; y++) $year.append($('<option></option>').val(y).text(y));
                $year.val(fp.currentYear).on('change', function () { fp.changeYear(parseInt(this.value)); });
                $(fp.currentYearElement).closest('.flatpickr-current-month').append($year);
            },
            onYearChange: function (sel, str, fp) { if ($year) $year.val(fp.currentYear); },
            onDayCreate: function (sel, str, fp, el) { if (el.dateObj > max) el.classList.add('beyond-window'); },
            onChange: function (selectedDates, dateStr, fp) {
                if (!selectedDates.length) return;
                if (selectedDates[0] > max) {
                    fp.setDate(currentDate, false);   // revert silently, keep the current slots
                    showLimitDialog();
                    return;
                }
                currentDate = dateStr;
                loadSlots(dateStr, hospitalLocationId, facilityId, doctorId);
            }
        });
    }

    function loadSlots(date, hospitalLocationId, facilityId, doctorId) {
        var reqId = ++slotRequestId;
        setDateLabel(date);
        $('#hdnSelectedSlotStart').val('');
        $('#hdnSelectedSlotEnd').val('');
        updateSummary();
        $('#slotsContainer').html('<div class="text-muted">' + UI.esc(t('Js.LoadingSlots')) + '</div>');

        $.get('?handler=SlotsByDate', {
            facilityId: facilityId, doctorId: doctorId, hospitalLocationId: hospitalLocationId, date: date
        }).done(function (data) {
            if (reqId !== slotRequestId) return;
            $('#slotsContainer').html(UI.renderSlots(data, '', t('Js.NoSlotsForDate')));
        }).fail(function () {
            if (reqId !== slotRequestId) return;
            $('#slotsContainer').html('<div class="text-danger">' + UI.esc(t('Js.SlotsLoadFailed')) + '</div>');
        });
    }

    /* ── Dropdown helpers ─────────────────────────────── */
    function resetDropdown(selector, placeholder) {
        $(selector).empty().append($('<option>', { value: '', text: placeholder })).trigger('change.select2');
    }
    function populateDropdown(selector, data, valField, textField, placeholder) {
        var $el = $(selector);
        $el.empty().append($('<option>', { value: '', text: placeholder }));
        $.each(data || [], function (i, item) {
            $el.append($('<option>', { value: item[valField], text: item[textField] || item.name || t('Js.NA') }));
        });
        $el.trigger('change.select2');
    }
});





// /* ── Validation ───────────────────────────────────────── */
// var AppointmentValidation = {
//     rules: {
//         facility: { selector: '#facilityDropdown', message: 'Please select a facility.' },
//         speciality: { selector: '#specialityDropdown', message: 'Please select a speciality.' },
//         doctor: { selector: '#doctorDropdown', message: 'Please select a doctor.' },
//         slot: { selector: '#hdnSelectedSlotStart', message: 'Please select a time slot.' }
//     },
//     validate: function () {
//         var errors = [];
//         $.each(this.rules, function (key, rule) {
//             if (!$(rule.selector).val()) errors.push(rule.message);
//         });
//         return errors;
//     },
//     showErrors: function (errors) {
//         var $box = $('#clientValidationError').empty();
//         if (!errors.length) return;
//         var $ul = $('<ul class="mb-0"></ul>');
//         $.each(errors, function (i, e) { $ul.append($('<li></li>').text(e)); });
//         $box.append($ul);
//         if ($box[0] && $box[0].scrollIntoView) $box[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
//     },
//     clearErrors: function () { $('#clientValidationError').empty(); }
// };

// /* ── Pure helpers (no DOM reads) ──────────────────────── */
// var AppointmentUI = (function () {
//     var DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
//     var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
//     var BUCKETS = ['Morning', 'Afternoon', 'Evening', 'Slots'];

//     function esc(s) {
//         return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
//             return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
//         });
//     }
//     function pad(n) { return n < 10 ? '0' + n : '' + n; }
//     function parseSlot(str) {
//         var d = new Date(String(str || '').replace(' ', 'T'));
//         return isNaN(d.getTime()) ? null : d;
//     }
//     function formatTime(d) {
//         var h = d.getHours();
//         return pad(h % 12 || 12) + ':' + pad(d.getMinutes()) + ' ' + (h < 12 ? 'AM' : 'PM');
//     }
//     function formatDay(d) { return DAYS[d.getDay()] + ', ' + d.getDate() + ' ' + MONTHS[d.getMonth()]; }
//     function toDateStr(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); }
//     function bucketOf(d) {
//         if (!d) return 'Slots';
//         return d.getHours() < 12 ? 'Morning' : d.getHours() < 17 ? 'Afternoon' : 'Evening';
//     }
//     function clean(v) {
//         v = (v == null ? '' : String(v)).trim();
//         return (!v || /^(n\/?a|null|undefined|-+)$/i.test(v)) ? '' : v;
//     }
//     function list(v) {
//         return clean(v).split(',').map(function (x) { return x.trim(); }).filter(Boolean).join(', ');
//     }
//     function initials(name) {
//         return String(name || '').replace(/^dr\.?\s+/i, '').split(/\s+/).filter(Boolean).slice(0, 2)
//             .map(function (w) { return w.charAt(0).toUpperCase(); }).join('') || 'Dr';
//     }
//     // Icon first, then ImageUrl. http images go through the server handler (browsers block/fail http assets from an https site).
//     function photoSrc(doc) {
//         var u = clean(doc.icon) || clean(doc.imageUrl);
//         if (!u) return '';
//         return /^http:\/\//i.test(u) ? '?handler=DoctorImage&url=' + encodeURIComponent(u) : u;
//     }

//     function groupSlots(slots) {
//         var map = {};
//         (slots || []).forEach(function (s) {
//             var d = parseSlot(s.startTime);
//             (map[bucketOf(d)] = map[bucketOf(d)] || []).push({ slot: s, label: d ? formatTime(d) : s.startTime });
//         });
//         return BUCKETS.filter(function (b) { return map[b]; }).map(function (b) { return { name: b, items: map[b] }; });
//     }

//     function renderSlots(slots, selectedStart, emptyMsg) {
//         var groups = groupSlots(slots);
//         if (!groups.length) return '<div class="text-muted">' + esc(emptyMsg || 'No slots available for this date.') + '</div>';
//         return groups.map(function (g) {
//             var btns = g.items.map(function (it) {
//                 var on = selectedStart && it.slot.startTime === selectedStart;
//                 return '<button type="button" class="slot-btn' + (on ? ' is-selected' : '') + '" aria-pressed="' + (on ? 'true' : 'false') +
//                     '" data-start="' + esc(it.slot.startTime) + '" data-end="' + esc(it.slot.endTime) + '">' + esc(it.label) + '</button>';
//             }).join('');
//             return '<div class="slot-group"><div class="slot-group-label">' + g.name +
//                 ' <span class="slot-group-count">' + g.items.length + '</span></div><div class="slot-group-grid">' + btns + '</div></div>';
//         }).join('');
//     }

//     function renderDoctorCard(doc) {
//         var src = photoSrc(doc);
//         var rows = [];
//         if (list(doc.education)) rows.push(['Education', list(doc.education)]);
//         if (clean(doc.experience)) rows.push(['Experience', clean(doc.experience)]);
//         rows.push(['Next available', clean(doc.timing) || 'Not specified']);
//         if (list(doc.treatment)) rows.push(['Treatments', list(doc.treatment)]);
//         if (clean(doc.hospitalName)) rows.push(['Hospital', doc.hospitalName + (clean(doc.hospitalCity) ? ', ' + doc.hospitalCity : '')]);

//         return '<div class="doctor-head">' +
//             '<div class="doctor-avatar-wrap' + (src ? '' : ' no-photo') + '" data-initials="' + esc(initials(doc.name)) + '">' +
//             (src ? '<img class="doctor-avatar" src="' + esc(src) + '" alt="" onerror="this.onerror=null; this.parentNode.classList.add(\'no-photo\'); this.remove();" />' : '') + '</div>' +
//             '<div class="doctor-info"><h3 class="doctor-name">' + esc(doc.name) + '</h3>' +
//             '<p class="doctor-speciality">' + esc(clean(doc.specialisation)) + '</p>' +
//             '<div class="doctor-rating"><span class="rating-badge">★ ' + esc(clean(doc.rating) || 'N/A') + '</span>' +
//             '<span class="review-count">' + esc(clean(doc.review) || '0') + ' reviews</span></div>' +
//             (doc.isTeleconsult ? '<span class="teleconsult-badge">Teleconsultation available</span>' : '') + '</div></div>' +
//             '<div class="fee-block"><span class="fee-label">Consultation fee</span><span class="fee-value">₹ ' + esc(doc.fee) + '</span></div>' +
//             '<dl class="doctor-details">' + rows.map(function (r) {
//                 return '<div class="detail"><dt>' + r[0] + '</dt><dd>' + esc(r[1]) + '</dd></div>';
//             }).join('') + '</dl>';
//     }

//     function renderBooking(slotsHtml, noteText) {
//         return '<div id="selectionSummary" class="selection-banner" aria-live="polite"><span class="selection-label">Your slot</span>' +
//             '<span id="selectionValue" class="selection-value">Select a date and time</span></div>' +
//             '<div class="booking-grid">' +
//             '<div class="calendar-panel"><h5 class="panel-title">Select date</h5>' +
//             '<input type="text" id="slotCalendar" class="form-control" placeholder="Select date" readonly style="display:none;" />' +
//             '<p class="calendar-note">' + esc(noteText || '') + '</p></div>' +
//             '<div class="slots-panel"><div class="slots-head"><h5 class="panel-title">Available slots</h5><span id="slotDateLabel"></span></div>' +
//             '<div id="slotsContainer" class="slots-container">' + slotsHtml + '</div></div></div>' +
//             '<div class="booking-actions"><button type="button" id="continueBtn" class="btn-continue">Continue →</button></div>';
//     }

//     return {
//         esc: esc, parseSlot: parseSlot, formatTime: formatTime, formatDay: formatDay, toDateStr: toDateStr,
//         groupSlots: groupSlots, renderSlots: renderSlots, renderDoctorCard: renderDoctorCard, renderBooking: renderBooking, initials: initials, photoSrc: photoSrc
//     };
// })();

// /* ── Validation ───────────────────────────────────────── */
// var AppointmentValidation = {
//     rules: {
//         facility: { selector: '#facilityDropdown', message: 'Please select a facility.' },
//         speciality: { selector: '#specialityDropdown', message: 'Please select a speciality.' },
//         doctor: { selector: '#doctorDropdown', message: 'Please select a doctor.' },
//         slot: { selector: '#hdnSelectedSlotStart', message: 'Please select a time slot.' }
//     },
//     validate: function () {
//         var errors = [];
//         $.each(this.rules, function (key, rule) {
//             if (!$(rule.selector).val()) errors.push(rule.message);
//         });
//         return errors;
//     },
//     showErrors: function (errors) {
//         var $box = $('#clientValidationError').empty();
//         if (!errors.length) return;
//         var $ul = $('<ul class="mb-0"></ul>');
//         $.each(errors, function (i, e) { $ul.append($('<li></li>').text(e)); });
//         $box.append($ul);
//         if ($box[0] && $box[0].scrollIntoView) $box[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
//     },
//     clearErrors: function () { $('#clientValidationError').empty(); }
// };

// /* ── Pure helpers (no DOM reads) ──────────────────────── */
// var AppointmentUI = (function () {
//     var DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
//     var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
//     var BUCKETS = ['Morning', 'Afternoon', 'Evening', 'Slots'];

//     function esc(s) {
//         return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
//             return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
//         });
//     }
//     function pad(n) { return n < 10 ? '0' + n : '' + n; }
//     function parseSlot(str) {
//         var d = new Date(String(str || '').replace(' ', 'T'));
//         return isNaN(d.getTime()) ? null : d;
//     }
//     function formatTime(d) {
//         var h = d.getHours();
//         return pad(h % 12 || 12) + ':' + pad(d.getMinutes()) + ' ' + (h < 12 ? 'AM' : 'PM');
//     }
//     function formatDay(d) { return DAYS[d.getDay()] + ', ' + d.getDate() + ' ' + MONTHS[d.getMonth()]; }
//     function toDateStr(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); }
//     function bucketOf(d) {
//         if (!d) return 'Slots';
//         return d.getHours() < 12 ? 'Morning' : d.getHours() < 17 ? 'Afternoon' : 'Evening';
//     }
//     function clean(v) {
//         v = (v == null ? '' : String(v)).trim();
//         return (!v || /^(n\/?a|null|undefined|-+)$/i.test(v)) ? '' : v;
//     }
//     function list(v) {
//         return clean(v).split(',').map(function (x) { return x.trim(); }).filter(Boolean).join(', ');
//     }
//     function initials(name) {
//         return String(name || '').replace(/^dr\.?\s+/i, '').split(/\s+/).filter(Boolean).slice(0, 2)
//             .map(function (w) { return w.charAt(0).toUpperCase(); }).join('') || 'Dr';
//     }
//     // Icon first, then ImageUrl. http images go through the server handler (browsers block/fail http assets from an https site).
//     function photoSrc(doc) {
//         var u = clean(doc.icon) || clean(doc.imageUrl);
//         if (!u) return '';
//         return /^http:\/\//i.test(u) ? '?handler=DoctorImage&url=' + encodeURIComponent(u) : u;
//     }

//     function groupSlots(slots) {
//         var map = {};
//         (slots || []).forEach(function (s) {
//             var d = parseSlot(s.startTime);
//             (map[bucketOf(d)] = map[bucketOf(d)] || []).push({ slot: s, label: d ? formatTime(d) : s.startTime });
//         });
//         return BUCKETS.filter(function (b) { return map[b]; }).map(function (b) { return { name: b, items: map[b] }; });
//     }

//     function renderSlots(slots, selectedStart, emptyMsg) {
//         var groups = groupSlots(slots);
//         if (!groups.length) return '<div class="text-muted">' + esc(emptyMsg || 'No slots available for this date.') + '</div>';
//         return groups.map(function (g) {
//             var btns = g.items.map(function (it) {
//                 var on = selectedStart && it.slot.startTime === selectedStart;
//                 return '<button type="button" class="slot-btn' + (on ? ' is-selected' : '') + '" aria-pressed="' + (on ? 'true' : 'false') +
//                     '" data-start="' + esc(it.slot.startTime) + '" data-end="' + esc(it.slot.endTime) + '">' + esc(it.label) + '</button>';
//             }).join('');
//             return '<div class="slot-group"><div class="slot-group-label">' + g.name +
//                 ' <span class="slot-group-count">' + g.items.length + '</span></div><div class="slot-group-grid">' + btns + '</div></div>';
//         }).join('');
//     }

//     function renderDoctorCard(doc) {
//         var src = photoSrc(doc);
//         var rows = [];
//         if (list(doc.education)) rows.push(['Education', list(doc.education)]);
//         if (clean(doc.experience)) rows.push(['Experience', clean(doc.experience)]);
//         rows.push(['Next available', clean(doc.timing) || 'Not specified']);
//         if (list(doc.treatment)) rows.push(['Treatments', list(doc.treatment)]);
//         if (clean(doc.hospitalName)) rows.push(['Hospital', doc.hospitalName + (clean(doc.hospitalCity) ? ', ' + doc.hospitalCity : '')]);

//         return '<div class="doctor-head"><div class="doctor-avatar-col">' +
//             '<div class="doctor-avatar-wrap' + (src ? '' : ' no-photo') + '" data-initials="' + esc(initials(doc.name)) + '">' +
//             (src ? '<img class="doctor-avatar" src="' + esc(src) + '" alt="" onerror="this.onerror=null; this.parentNode.classList.add(\'no-photo\'); this.remove();" />' : '') + '</div>' +
//             '<div class="doctor-rating"><span class="rating-badge">★ ' + esc(clean(doc.rating) || 'N/A') + '</span>' +
//             '<span class="review-count">' + esc(clean(doc.review) || '0') + ' reviews</span></div></div>' +
//             '<div class="doctor-info"><h3 class="doctor-name">' + esc(doc.name) + '</h3>' +
//             '<p class="doctor-speciality">' + esc(clean(doc.specialisation)) + '</p>' +
//             (doc.isTeleconsult ? '<span class="teleconsult-badge">Teleconsultation available</span>' : '') + '</div></div>' +
//             '<div class="fee-block"><span class="fee-label">Consultation fee</span><span class="fee-value">₹ ' + esc(doc.fee) + '</span></div>' +
//             '<dl class="doctor-details">' + rows.map(function (r) {
//                 return '<div class="detail"><dt>' + r[0] + '</dt><dd>' + esc(r[1]) + '</dd></div>';
//             }).join('') + '</dl>';
//     }

//     function renderBooking(slotsHtml, noteText) {
//         return '<div id="selectionSummary" class="selection-banner" aria-live="polite"><span class="selection-label">Your slot</span>' +
//             '<span id="selectionValue" class="selection-value">Select a date and time</span></div>' +
//             '<div class="booking-grid">' +
//             '<div class="calendar-panel"><h5 class="panel-title">Select date</h5>' +
//             '<input type="text" id="slotCalendar" class="form-control" placeholder="Select date" readonly style="display:none;" />' +
//             '<p class="calendar-note">' + esc(noteText || '') + '</p></div>' +
//             '<div class="slots-panel"><div class="slots-head"><h5 class="panel-title">Available slots</h5><span id="slotDateLabel"></span></div>' +
//             '<div id="slotsContainer" class="slots-container">' + slotsHtml + '</div></div></div>' +
//             '<div class="booking-actions"><button type="button" id="continueBtn" class="btn-continue">Continue →</button></div>';
//     }

//     return {
//         esc: esc, parseSlot: parseSlot, formatTime: formatTime, formatDay: formatDay, toDateStr: toDateStr,
//         groupSlots: groupSlots, renderSlots: renderSlots, renderDoctorCard: renderDoctorCard, renderBooking: renderBooking, initials: initials, photoSrc: photoSrc
//     };
// })();

// $(document).ready(function () {
//     var UI = AppointmentUI;
//     var cfg = window.AppointmentConfig || {};
//     var token = $('input[name="__RequestVerificationToken"]').val();
//     var slotRequestId = 0, doctorRequestId = 0;
//     var maxDays = cfg.maxAdvanceDays || 90;
//     var currentDate = '';

//     $('#facilityDropdown').select2({ width: '100%', placeholder: 'Select Facility' });
//     $('#specialityDropdown').select2({ width: '100%', placeholder: 'Search Speciality' });
//     $('#doctorDropdown').select2({ width: '100%', placeholder: 'Search Doctor' });

//     refreshLockState();

//     $('#specialityLockWrap, #doctorLockWrap').on('mouseenter', function () {
//         if ($(this).find('select').prop('disabled')) showTooltip($(this), $(this).attr('data-tooltip'));
//     }).on('mouseleave', hideTooltip);

//     buildCarousel();

//     // Page load / back button / server re-render: draw the doctor that Razor selected
//     if (cfg.initialDoctor) {
//         mountDoctor(cfg.initialDoctor, cfg.facilityId, cfg.doctorId, cfg.savedSlotDate, cfg.savedSlotStart);
//     }

//     /* ── Tooltip & lock state ─────────────────────────── */
//     function showTooltip($wrap, message) {
//         hideTooltip();
//         $wrap.append($('<div class="dropdown-tooltip"></div>').text(message));
//     }
//     function hideTooltip() { $('.dropdown-tooltip').remove(); }

//     function refreshLockState() {
//         var facilityId = $('#facilityDropdown').val();
//         var specialityId = $('#specialityDropdown').val();
//         var doctorId = $('#doctorDropdown').val();

//         if (!facilityId) {
//             lockDropdown('#specialityDropdown', '#specialityLockWrap', 'Please select a facility first');
//             lockDropdown('#doctorDropdown', '#doctorLockWrap', 'Please select a facility first');
//         } else if (!specialityId) {
//             unlockDropdown('#specialityDropdown', '#specialityLockWrap');
//             lockDropdown('#doctorDropdown', '#doctorLockWrap', 'Please select a speciality first');
//         } else {
//             unlockDropdown('#specialityDropdown', '#specialityLockWrap');
//             unlockDropdown('#doctorDropdown', '#doctorLockWrap');
//         }
//         var current = !facilityId ? 'facility' : !specialityId ? 'speciality' : !doctorId ? 'doctor' : 'slot';
//         var order = ['facility', 'speciality', 'doctor', 'slot'];
//         $('#welcomeSteps li').each(function () {
//             var idx = order.indexOf($(this).data('step'));
//             $(this).toggleClass('done', idx < order.indexOf(current)).toggleClass('current', idx === order.indexOf(current));
//         });
//     }
//     function lockDropdown(sel, wrap, msg) {
//         $(sel).prop('disabled', true).trigger('change.select2');
//         $(wrap).attr('data-tooltip', msg).addClass('is-locked');
//     }
//     function unlockDropdown(sel, wrap) {
//         $(sel).prop('disabled', false).trigger('change.select2');
//         $(wrap).removeClass('is-locked');
//     }

//     /* ── Hospital carousel (skips images that fail to load) ─ */
//     function buildCarousel() {
//         var images = cfg.hospitalImages || [];
//         var loaded = new Array(images.length), pending = images.length;
//         if (!pending) return finish();

//         images.forEach(function (img, i) {
//             var probe = new Image();
//             probe.onload = function () { loaded[i] = img; done(); };
//             probe.onerror = function () { done(); };
//             probe.src = img.src;
//         });
//         function done() { if (--pending === 0) finish(); }

//         function finish() {
//             var ok = loaded.filter(Boolean);
//             window._carouselImages = ok;
//             if (!ok.length) { $('#hospitalCarousel').hide(); $('#welcomeStage').removeClass('has-media'); return; }

//             var $track = $('#carouselTrack').empty(), $dots = $('#carouselDots').empty();
//             ok.forEach(function (img, i) {
//                 var $slide = $('<div class="carousel-slide"></div>').append($('<img>').attr({ src: img.src, alt: img.caption || 'Hospital photo' }));
//                 if (img.caption) $slide.append($('<div class="carousel-caption"></div>').text(img.caption));
//                 $track.append($slide);
//                 $dots.append($('<button type="button" class="carousel-dot"></button>')
//                     .toggleClass('active', i === 0).attr('aria-label', 'Slide ' + (i + 1))
//                     .on('click', function () { goToSlide(i); startCarousel(); }));
//             });
//             window._carouselIndex = 0;
//             $('#welcomeStage').addClass('has-media');
//             $('#hospitalCarousel').show();
//             if ($('#welcomeStage').css('display') !== 'none') startCarousel();
//         }
//     }
//     function goToSlide(index) {
//         var n = (window._carouselImages || []).length;
//         if (!n) return;
//         window._carouselIndex = (index + n) % n;
//         $('#carouselTrack').css('transform', 'translateX(-' + (window._carouselIndex * 100) + '%)');
//         $('.carousel-dot').removeClass('active').eq(window._carouselIndex).addClass('active');
//     }
//     function startCarousel() {
//         stopCarousel();
//         if ((window._carouselImages || []).length < 2) return;
//         if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
//         window._carouselInterval = setInterval(function () { goToSlide((window._carouselIndex || 0) + 1); }, 3500);
//     }
//     function stopCarousel() {
//         if (window._carouselInterval) { clearInterval(window._carouselInterval); window._carouselInterval = null; }
//     }

//     /* ── Stage switching ──────────────────────────────── */
//     function showWelcome() {
//         doctorRequestId++; slotRequestId++;
//         if (window._flatpickrInstance) { window._flatpickrInstance.destroy(); window._flatpickrInstance = null; }
//         $('#doctorIdentityCard').empty();
//         $('#bookingPanel').empty();
//         $('#bookingStage').hide();
//         $('#hdnSelectedSlotStart').val('');
//         $('#hdnSelectedSlotEnd').val('');
//         $('#welcomeStage').show();
//         startCarousel();
//     }
//     function showBookingStage() {
//         stopCarousel();
//         $('#welcomeStage').hide();
//         $('#bookingStage').show();
//     }

//     /* ── Dropdown cascade ─────────────────────────────── */
//     $('#facilityDropdown').on('change', function () {
//         var facilityId = parseInt($(this).val());
//         resetDropdown('#specialityDropdown', 'Search Speciality');
//         resetDropdown('#doctorDropdown', 'Search Doctor');
//         showWelcome();
//         AppointmentValidation.clearErrors();
//         refreshLockState();
//         if (!facilityId) return;

//         $.ajax({
//             url: '?handler=GetSpecialities', method: 'POST', contentType: 'application/json',
//             headers: { 'RequestVerificationToken': token }, data: JSON.stringify(facilityId),
//             success: function (res) {
//                 if (!res.success || parseInt($('#facilityDropdown').val()) !== facilityId) return;
//                 populateDropdown('#specialityDropdown', res.data, 'id', 'specialisationName', 'Search Speciality');
//                 refreshLockState();
//             }
//         });
//     });

//     $('#specialityDropdown').on('change', function () {
//         var facilityId = parseInt($('#facilityDropdown').val());
//         var specialityId = parseInt($(this).val());
//         resetDropdown('#doctorDropdown', 'Search Doctor');
//         showWelcome();
//         AppointmentValidation.clearErrors();
//         refreshLockState();
//         if (!facilityId || !specialityId) return;

//         $.ajax({
//             url: '?handler=GetDoctors', method: 'POST', contentType: 'application/json',
//             headers: { 'RequestVerificationToken': token },
//             data: JSON.stringify({ facilityId: facilityId, specialityId: specialityId }),
//             success: function (res) {
//                 if (!res.success || parseInt($('#specialityDropdown').val()) !== specialityId) return;
//                 populateDropdown('#doctorDropdown', res.data, 'id', 'fullName', 'Search Doctor');
//                 refreshLockState();
//             }
//         });
//     });

//     $('#doctorDropdown').on('change', function () {
//         var facilityId = parseInt($('#facilityDropdown').val());
//         var specialityId = parseInt($('#specialityDropdown').val());
//         var doctorId = parseInt($(this).val());
//         AppointmentValidation.clearErrors();
//         refreshLockState();

//         if (!facilityId || !specialityId || !doctorId) { showWelcome(); return; }

//         var reqId = ++doctorRequestId;
//         showBookingStage();
//         $('#hdnSelectedSlotStart').val('');
//         $('#hdnSelectedSlotEnd').val('');
//         $('#doctorIdentityCard').html('<div class="loading-note">Loading doctor details…</div>');
//         $('#bookingPanel').empty();

//         $.ajax({
//             url: '?handler=GetDoctorDetails', method: 'POST', contentType: 'application/json',
//             headers: { 'RequestVerificationToken': token },
//             data: JSON.stringify({ facilityId: facilityId, specialityId: specialityId, doctorId: doctorId }),
//             success: function (res) {
//                 if (reqId !== doctorRequestId) return;
//                 if (!res.success) { $('#doctorIdentityCard').html('<div class="text-danger">Failed to load doctor details.</div>'); return; }
//                 mountDoctor(res.data, facilityId, doctorId, '', '');
//             },
//             error: function () {
//                 if (reqId !== doctorRequestId) return;
//                 $('#doctorIdentityCard').html('<div class="text-danger">Something went wrong. Please try again.</div>');
//             }
//         });
//     });

//     /* ── Render doctor + booking panel ────────────────── */
//     function mountDoctor(doc, facilityId, doctorId, savedDate, savedStart) {
//         var slots = doc.slots || [];
//         var firstSlot = slots.length ? UI.parseSlot(slots[0].startTime) : null;
//         var date = savedDate || (firstSlot ? UI.toDateStr(firstSlot) : UI.toDateStr(new Date()));

//         $('#doctorIdentityCard').html(UI.renderDoctorCard(doc));
//         $('#bookingPanel').html(UI.renderBooking(UI.renderSlots(slots, savedStart, 'No slots available for this date.'), noteText()));
//         showBookingStage();
//         setDateLabel(date);

//         var saved = savedStart && slots.filter(function (s) { return s.startTime === savedStart; })[0];
//         if (saved) { $('#hdnSelectedSlotStart').val(saved.startTime); $('#hdnSelectedSlotEnd').val(saved.endTime); }
//         updateSummary();
//         initCalendar(doc.hospitalLocationId, facilityId, doctorId, date);
//     }

//     function setDateLabel(dateStr) {
//         var d = UI.parseSlot(dateStr + 'T00:00:00');
//         $('#slotDateLabel').text(d ? UI.formatDay(d) : '');
//     }

//     function updateSummary() {
//         var start = $('#hdnSelectedSlotStart').val();
//         var d = start ? UI.parseSlot(start) : null;
//         $('#selectionSummary').toggleClass('has-slot', !!d);
//         $('#selectionValue').text(d ? UI.formatDay(d) + ' · ' + UI.formatTime(d) : 'Select a date and time');
//     }

//     /* ── Slot select & continue ───────────────────────── */
//     $(document).on('click', '.slot-btn', function () {
//         $('.slot-btn').removeClass('is-selected').attr('aria-pressed', 'false');
//         $(this).addClass('is-selected').attr('aria-pressed', 'true');
//         $('#hdnSelectedSlotStart').val($(this).attr('data-start'));
//         $('#hdnSelectedSlotEnd').val($(this).attr('data-end'));
//         AppointmentValidation.clearErrors();
//         updateSummary();
//     });

//     $(document).on('click', '#continueBtn', function () {
//         var errors = AppointmentValidation.validate();
//         if (errors.length) { AppointmentValidation.showErrors(errors); return; }
//         AppointmentValidation.clearErrors();

//         $('#hdnFacilityId').val($('#facilityDropdown').val());
//         $('#hdnSpecialityId').val($('#specialityDropdown').val());
//         $('#hdnDoctorId').val($('#doctorDropdown').val());
//         $('#appointmentForm').attr('action', '?handler=Continue').submit();
//     });

//     /* ── Calendar & slots by date ─────────────────────── */
//     function startOfToday() { var d = new Date(); d.setHours(0, 0, 0, 0); return d; }
//     function lastBookable() { var d = startOfToday(); d.setDate(d.getDate() + maxDays); return d; }
//     function windowText() { return maxDays === 90 ? '3 months' : maxDays + ' days'; }
//     function noteText() { return 'Appointments can be booked up to ' + windowText() + ' in advance.'; }

//     function showLimitDialog() {
//         var last = lastBookable();
//         $('#limitDialogText').text("Appointments can't be booked beyond " + windowText() + " from today. Please choose a date on or before " +
//             UI.formatDay(last) + ' ' + last.getFullYear() + '.');
//         $('#limitDialog').prop('hidden', false);
//         $('#limitDialogClose').trigger('focus');
//     }
//     function hideLimitDialog() { $('#limitDialog').prop('hidden', true); }
//     $('#limitDialogClose').on('click', hideLimitDialog);
//     $('#limitDialog').on('click', function (e) { if (e.target === this) hideLimitDialog(); });
//     $(document).on('keydown', function (e) { if (e.key === 'Escape') hideLimitDialog(); });

//     function initCalendar(hospitalLocationId, facilityId, doctorId, date) {
//         if (window._flatpickrInstance) window._flatpickrInstance.destroy();
//         currentDate = date;
//         var today = startOfToday(), max = lastBookable(), $year;

//         // No minDate: that would trim the month dropdown. Past days are disabled instead.
//         window._flatpickrInstance = flatpickr('#slotCalendar', {
//             inline: true, dateFormat: 'Y-m-d', defaultDate: date,
//             disable: [function (d) { return d < today; }],
//             onReady: function (sel, str, fp) {
//                 var first = Math.min(today.getFullYear(), fp.currentYear);
//                 $year = $('<select class="fp-year" aria-label="Year"></select>');
//                 for (var y = first; y <= today.getFullYear() + 5; y++) $year.append($('<option></option>').val(y).text(y));
//                 $year.val(fp.currentYear).on('change', function () { fp.changeYear(parseInt(this.value)); });
//                 $(fp.currentYearElement).closest('.flatpickr-current-month').append($year);
//             },
//             onYearChange: function (sel, str, fp) { if ($year) $year.val(fp.currentYear); },
//             onDayCreate: function (sel, str, fp, el) { if (el.dateObj > max) el.classList.add('beyond-window'); },
//             onChange: function (selectedDates, dateStr, fp) {
//                 if (!selectedDates.length) return;
//                 if (selectedDates[0] > max) {
//                     fp.setDate(currentDate, false);   // revert silently, keep the current slots
//                     showLimitDialog();
//                     return;
//                 }
//                 currentDate = dateStr;
//                 loadSlots(dateStr, hospitalLocationId, facilityId, doctorId);
//             }
//         });
//     }

//     function loadSlots(date, hospitalLocationId, facilityId, doctorId) {
//         var reqId = ++slotRequestId;
//         setDateLabel(date);
//         $('#hdnSelectedSlotStart').val('');
//         $('#hdnSelectedSlotEnd').val('');
//         updateSummary();
//         $('#slotsContainer').html('<div class="text-muted">Loading slots…</div>');

//         $.get('?handler=SlotsByDate', {
//             facilityId: facilityId, doctorId: doctorId, hospitalLocationId: hospitalLocationId, date: date
//         }).done(function (data) {
//             if (reqId !== slotRequestId) return;
//             $('#slotsContainer').html(UI.renderSlots(data, '', 'No slots available for this date.'));
//         }).fail(function () {
//             if (reqId !== slotRequestId) return;
//             $('#slotsContainer').html('<div class="text-danger">Could not load slots. Please pick the date again.</div>');
//         });
//     }

//     /* ── Dropdown helpers ─────────────────────────────── */
//     function resetDropdown(selector, placeholder) {
//         $(selector).empty().append($('<option>', { value: '', text: placeholder })).trigger('change.select2');
//     }
//     function populateDropdown(selector, data, valField, textField, placeholder) {
//         var $el = $(selector);
//         $el.empty().append($('<option>', { value: '', text: placeholder }));
//         $.each(data || [], function (i, item) {
//             $el.append($('<option>', { value: item[valField], text: item[textField] || item.name || 'N/A' }));
//         });
//         $el.trigger('change.select2');
//     }
// });




