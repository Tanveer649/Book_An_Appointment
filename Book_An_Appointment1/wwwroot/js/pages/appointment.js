var AppointmentValidation = {

    rules: {
        facility: {
            selector: '#facilityDropdown',
            message: 'Please select a facility.'
        },
        speciality: {
            selector: '#specialityDropdown',
            message: 'Please select a speciality.'
        },
        doctor: {
            selector: '#doctorDropdown',
            message: 'Please select a doctor.'
        },
        slot: {
            selector: '#hdnSelectedSlotStart',
            message: 'Please select a time slot.'
        }
    },

    validate: function () {
        var errors = [];
        $.each(this.rules, function (key, rule) {
            if (!$(rule.selector).val()) {
                errors.push(rule.message);
            }
        });
        return errors;
    },

    showErrors: function (errors) {
        $('#clientErrors').remove();
        if (errors.length === 0) return;

        var html = '<div id="clientErrors" class="alert alert-danger mt-3"><ul class="mb-0">';
        $.each(errors, function (i, e) {
            html += '<li>' + e + '</li>';
        });
        html += '</ul></div>';

        $('.wizard-track').after(html);
        $('html, body').animate({ scrollTop: 0 }, 300);
    },

    clearErrors: function () {
        $('#clientErrors').remove();
    }
};

$(document).ready(function () {

    var token = $('input[name="__RequestVerificationToken"]').val();

    // ── Select2 Initialize ───────────────────────────────
    $('#facilityDropdown').select2({ width: '100%', placeholder: 'Select Facility' });
    $('#specialityDropdown').select2({ width: '100%', placeholder: 'Search Speciality' });
    $('#doctorDropdown').select2({ width: '100%', placeholder: 'Search Doctor' });

    // ── Initial Lock State (based on what's already selected on load) ──
    refreshLockState();

    // ── Tooltip on disabled dropdown hover ───────────────
    $('#specialityLockWrap, #doctorLockWrap').on('mouseenter', function () {
        var $select = $(this).find('select');
        if ($select.prop('disabled')) {
            showTooltip($(this), $(this).attr('data-tooltip'));
        }
    }).on('mouseleave', function () {
        hideTooltip();
    });



    // ── Back button case: Razor ne already card render kiya hai ──
    // Fresh select case mein #slotCalendar tab nahi hota DOM mein
    if ($('#slotCalendar').length > 0) {
        var savedDate = window.AppointmentConfig.savedSlotDate || 'today';
        var savedStart = window.AppointmentConfig.savedSlotStart || '';

        // Calendar initialize karo
        initCalendar(
            window.AppointmentConfig.hospitalLocationId,
            window.AppointmentConfig.facilityId,
            window.AppointmentConfig.doctorId
        );

        // Saved slot highlight karo
        if (savedStart) {
            $('#hdnSelectedSlotStart').val(savedStart);
            $('.slot-btn').each(function () {
                if ($(this).data('start') === savedStart) {
                    $(this).removeClass('btn-outline-primary')
                        .addClass('btn-primary');
                }
            });
        }
    }
    function showTooltip($wrap, message) {
        $('.dropdown-tooltip').remove();
        var tip = $('<div class="dropdown-tooltip">' + message + '</div>');
        $wrap.append(tip);
    }

    function hideTooltip() {
        $('.dropdown-tooltip').remove();
    }

    // ── Lock/Unlock logic ─────────────────────────────────
    function refreshLockState() {
        var facilityId = $('#facilityDropdown').val();
        var specialityId = $('#specialityDropdown').val();

        if (!facilityId) {
            lockDropdown('#specialityDropdown', '#specialityLockWrap', 'Please select a facility first');
            lockDropdown('#doctorDropdown', '#doctorLockWrap', 'Please select a facility first');
        } else if (!specialityId) {
            unlockDropdown('#specialityDropdown', '#specialityLockWrap');
            lockDropdown('#doctorDropdown', '#doctorLockWrap', 'Please select a speciality first');
        } else {
            unlockDropdown('#specialityDropdown', '#specialityLockWrap');
            unlockDropdown('#doctorDropdown', '#doctorLockWrap');
        }
    }

    function lockDropdown(selector, wrapSelector, tooltipMsg) {
        $(selector).prop('disabled', true).trigger('change.select2');
        $(wrapSelector).attr('data-tooltip', tooltipMsg).addClass('is-locked');
    }

    function unlockDropdown(selector, wrapSelector) {
        $(selector).prop('disabled', false).trigger('change.select2');
        $(wrapSelector).removeClass('is-locked');
    }

    // ── Facility Change → Load Specialities ──────────────
    $('#facilityDropdown').on('change', function () {
        var facilityId = parseInt($(this).val());

        resetDropdown('#specialityDropdown', 'Search Speciality');
        resetDropdown('#doctorDropdown', 'Search Doctor');
        $('#doctorDetailsCard').hide().html('');
        AppointmentValidation.clearErrors();

        refreshLockState(); // pehle lock karo

        if (!facilityId) return;

        $.ajax({
            url: '?handler=GetSpecialities',
            method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify(facilityId),
            success: function (res) {
                if (!res.success) return;
                populateDropdown(
                    '#specialityDropdown', res.data,
                    'id', 'specialisationName', 'Search Speciality');
                refreshLockState(); // ← YAHAN UNCOMMENT/ADD KARO ✅
            }
        });
    });

    // ── Speciality Change → Load Doctors ─────────────────
    $('#specialityDropdown').on('change', function () {
        var facilityId = parseInt($('#facilityDropdown').val());
        var specialityId = parseInt($(this).val());

        resetDropdown('#doctorDropdown', 'Search Doctor');
        $('#doctorDetailsCard').hide().html('');
        AppointmentValidation.clearErrors();

        refreshLockState();

        if (!facilityId || !specialityId) return;

        $.ajax({
            url: '?handler=GetDoctors',
            method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({
                facilityId: facilityId,
                specialityId: specialityId
            }),
            success: function (res) {
                if (!res.success) return;
                populateDropdown(
                    '#doctorDropdown', res.data,
                    'id', 'fullName', 'Search Doctor');
                refreshLockState();
            }
        });
    });

    // ── Doctor Change → Load Doctor Details ──────────────
    $('#doctorDropdown').on('change', function () {
        var facilityId = parseInt($('#facilityDropdown').val());
        var specialityId = parseInt($('#specialityDropdown').val());
        var doctorId = parseInt($(this).val());

        $('#doctorDetailsCard').hide().html('');
        AppointmentValidation.clearErrors();

        if (!facilityId || !specialityId || !doctorId) return;

        $('#doctorDetailsCard')
            .show()
            .html('<div class="text-muted p-3">Loading doctor details...</div>');

        $.ajax({
            url: '?handler=GetDoctorDetails',
            method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({
                facilityId: facilityId,
                specialityId: specialityId,
                doctorId: doctorId
            }),
            success: function (res) {
                if (!res.success) {
                    $('#doctorDetailsCard').html(
                        '<div class="text-danger p-3">Failed to load doctor details.</div>');
                    return;
                }
                renderDoctorCard(res.data, facilityId, doctorId);
            },
            error: function () {
                $('#doctorDetailsCard').html(
                    '<div class="text-danger p-3">Something went wrong.</div>');
            }
        });
    });

    // ── Slot Select ───────────────────────────────────────
    $(document).on('click', '.slot-btn', function () {
        $('.slot-btn').removeClass('btn-primary').addClass('btn-outline-primary');
        $(this).removeClass('btn-outline-primary').addClass('btn-primary');
        $('#hdnSelectedSlotStart').val($(this).data('start'));
        $('#hdnSelectedSlotEnd').val($(this).data('end'));
        AppointmentValidation.clearErrors();
    });

    // ── Continue Button ───────────────────────────────────
    $(document).on('click', '#continueBtn', function () {
        var errors = AppointmentValidation.validate();
        if (errors.length > 0) {
            AppointmentValidation.showErrors(errors);
            return;
        }
        AppointmentValidation.clearErrors();

        // Hidden fields update karo before submit
        $('#hdnFacilityId').val($('#facilityDropdown').val());
        $('#hdnSpecialityId').val($('#specialityDropdown').val());
        $('#hdnDoctorId').val($('#doctorDropdown').val());

        $('#appointmentForm')
            .attr('action', '?handler=Continue')
            .submit();
    });

    function toHttps(url) {
        if (!url) return '/Image/default-doctor.png';
        return url.replace('http://', 'https://');
    }
    // ── Render Doctor Card ────────────────────────────────
    function renderDoctorCard(doc, facilityId, doctorId) {
        var html = `
        <div class="row">
            <div class="col-md-3 text-center">
                <img src="${toHttps(doc.imageUrl)}"
                     style="width:180px;height:180px;object-fit:cover;border-radius:50%;"
                     onerror="this.onerror=null; this.src='/Image/default-doctor.png';" />
                <div class="mt-2">
                    <span class="badge bg-warning text-dark">
                        ⭐ ${doc.rating || 'N/A'}
                    </span>
                    <span class="text-muted ms-1" style="font-size:12px;">
                        (${doc.review || '0'} reviews)
                    </span>
                </div>
                ${doc.isTeleconsult
                ? `<div class="mt-2">
                           <span class="badge bg-success">Teleconsultation Available</span>
                       </div>`
                : ''}
            </div>
            <div class="col-md-9">
                <h3>${doc.name}</h3>
                <p class="text-muted mb-3">${doc.specialisation}</p>
                <div class="row g-2">
                    <div class="col-md-6">
                        <strong>Experience:</strong>
                        <span>${doc.experience}</span>
                    </div>
                    <div class="col-md-6">
                        <strong>Consultation Fee:</strong>
                        <span class="text-success fw-bold">₹ ${doc.fee}</span>
                    </div>
                    <div class="col-md-6">
                        <strong>Next Available:</strong>
                        <span>${doc.timing}</span>
                    </div>
                    ${doc.treatment
                ? `<div class="col-md-12">
                               <strong>Treatments:</strong>
                               <span>${doc.treatment}</span>
                           </div>`
                : ''}
                    ${doc.hospitalName
                ? `<div class="col-md-12">
                               <strong>Hospital:</strong>
                               <span>${doc.hospitalName}, ${doc.hospitalCity}</span>
                           </div>`
                : ''}
                </div>
            </div>
        </div>

        <hr class="my-4" />

        <div class="row g-4">
            <div class="col-md-4">
                <h5 class="mb-3">Select Date</h5>
                <input type="text" id="slotCalendar"
                       class="form-control"
                       placeholder="Select date"
                       readonly 
                       style="display:none;" />
            </div>
            <div class="col-md-8">
                <h5 class="mb-3">
                    Available Slots
                    <span id="slotDateLabel" class="text-muted fs-6 ms-2"></span>
                </h5>
                <div id="slotsContainer" class="d-flex flex-wrap gap-2">
                    ${renderSlots(doc.slots)}
                </div>
            </div>
        </div>

        <input type="hidden" id="hdnHospitalLocationId" value="${doc.hospitalLocationId}" />
        <input type="hidden" id="hdnDoctorIdForSlot"    value="${doctorId}" />
        <input type="hidden" id="hdnFacilityIdForSlot"  value="${facilityId}" />

        <div class="text-end mt-5">
            <button type="button" id="continueBtn"
                    class="btn btn-primary px-5 py-3 rounded-pill">
                Continue →
            </button>
        </div>`;

        $('#doctorDetailsCard').html(html);
        initCalendar(doc.hospitalLocationId, facilityId, doctorId);
    }

    // ── Render Slots ──────────────────────────────────────
    function renderSlots(slots) {
        if (!slots || slots.length === 0)
            return '<div class="text-muted">No slots available for today.</div>';

        var savedStart = window.AppointmentConfig.savedSlotStart;

        return slots.map(function (slot) {
            var isSelected = savedStart && slot.startTime === savedStart;

            // ── Saved slot ke hidden fields restore karo ──
            if (isSelected) {
                $('#hdnSelectedSlotStart').val(slot.startTime);
                $('#hdnSelectedSlotEnd').val(slot.endTime);
            }

            var parsed = new Date(slot.startTime.replace(' ', 'T'));
            var isValid = !isNaN(parsed.getTime());
            var timePart = isValid
                ? parsed.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
                : slot.startTime;
            var datePart = isValid
                ? parsed.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
                : '';

            return `<button type="button"
                    class="btn ${isSelected ? 'btn-primary' : 'btn-outline-primary'} rounded-pill px-3 py-1 slot-btn"
                    style="font-size:13px;"
                    data-start="${slot.startTime}"
                    data-end="${slot.endTime}">
                    <div>${timePart}</div>
                    <div style="font-size:10px;color:#888;">${datePart}</div>
                </button>`;
        }).join('');
    }

    // ── Calendar ──────────────────────────────────────────
    function initCalendar(hospitalLocationId, facilityId, doctorId) {
        if (window._flatpickrInstance)
            window._flatpickrInstance.destroy();

        var savedDate = window.AppointmentConfig.savedSlotDate || 'today';

        window._flatpickrInstance = flatpickr('#slotCalendar', {
            inline: true,        // ← hamesha open rahega
            minDate: 'today',
            dateFormat: 'Y-m-d',
            defaultDate: savedDate,
            onChange: function (selectedDates, dateStr) {
                loadSlots(dateStr, hospitalLocationId, facilityId, doctorId);

                var today = new Date().toISOString().split('T')[0];
                var dateToLoad = (savedDate && savedDate !== 'today' && savedDate !== today);
                    loadSlots(savedDate, hospitalLocationId, facilityId, doctorId);
            }
        });
    }
    // ── Load Slots by Date ────────────────────────────────
    function loadSlots(date, hospitalLocationId, facilityId, doctorId) {
        $('#slotsContainer').html(
            '<div class="text-muted">Loading slots...</div>');
        $('#slotDateLabel').text('— ' + date);
        $('#hdnSelectedSlotStart').val('');
        $('#hdnSelectedSlotEnd').val('');

        $.get('?handler=SlotsByDate', {
            facilityId: facilityId,
            doctorId: doctorId,
            hospitalLocationId: hospitalLocationId,
            date: date
        }, function (data) {
            $('#slotsContainer').html(
                data && data.length > 0
                    ? renderSlots(data)
                    : '<div class="text-muted">No slots available for this date.</div>'
            );
        });
    }

    // ── Helpers ───────────────────────────────────────────
    function resetDropdown(selector, placeholder) {
        $(selector)
            .empty()
            .append($('<option>', { value: '', text: placeholder }))
            .trigger('change.select2');
    }

    function populateDropdown(selector, data, valField, textField, placeholder) {
        var $el = $(selector);
        $el.empty().append($('<option>', { value: '', text: placeholder }));
        $.each(data, function (i, item) {
            $el.append($('<option>', {
                value: item[valField],
                text: item[textField] || 'N/A'
            }));
        });
        // Select2 ko UI update ke liye notify karo bina change event fire kiye
        if ($el.data('select2')) {
            $el[0].dispatchEvent(new Event('change.select2'));
        }
    }
});




//$(document).ready(function () {

//    var token = $('input[name="__RequestVerificationToken"]').val();

//    // ── Select2 Initialize ───────────────────────────────
//    $('#facilityDropdown').select2({ width: '100%', placeholder: 'Select Facility' });
//    $('#specialityDropdown').select2({ width: '100%', placeholder: 'Search Speciality' });
//    $('#doctorDropdown').select2({ width: '100%', placeholder: 'Search Doctor' });

//    // ── Facility Change → Load Specialities ──────────────
//    $('#facilityDropdown').on('change', function () {
//        var facilityId = parseInt($(this).val());

//        resetDropdown('#specialityDropdown', 'Search Speciality');
//        resetDropdown('#doctorDropdown', 'Search Doctor');
//        $('#doctorDetailsCard').hide().html('');

//        if (!facilityId) return;

//        $.ajax({
//            url: '?handler=GetSpecialities',
//            method: 'POST',
//            contentType: 'application/json',
//            headers: { 'RequestVerificationToken': token },
//            data: JSON.stringify(facilityId),
//            success: function (res) {
//                if (!res.success) return;
//                populateDropdown(
//                    '#specialityDropdown', res.data,
//                    'id', 'specialisationName', 'Search Speciality');
//            }
//        });
//    });

//    // ── Speciality Change → Load Doctors ─────────────────
//    $('#specialityDropdown').on('change', function () {
//        var facilityId = parseInt($('#facilityDropdown').val());
//        var specialityId = parseInt($(this).val());

//        resetDropdown('#doctorDropdown', 'Search Doctor');
//        $('#doctorDetailsCard').hide().html('');

//        if (!facilityId || !specialityId) return;

//        $.ajax({
//            url: '?handler=GetDoctors',
//            method: 'POST',
//            contentType: 'application/json',
//            headers: { 'RequestVerificationToken': token },
//            data: JSON.stringify({
//                facilityId: facilityId,
//                specialityId: specialityId
//            }),
//            success: function (res) {
//                if (!res.success) return;
//                populateDropdown(
//                    '#doctorDropdown', res.data,
//                    'id', 'fullName', 'Search Doctor');
//            }
//        });
//    });

//    // ── Doctor Change → Load Doctor Details ──────────────
//    $('#doctorDropdown').on('change', function () {
//        var facilityId = parseInt($('#facilityDropdown').val());
//        var specialityId = parseInt($('#specialityDropdown').val());
//        var doctorId = parseInt($(this).val());

//        $('#doctorDetailsCard').hide().html('');

//        if (!facilityId || !specialityId || !doctorId) return;

//        $('#doctorDetailsCard')
//            .show()
//            .html('<div class="text-muted p-3">Loading doctor details...</div>');

//        $.ajax({
//            url: '?handler=GetDoctorDetails',
//            method: 'POST',
//            contentType: 'application/json',
//            headers: { 'RequestVerificationToken': token },
//            data: JSON.stringify({
//                facilityId: facilityId,
//                specialityId: specialityId,
//                doctorId: doctorId
//            }),
//            success: function (res) {
//                if (!res.success) {
//                    $('#doctorDetailsCard').html(
//                        '<div class="text-danger p-3">Failed to load doctor details.</div>');
//                    return;
//                }
//                renderDoctorCard(res.data, facilityId, doctorId);
//            },
//            error: function () {
//                $('#doctorDetailsCard').html(
//                    '<div class="text-danger p-3">Something went wrong.</div>');
//            }
//        });
//    });

//    // ── Slot Select ───────────────────────────────────────
//    $(document).on('click', '.slot-btn', function () {
//        $('.slot-btn').removeClass('btn-primary').addClass('btn-outline-primary');
//        $(this).removeClass('btn-outline-primary').addClass('btn-primary');
//        $('#hdnSelectedSlotStart').val($(this).data('start'));
//        $('#hdnSelectedSlotEnd').val($(this).data('end'));
//        AppointmentValidation.clearErrors();
//    });

//    // ── Continue Button ───────────────────────────────────
//    $(document).on('click', '#continueBtn', function () {
//        var errors = AppointmentValidation.validate();
//        if (errors.length > 0) {
//            AppointmentValidation.showErrors(errors);
//            return;
//        }
//        AppointmentValidation.clearErrors();
//        $('#appointmentForm')
//            .attr('action', '?handler=Continue')
//            .submit();
//    });

//    // ── Render Doctor Card ────────────────────────────────
//    function renderDoctorCard(doc, facilityId, doctorId) {
//        var html = `
//        <div class="row">
//            <div class="col-md-3 text-center">
//                <img src="${doc.imageUrl}"
//                     style="width:180px;height:180px;object-fit:cover;border-radius:50%;"
//                     onerror="this.src='/images/default-doctor.png'" />
//                <div class="mt-2">
//                    <span class="badge bg-warning text-dark">
//                        ⭐ ${doc.rating || 'N/A'}
//                    </span>
//                    <span class="text-muted ms-1" style="font-size:12px;">
//                        (${doc.review || '0'} reviews)
//                    </span>
//                </div>
//                ${doc.isTeleconsult
//                ? `<div class="mt-2">
//                           <span class="badge bg-success">Teleconsultation Available</span>
//                       </div>`
//                : ''}
//            </div>
//            <div class="col-md-9">
//                <h3>${doc.name}</h3>
//                <p class="text-muted mb-3">${doc.specialisation}</p>
//                <div class="row g-2">
//                    <div class="col-md-6">
//                        <strong>Experience:</strong>
//                        <span>${doc.experience}</span>
//                    </div>
//                    <div class="col-md-6">
//                        <strong>Consultation Fee:</strong>
//                        <span class="text-success fw-bold">₹ ${doc.fee}</span>
//                    </div>
//                    <div class="col-md-6">
//                        <strong>Next Available:</strong>
//                        <span>${doc.timing}</span>
//                    </div>
//                    ${doc.treatment
//                ? `<div class="col-md-12">
//                               <strong>Treatments:</strong>
//                               <span>${doc.treatment}</span>
//                           </div>`
//                : ''}
//                    ${doc.hospitalName
//                ? `<div class="col-md-12">
//                               <strong>Hospital:</strong>
//                               <span>${doc.hospitalName}, ${doc.hospitalCity}</span>
//                           </div>`
//                : ''}
//                </div>
//            </div>
//        </div>

//        <hr class="my-4" />

//        <div class="row g-4">
//            <div class="col-md-4">
//                <h5 class="mb-3">Select Date</h5>
//                <input type="text" id="slotCalendar"
//                       class="form-control"
//                       placeholder="Select date"
//                       readonly />
//            </div>
//            <div class="col-md-8">
//                <h5 class="mb-3">
//                    Available Slots
//                    <span id="slotDateLabel" class="text-muted fs-6 ms-2"></span>
//                </h5>
//                <div id="slotsContainer" class="d-flex flex-wrap gap-2">
//                    ${renderSlots(doc.slots)}
//                </div>
//            </div>
//        </div>

//        <input type="hidden" id="hdnHospitalLocationId" value="${doc.hospitalLocationId}" />
//        <input type="hidden" id="hdnDoctorIdForSlot"    value="${doctorId}" />
//        <input type="hidden" id="hdnFacilityIdForSlot"  value="${facilityId}" />

//        <div class="text-end mt-5">
//            <button type="button" id="continueBtn"
//                    class="btn btn-primary px-5 py-3 rounded-pill">
//                Continue →
//            </button>
//        </div>`;

//        $('#doctorDetailsCard').html(html);
//        initCalendar(doc.hospitalLocationId, facilityId, doctorId);
//    }

//    // ── Render Slots ──────────────────────────────────────
//    function renderSlots(slots) {
//        if (!slots || slots.length === 0)
//            return '<div class="text-muted">No slots available for today.</div>';

//        return slots.map(function (slot) {
//            var parsed = new Date(slot.startTime.replace(' ', 'T'));
//            var isValid = !isNaN(parsed.getTime());
//            var timePart = isValid
//                ? parsed.toLocaleTimeString('en-US',
//                    { hour: '2-digit', minute: '2-digit' })
//                : slot.startTime;
//            var datePart = isValid
//                ? parsed.toLocaleDateString('en-IN',
//                    { day: '2-digit', month: 'short', year: 'numeric' })
//                : '';

//            return `<button type="button"
//                        class="btn btn-outline-primary rounded-pill px-3 py-1 slot-btn"
//                        style="font-size:13px;"
//                        data-start="${slot.startTime}"
//                        data-end="${slot.endTime}">
//                        <div>${timePart}</div>
//                        <div style="font-size:10px;color:#888;">${datePart}</div>
//                    </button>`;
//        }).join('');
//    }

//    // ── Calendar ──────────────────────────────────────────
//    function initCalendar(hospitalLocationId, facilityId, doctorId) {
//        if (window._flatpickrInstance)
//            window._flatpickrInstance.destroy();

//        window._flatpickrInstance = flatpickr('#slotCalendar', {
//            minDate: 'today',
//            dateFormat: 'Y-m-d',
//            defaultDate: 'today',
//            onChange: function (selectedDates, dateStr) {
//                loadSlots(dateStr, hospitalLocationId, facilityId, doctorId);
//            }
//        });
//    }

//    // ── Load Slots by Date ────────────────────────────────
//    function loadSlots(date, hospitalLocationId, facilityId, doctorId) {
//        $('#slotsContainer').html(
//            '<div class="text-muted">Loading slots...</div>');
//        $('#slotDateLabel').text('— ' + date);
//        $('#hdnSelectedSlotStart').val('');
//        $('#hdnSelectedSlotEnd').val('');

//        $.get('?handler=SlotsByDate', {
//            facilityId: facilityId,
//            doctorId: doctorId,
//            hospitalLocationId: hospitalLocationId,
//            date: date
//        }, function (data) {
//            $('#slotsContainer').html(
//                data && data.length > 0
//                    ? renderSlots(data)
//                    : '<div class="text-muted">No slots available for this date.</div>'
//            );
//        });
//    }

//    // ── Helpers ───────────────────────────────────────────
//    function resetDropdown(selector, placeholder) {
//        $(selector)
//            .empty()
//            .append($('<option>', { value: '', text: placeholder }))
//            .trigger('change.select2');
//    }

//    function populateDropdown(selector, data, valField, textField, placeholder) {
//        var $el = $(selector);
//        $el.empty().append($('<option>', { value: '', text: placeholder }));
//        $.each(data, function (i, item) {
//            $el.append($('<option>', {
//                value: item[valField],
//                text: item[textField] || 'N/A'
//            }));
//        });
//        $el.trigger('change.select2');
//    }
//});