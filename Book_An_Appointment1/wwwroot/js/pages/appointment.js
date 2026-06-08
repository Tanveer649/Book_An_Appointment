$(document).ready(function () {

    if (performance.navigation.type === 1) {
        window.location.href = window.location.pathname;
        return;
    }

    // Select2 initialize
    $('#facilityDropdown').select2({ width: '100%', placeholder: 'Select Facility' });
    $('#specialityDropdown').select2({ width: '100%', placeholder: 'Search Speciality' });
    $('#doctorDropdown').select2({ width: '100%', placeholder: 'Search Doctor' });

    // Selected values restore
    $('#facilityDropdown').val(window.AppointmentConfig.facilityId).trigger('change.select2');
    $('#specialityDropdown').val(window.AppointmentConfig.specialityId).trigger('change.select2');

    if (window.AppointmentConfig.doctorId > 0) {
        $('#doctorDropdown').val(window.AppointmentConfig.doctorId).trigger('change.select2');
        initCalendar();
    }

    // Dropdown change events
    $('#facilityDropdown').on('change', function () { $('form').submit(); });
    $('#specialityDropdown').on('change', function () { $('form').submit(); });
    $('#doctorDropdown').on('change', function () { $('form').submit(); });

    // Slot select
    $(document).on('click', '.slot-btn', function () {
        $('.slot-btn').removeClass('btn-primary').addClass('btn-outline-primary');
        $(this).removeClass('btn-outline-primary').addClass('btn-primary');
        $('#hdnSelectedSlotStart').val($(this).data('start'));
        $('#hdnSelectedSlotEnd').val($(this).data('end'));
        AppointmentValidation.clearErrors(); // slot select hone pe error clear
    });

    // Continue button
    $('#continueBtn').on('click', function () {
        var errors = AppointmentValidation.validate();

        if (errors.length > 0) {
            AppointmentValidation.showErrors(errors);
            return;
        }

        AppointmentValidation.clearErrors();
        $('#formHandler').val('Continue');
        $('#appointmentForm').submit();
    });

});

function initCalendar() {
    flatpickr("#slotCalendar", {
        minDate: "today",
        dateFormat: "Y-m-d",
        defaultDate: "today",
        onChange: function (selectedDates, dateStr) {
            loadSlots(dateStr);
        }
    });
}

function loadSlots(date) {
    var facilityId = $('#hdnFacilityIdForSlot').val();
    var doctorId = $('#hdnDoctorIdForSlot').val();
    var hospitalLocationId = $('#hdnHospitalLocationId').val();

    $('#slotsContainer').html('<div class="text-muted">Loading slots...</div>');
    $('#slotDateLabel').text('— ' + date);

    $.get('?handler=SlotsByDate', {
        facilityId: facilityId,
        doctorId: doctorId,
        hospitalLocationId: hospitalLocationId,
        date: date
    }, function (data) {
        $('#slotsContainer').empty();

        if (!data || data.length === 0) {
            $('#slotsContainer').html('<div class="text-muted">No slots available for this date.</div>');
            return;
        }

        $.each(data, function (i, slot) {
            var parsed = new Date(slot.startTime.replace(' ', 'T'));
            var timePart = isNaN(parsed) ? slot.startTime
                : parsed.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
            var datePart = isNaN(parsed) ? ''
                : parsed.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });

            $('#slotsContainer').append(
                $('<button>', {
                    type: 'button',
                    class: 'btn btn-outline-primary rounded-pill px-3 py-1 slot-btn',
                    style: 'font-size:13px;',
                    'data-start': slot.startTime,
                    'data-end': slot.endTime
                }).append(
                    $('<div>').text(timePart),
                    $('<div>', { style: 'font-size:10px;color:#888;' }).text(datePart)
                )
            );
        });
    });
}