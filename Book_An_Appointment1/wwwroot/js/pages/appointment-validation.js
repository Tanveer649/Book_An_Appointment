var AppointmentValidation = {

    rules: {
        slot: {
            selector: '#hdnSelectedSlotStart',
            message: 'Please select a time slot.'
        }
        // Future mein yahan add karo:
        // facility: {
        //     selector: '#facilityDropdown',
        //     message: 'Please select a Facility.'
        // },
        // speciality: {
        //     selector: '#specialityDropdown',
        //     message: 'Please select a Speciality.'
        // },
        // doctor: {
        //     selector: '#doctorDropdown',
        //     message: 'Please select a Doctor.'
        // }
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