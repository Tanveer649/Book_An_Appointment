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

    // API statusText "Available" → open; "Booked" (or any other status) → isAvailable false. Missing flag = open.
    function isOpen(s) { return !s || s.isAvailable !== false; }
    function hasOpen(slots) { return (slots || []).some(isOpen); }

    function renderSlots(slots, selectedStart, emptyMsg) {
        var groups = groupSlots(slots);
        if (!groups.length) return '<div class="text-muted">' + esc(emptyMsg || t('Js.NoSlotsForDate')) + '</div>';
        // Key under the slots, only when the day has a booked slot
        var legend = (slots || []).some(function (s) { return !isOpen(s); })
            ? '<div class="slot-legend" aria-hidden="true"><span><i class="slot-swatch"></i>' + esc(t('Js.SlotAvailable')) +
              '</span><span><i class="slot-swatch is-booked"></i>' + esc(t('Js.SlotBooked')) + '</span></div>'
            : '';
        // Phone: one group at a time behind Morning / Afternoon / Evening tabs (CSS shows the tabs only on phones).
        // Opens on the group with the chosen slot, else the first group with a free slot.
        var active = 0;
        groups.forEach(function (g, i) {
            if (selectedStart && g.items.some(function (it) { return it.slot.startTime === selectedStart; })) active = i;
        });
        if (!selectedStart || !groups[active].items.some(function (it) { return it.slot.startTime === selectedStart; })) {
            for (var i = 0; i < groups.length; i++) { if (groups[i].items.some(function (it) { return isOpen(it.slot); })) { active = i; break; } }
        }
        var tabs = '<div class="slot-tabs" role="tablist">' + groups.map(function (g, i) {
            var free = g.items.filter(function (it) { return isOpen(it.slot); }).length;
            return '<button type="button" class="slot-tab' + (i === active ? ' is-active' : '') + '" role="tab" aria-selected="' + (i === active) +
                '" data-group="' + i + '">' + esc(t('Js.' + g.name)) + ' <span class="slot-tab-count">' + free + '</span></button>';
        }).join('') + '</div>';

        return tabs + legend + groups.map(function (g, gi) {
            var btns = g.items.map(function (it) {
                if (!isOpen(it.slot)) {   // booked → grey, disabled, not clickable
                    return '<button type="button" class="slot-btn is-booked" disabled aria-disabled="true" title="' + esc(t('Js.SlotBooked')) +
                        '" aria-label="' + esc(it.label + ' – ' + t('Js.SlotBooked')) + '">' + esc(it.label) + '</button>';
                }
                var on = selectedStart && it.slot.startTime === selectedStart;
                return '<button type="button" class="slot-btn' + (on ? ' is-selected' : '') + '" aria-pressed="' + (on ? 'true' : 'false') +
                    '" data-start="' + esc(it.slot.startTime) + '" data-end="' + esc(it.slot.endTime) + '">' + esc(it.label) + '</button>';
            }).join('');
            var free = g.items.filter(function (it) { return isOpen(it.slot); }).length;
            return '<div class="slot-group' + (gi === active ? ' is-active' : '') + '" data-group="' + gi + '"><div class="slot-group-label">' + esc(t('Js.' + g.name)) +
                ' <span class="slot-group-count">' + free + '</span></div><div class="slot-group-grid">' + btns + '</div></div>';
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
            // Phone: details fold away behind "View doctor details" (always open on larger screens — the summary is hidden there)
            '<details class="doctor-more"' + (isPhone() ? '' : ' open') + '><summary>' + esc(t('Js.DoctorDetails')) + '</summary>' +
            '<dl class="doctor-details">' + rows.map(function (r) {
                return '<div class="detail"><dt>' + esc(r[0]) + '</dt><dd>' + esc(r[1]) + '</dd></div>';
            }).join('') + '</dl></details>';
    }

    // Same breakpoints as appointment.css
    function isPhone() { return !!(window.matchMedia && window.matchMedia('(max-width: 767px)').matches); }

    // Tablet / phone: one-tap row of bookable days (the month calendar opens from the "Calendar" button)
    function renderDateStrip(from, to) {
        var html = '', d = new Date(from.getTime());
        while (d <= to) {
            html += '<button type="button" class="ds-day" data-date="' + toDateStr(d) + '">' +
                '<span class="ds-dow">' + esc(DAYS[d.getDay()]) + '</span><span class="ds-num">' + d.getDate() + '</span>' +
                '<span class="ds-mon">' + esc(MONTHS[d.getMonth()]) + '</span></button>';
            d.setDate(d.getDate() + 1);
        }
        return html;
    }

    function renderBooking(slotsHtml, noteText) {
        return '<div id="selectionSummary" class="selection-banner" aria-live="polite"><span class="selection-label">' + esc(t('Js.YourSlot')) + '</span>' +
            '<span id="selectionValue" class="selection-value">' + esc(t('Js.SelectDateTime')) + '</span></div>' +
            '<div class="booking-grid">' +
            '<div class="calendar-panel"><div class="calendar-head"><h5 class="panel-title">' + esc(t('Js.SelectDate')) + '</h5>' +
            '<button type="button" class="cal-toggle" id="calToggle" aria-expanded="false" aria-controls="calWrap">' +
            '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
            '<rect x="3" y="4" width="18" height="18" rx="2"/><path d="M16 2v4M8 2v4M3 10h18"/></svg>' + esc(t('Js.Calendar')) + '</button></div>' +
            '<div class="date-strip" id="dateStrip" role="group" aria-label="' + esc(t('Js.SelectDate')) + '"></div>' +
            '<div class="cal-wrap" id="calWrap">' +
            '<input type="text" id="slotCalendar" class="form-control" placeholder="' + esc(t('Js.SelectDate')) + '" readonly style="display:none;" />' +
            '<p class="calendar-note">' + esc(noteText || '') + '</p></div></div>' +
            '<div class="slots-panel"><div class="slots-head"><h5 class="panel-title">' + esc(t('Js.AvailableSlots')) + '</h5><span id="slotDateLabel"></span></div>' +
            '<div id="slotsContainer" class="slots-container">' + slotsHtml + '</div></div></div>' +
            '<div class="booking-actions"><span class="action-slot" id="actionSlot" aria-hidden="true"></span>' +
            '<button type="button" id="continueBtn" class="btn-continue">' + esc(t('Js.Continue')) + '</button></div>';
    }

    return {
        esc: esc, parseSlot: parseSlot, formatTime: formatTime, formatDay: formatDay, toDateStr: toDateStr,
        groupSlots: groupSlots, renderSlots: renderSlots, isOpen: isOpen, hasOpen: hasOpen, renderDateStrip: renderDateStrip, isPhone: isPhone, renderDoctorCard: renderDoctorCard, renderBooking: renderBooking, initials: initials, photoSrc: photoSrc
    };
})();

$(document).ready(function () {
    var UI = AppointmentUI;
    var cfg = window.AppointmentConfig || {};
    var token = $('input[name="__RequestVerificationToken"]').val();
    var slotRequestId = 0, doctorRequestId = 0;
    var maxDays = cfg.maxAdvanceDays || 90;
    var currentDate = '';

    // After a failed Continue post the address bar shows ?handler=Continue — keep the clean URL
    if (window.history && window.history.replaceState && /[?&]handler=/i.test(location.search)) {
        history.replaceState(null, '', location.pathname);
    }

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
        } else {
            // Facility chosen → speciality AND doctor are open: the patient may start with either one
            unlockDropdown('#specialityDropdown', '#specialityLockWrap');
            unlockDropdown('#doctorDropdown', '#doctorLockWrap');
        }
        // 'Start over' only makes sense once something beyond the default facility is chosen
        $('#startOverBtn').prop('hidden', !specialityId && !doctorId);

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
    // Tablet / phone (≤ 991 px, same as appointment.css): no carousel — keeps the first page short and saves mobile data
    function carouselOff() { return !!(window.matchMedia && window.matchMedia('(max-width: 991px)').matches); }

    function buildCarousel() {
        if (carouselOff()) { $('#hospitalCarousel').hide(); $('#welcomeStage').removeClass('has-media'); return; }
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
        if ((window._carouselImages || []).length < 2 || carouselOff()) return;
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
        loadDoctors(facilityId, 0);       // every doctor of the facility — a doctor can be chosen before the speciality
    });

    // Speciality chosen → only its doctors; speciality cleared → every doctor of the facility again
    $('#specialityDropdown').on('change', function () {
        var facilityId = parseInt($('#facilityDropdown').val());
        var specialityId = parseInt($(this).val()) || 0;
        resetDropdown('#doctorDropdown', t('Js.SearchDoctor'));
        showWelcome();
        AppointmentValidation.clearErrors();
        refreshLockState();
        if (!facilityId) return;
        loadDoctors(facilityId, specialityId);
    });

    // Doctor list (GetDoctorList: specialityId 0 = all). keepDoctorId re-selects a doctor after the list changes.
    var doctorListRequestId = 0;
    function loadDoctors(facilityId, specialityId, keepDoctorId) {
        var reqId = ++doctorListRequestId;
        $.ajax({
            url: '?handler=GetDoctors', method: 'POST', contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({ facilityId: facilityId, specialityId: specialityId }),
            success: function (res) {
                if (!res.success || reqId !== doctorListRequestId || parseInt($('#facilityDropdown').val()) !== facilityId) return;
                populateDoctors(res.data);
                if (keepDoctorId) $('#doctorDropdown').val(String(keepDoctorId)).trigger('change.select2');
                refreshLockState();
            }
        });
    }

    $('#doctorDropdown').on('change', function () {
        var facilityId = parseInt($('#facilityDropdown').val());
        var doctorId = parseInt($(this).val());

        // Doctor picked before (or outside) the speciality → select the doctor's speciality and narrow the doctor
        // list to that speciality, keeping this doctor selected. 'change.select2' only redraws the dropdown, it does
        // not run the speciality handler above (which would clear the doctor).
        var doctorSpeciality = parseInt($(this).find('option:selected').attr('data-speciality')) || 0;
        if (doctorId && doctorSpeciality && parseInt($('#specialityDropdown').val()) !== doctorSpeciality &&
            $('#specialityDropdown option[value="' + doctorSpeciality + '"]').length) {
            $('#specialityDropdown').val(String(doctorSpeciality)).trigger('change.select2');
            loadDoctors(facilityId, doctorSpeciality, doctorId);
        }

        var specialityId = parseInt($('#specialityDropdown').val()) || doctorSpeciality;
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

        var saved = savedStart && slots.filter(function (s) { return s.startTime === savedStart && UI.isOpen(s); })[0];
        if (saved) { $('#hdnSelectedSlotStart').val(saved.startTime); $('#hdnSelectedSlotEnd').val(saved.endTime); }
        updateSummary();
        initCalendar(doc.hospitalLocationId, facilityId, doctorId, date);
        if (!UI.hasOpen(slots)) showEmptyDay(date, facilityId, doctorId, doc.hospitalLocationId);
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
        // Tablet / phone: the floating Continue bar repeats the chosen slot
        $('#actionSlot').toggleClass('has-slot', !!d).empty().append(d
            ? [$('<small></small>').text(UI.formatDay(d)), $('<strong></strong>').text(UI.formatTime(d))]
            : $('<strong></strong>').text(t('Js.PickTime')));
    }

    /* ── Tablet / phone: date strip, calendar toggle, slot tabs ── */
    function buildDateStrip(selected) {
        var $strip = $('#dateStrip');
        if (!$strip.length) return;
        $strip.html(UI.renderDateStrip(startOfToday(), lastBookable()));
        syncDateStrip(selected);
    }

    // Highlight the chosen day and scroll the strip (not the page) so it is in view
    function syncDateStrip(dateStr) {
        var $strip = $('#dateStrip');
        $strip.find('.ds-day').removeClass('is-selected').attr('aria-pressed', 'false');
        var $day = $strip.find('.ds-day[data-date="' + dateStr + '"]').addClass('is-selected').attr('aria-pressed', 'true');
        if ($day.length && $strip[0].clientWidth) {
            var el = $day[0], box = $strip[0];
            box.scrollLeft = Math.max(0, el.offsetLeft - box.offsetLeft - (box.clientWidth - el.offsetWidth) / 2);
        }
    }

    function setCalendarOpen(open) {
        $('#bookingPanel').toggleClass('calendar-open', open);
        $('#calToggle').attr('aria-expanded', open ? 'true' : 'false');
    }

    $(document).on('click', '.ds-day', function () {
        var fp = window._flatpickrInstance, date = $(this).attr('data-date');
        if (!fp || date === currentDate) return;
        fp.setDate(date, true);      // same path as picking the day in the calendar (onChange loads the slots)
    });

    $(document).on('click', '#calToggle', function () {
        setCalendarOpen(!$('#bookingPanel').hasClass('calendar-open'));
    });

    $(document).on('click', '.slot-tab', function () {
        var g = $(this).attr('data-group');
        $('.slot-tab').removeClass('is-active').attr('aria-selected', 'false');
        $(this).addClass('is-active').attr('aria-selected', 'true');
        $('.slot-group').removeClass('is-active').filter('[data-group="' + g + '"]').addClass('is-active');
    });

    // Doctor details: folded on phones, always open on larger screens
    if (window.matchMedia) {
        var phoneMq = window.matchMedia('(max-width: 767px)');
        var onPhoneChange = function () { if (!phoneMq.matches) $('.doctor-more').attr('open', ''); };
        if (phoneMq.addEventListener) phoneMq.addEventListener('change', onPhoneChange); else if (phoneMq.addListener) phoneMq.addListener(onPhoneChange);
    }

    /* ── Slot select & continue ───────────────────────── */
    $(document).on('click', '.slot-btn', function () {
        $('.slot-btn').removeClass('is-selected').attr('aria-pressed', 'false');
        $(this).addClass('is-selected').attr('aria-pressed', 'true');
        $('#hdnSelectedSlotStart').val($(this).attr('data-start'));
        $('#hdnSelectedSlotEnd').val($(this).attr('data-end'));
        AppointmentValidation.clearErrors();
        updateSummary();
        rememberSlot(currentDate, $(this).attr('data-start'));
    });

    // Keep the picked date / slot in the session so a page reload restores it (Continue re-validates it)
    function rememberSlot(date, start) {
        if (!date) return;
        $.ajax({
            url: '?handler=RememberSlot', method: 'POST', contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({ date: date, start: start || '' })
        });
    }

   
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
    $(document).on('keydown', function (e) { if (e.key === 'Escape') { hideLimitDialog(); closePatientTypeDialog(); hideStartOver(); } });

    /* ── Start over (confirmation → ?handler=StartOver clears this page's choices) ── */
    function hideStartOver() {
        if ($('#startOverDialog').prop('hidden')) return;
        $('#startOverDialog').prop('hidden', true);
        $('#startOverBtn').trigger('focus');
    }
    $('#startOverBtn').on('click', function () {
        $('#startOverDialog').prop('hidden', false);
        $('#startOverCancel').trigger('focus');
    });
    $('#startOverCancel').on('click', hideStartOver);
    $('#startOverDialog').on('click', function (e) { if (e.target === this) hideStartOver(); });

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
                buildDateStrip(date);
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
                syncDateStrip(dateStr);
                setCalendarOpen(false);
                rememberSlot(dateStr, '');
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
            if (!UI.hasOpen(data)) showEmptyDay(date, facilityId, doctorId, hospitalLocationId);
        }).fail(function () {
            if (reqId !== slotRequestId) return;
            $('#slotsContainer').html('<div class="text-danger">' + UI.esc(t('Js.SlotsLoadFailed')) + '</div>');
        });
    }

    /* ── Day without free slots → next free day (appsettings AppointmentPageSettings:EnableNextAvailable) ── */
    var calendarOffIcon = '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" ' +
        'stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9 5h9a2 2 0 0 1 2 2v9M20 20H6a2 2 0 0 1-2-2V7' +
        'a2 2 0 0 1 1-1.7M16 3v4M8 3v1M4 11h7M15 11h5M3 3l18 18"/></svg>';

    function emptyDayHtml(title, sub, goDate, goLabel) {
        return '<div class="empty-day" role="status"><div class="empty-day-icon">' + calendarOffIcon + '</div>' +
            '<div class="empty-day-title">' + UI.esc(title) + '</div>' +
            (sub ? '<div class="empty-day-sub">' + UI.esc(sub) + '</div>' : '') +
            (goDate ? '<button type="button" class="empty-day-btn" data-go-day="' + UI.esc(goDate) + '">' + UI.esc(goLabel) + '</button>' : '') +
            '</div>';
    }

    function dayLabel(dateStr) {
        var d = UI.parseSlot(dateStr + 'T00:00:00');
        return d ? UI.formatDay(d) : dateStr;
    }

    function showEmptyDay(dateStr, facilityId, doctorId, hospitalLocationId) {
        if (!cfg.nextAvailableEnabled) return;                       // switched off → plain "No slots available" text
        var reqId = slotRequestId;                                   // a newer date / doctor choice wins
        var title = t('Js.NoSlotsOn', dayLabel(dateStr));
        $('#slotsContainer').html(emptyDayHtml(title, t('Js.FindingNextFreeDay')));

        $.get('?handler=NextFreeDay', {
            facilityId: facilityId, doctorId: doctorId, hospitalLocationId: hospitalLocationId, after: dateStr
        }).done(function (res) {
            if (reqId !== slotRequestId) return;
            if (res && res.date) {
                var next = dayLabel(res.date);
                $('#slotsContainer').html(emptyDayHtml(title, t('Js.NextFreeDay', next), res.date, t('Js.GoToDay', next)));
            } else {
                $('#slotsContainer').html(emptyDayHtml(title, t('Js.ChooseAnotherDate')));
            }
        }).fail(function () {
            if (reqId !== slotRequestId) return;
            $('#slotsContainer').html(emptyDayHtml(title, t('Js.ChooseAnotherDate')));
        });
    }

    // "Go to …" → same as picking that day in the calendar (moves the calendar, loads that day's slots)
    $(document).on('click', '[data-go-day]', function () {
        if (window._flatpickrInstance) window._flatpickrInstance.setDate($(this).attr('data-go-day'), true);
    });

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
    // Doctor options carry their speciality (data-speciality) so choosing a doctor first can fill the speciality
    function populateDoctors(doctors) {
        var $el = $('#doctorDropdown');
        $el.empty().append($('<option>', { value: '', text: t('Js.SearchDoctor') }));
        $.each(doctors || [], function (i, d) {
            $el.append($('<option>', { value: d.id, text: d.fullName || t('Js.NA'), 'data-speciality': d.specialisationId || '' }));
        });
        $el.trigger('change.select2');
    }
});
