function initPatientDetailsPage() {

    let lockedMode = null;          // 'new' | 'registered' once the type was chosen on the previous page
    const timers = {};              // one countdown per OTP box (new / registered), they must not cancel each other

    function onReady(fn) {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
    }

    onReady(() => {
        loadTitles();
        loadCountries();
        prefillCommonFields();
        limitDobToPast();

        // Open the right section (also after a server-side validation failure)
        switchMode(SavedPatientType === 'registered' ? 'registered' : 'new');
        applyPatientTypeLock(SavedPatientType);

        document.querySelectorAll('input, select, textarea').forEach(el => {
            el.addEventListener('input', () => clearError(el.id));
            el.addEventListener('change', () => clearError(el.id));
        });

        const form = document.getElementById('patientForm');
        if (form) form.addEventListener('submit', () => {
            // validateForm() already ran in onsubmit; only a valid form reaches this point
            if (!form.dataset.invalid) setSubmitting(true);
        });
        window.addEventListener('pageshow', () => setSubmitting(false));   // coming back with the Back button
    });

    // ── Patient type: toggle + lock ──────────────────────
    window.switchMode = function (mode) {
        if (lockedMode && mode !== lockedMode) return;       // the other section is locked

        const newForm = document.getElementById('new-patient-form');
        const regForm = document.getElementById('registered-form');
        const btnNew = document.getElementById('btn-new');
        const btnReg = document.getElementById('btn-existing');
        const isNew = mode === 'new';

        newForm.style.display = isNew ? 'block' : 'none';
        regForm.style.display = isNew ? 'none' : 'block';
        btnNew.classList.toggle('active', isNew);
        btnReg.classList.toggle('active', !isNew);
        btnNew.setAttribute('aria-pressed', String(isNew));
        btnReg.setAttribute('aria-pressed', String(!isNew));
        document.getElementById('Form_PatientType').value = isNew ? 'new' : 'registered';
    };

    function applyPatientTypeLock(type) {
        if (type !== 'new' && type !== 'registered') return;     // opened without a choice → both stay clickable
        lockedMode = type;

        const other = document.getElementById(type === 'new' ? 'btn-existing' : 'btn-new');
        other.disabled = true;
        other.setAttribute('aria-disabled', 'true');
        other.title = 'Go back to change the patient type';
        document.getElementById('patientToggle').classList.add('is-locked');

        //document.getElementById('toggleNoteType').textContent = type === 'new' ? 'a new patient' : 'an already registered patient';
        //document.getElementById('toggleNote').hidden = false;
    }

    function setSubmitting(on) {
        const btn = document.getElementById('submitBtn');
        if (!btn) return;
        btn.disabled = on;
        btn.textContent = on ? 'Please wait…' : 'Review & Confirm →';
    }

    function limitDobToPast() {
        const dob = document.getElementById('Form_DOB');
        if (!dob) return;
        const y = new Date(); y.setDate(y.getDate() - 1);
        dob.max = y.getFullYear() + '-' + String(y.getMonth() + 1).padStart(2, '0') + '-' + String(y.getDate()).padStart(2, '0');
    }

    // ── Common — Prefill from session ────────────────────
    function prefillCommonFields() {
        const verified = document.getElementById('Form_IsMobileVerified').value;
        const mobile = document.getElementById('phone-input').value;
        const btn = document.getElementById('send-otp-btn');

        if (verified === 'true') {
            document.getElementById('verify-badge').classList.add('show');
            btn.textContent = 'OTP Sent ✓';
            btn.classList.add('otp-sent');
            btn.disabled = true;
        } else if (mobile && mobile.length === 10) {
            btn.disabled = false;
        }

        const regVerified = document.getElementById('Reg_IsMobileVerified').value;
        if (regVerified === 'true') {
            const regBtn = document.getElementById('reg-send-otp-btn');
            regBtn.textContent = 'OTP Sent ✓';
            regBtn.classList.add('otp-sent');
            regBtn.disabled = true;
            document.getElementById('reg-otp-verified').classList.add('show');

            // Back / validation failure — rebuild the family cards from session, keep the selection
            renderPatientCards(window.SavedRegisteredPatients || [], document.getElementById('Reg_RegistrationNo').value);
        } else {
            const regMobile = document.getElementById('reg-phone-input').value;
            if (regMobile && regMobile.length === 10) document.getElementById('reg-send-otp-btn').disabled = false;
        }

        const savedGender = document.getElementById('Form_Gender').value;
        if (savedGender) {
            const gbtn = document.querySelector(`.gender-btn[data-value="${savedGender}"]`);
            if (gbtn) gbtn.classList.add('sel');
        }
        document.getElementById('char-count').textContent =
            (document.getElementById('Form_AppointmentNotes')?.value || '').length;
    }

    // ── Gender ───────────────────────────────────────────
    window.selGender = function (el) {
        document.querySelectorAll('.gender-btn').forEach(b => b.classList.remove('sel'));
        el.classList.add('sel');
        document.getElementById('Form_Gender').value = el.dataset.value;
        document.getElementById('gender-error').style.display = 'none';
    };

    function clearGender() {
        document.querySelectorAll('.gender-btn').forEach(b => b.classList.remove('sel'));
        document.getElementById('Form_Gender').value = '';
    }

    function autoSelectGender(value) {
        const btn = document.querySelector(`.gender-btn[data-value="${value}"]`);
        if (btn) {
            document.querySelectorAll('.gender-btn').forEach(b => b.classList.remove('sel'));
            btn.classList.add('sel');
            document.getElementById('Form_Gender').value = value;
            document.getElementById('gender-error').style.display = 'none';
        }
    }

    // ── Titles ───────────────────────────────────────────
    function loadTitles() {
        fetch('?handler=Titles')
            .then(r => r.json())
            .then(res => {
                if (!res.success) return;

                const select = document.getElementById('Form_Title');
                select.innerHTML = '<option value="">--</option>';

                res.data.forEach(t => {
                    const opt = document.createElement('option');
                    opt.value = t.name.trim();
                    opt.text = t.name.trim();
                    opt.dataset.gender = t.gender;
                    opt.dataset.titleId = t.titleId;
                    select.appendChild(opt);
                });

                $('#Form_Title').select2({ width: '100%', placeholder: '--', minimumResultsForSearch: Infinity });

                if (SavedTitleName) {
                    $('#Form_Title').val(SavedTitleName).trigger('change.select2');
                    document.getElementById('Form_TitleId').value = SavedTitleId;
                }

                $('#Form_Title').off('change.titleHandler').on('change.titleHandler', function () {
                    clearError('Form_Title');
                    const opt = this.options[this.selectedIndex];
                    document.getElementById('Form_TitleId').value = opt?.dataset?.titleId || '';

                    const gender = opt?.dataset?.gender;
                    if (gender === 'M') autoSelectGender('Male');
                    else if (gender === 'F') autoSelectGender('Female');
                    else clearGender();
                });
            })
            .catch(err => console.error('Titles load failed:', err));
    }

    // ── Countries / States / Cities ──────────────────────
    function loadCountries() {
        fetch('?handler=Countries')
            .then(r => r.json())
            .then(res => {
                if (!res.success) return;

                const select = document.getElementById('Form_Country');
                select.innerHTML = '<option value="">Select country</option>';

                res.data.forEach(c => {
                    const opt = document.createElement('option');
                    opt.value = c.countryId;
                    opt.text = c.countryName.trim();
                    select.appendChild(opt);
                });

                $('#Form_Country').select2({ width: '100%', placeholder: 'Select country' });

                if (SavedCountryName) {
                    const opt = Array.from(select.options).find(o => o.text === SavedCountryName);
                    if (opt) {
                        $('#Form_Country').val(opt.value).trigger('change.select2');
                        document.getElementById('Form_CountryId').value = opt.value;
                        document.getElementById('Form_CountryName').value = SavedCountryName;
                        loadStates(opt.value);
                    }
                } else {
                    $('#Form_Country').val(DefaultCountryId).trigger('change.select2');
                    document.getElementById('Form_CountryId').value = DefaultCountryId;
                    loadStates(DefaultCountryId);
                }

                $('#Form_Country').off('change').on('change', function () {
                    const countryId = this.value;
                    document.getElementById('Form_CountryId').value = countryId;
                    document.getElementById('Form_CountryName').value = this.options[this.selectedIndex]?.text || '';

                    resetLocationDropdown('Form_State', 'Select state');
                    resetLocationDropdown('Form_City', 'Select city');
                    document.getElementById('Form_StateId').value = '';
                    document.getElementById('Form_StateName').value = '';
                    document.getElementById('Form_CityId').value = '';
                    document.getElementById('Form_CityName').value = '';

                    if (countryId) loadStates(countryId);
                });
            })
            .catch(err => console.error('Countries load failed:', err));
    }

    function loadStates(countryId) {
        const stateSelect = document.getElementById('Form_State');
        stateSelect.innerHTML = '<option value="">Loading...</option>';

        if ($.fn.select2 && $(stateSelect).data('select2')) $('#Form_State').select2('destroy');
        stateSelect.disabled = true;

        fetch(`?handler=States&countryId=${countryId}`)
            .then(r => r.json())
            .then(res => {
                stateSelect.innerHTML = '<option value="">Select state</option>';

                if (!res.success || res.data.length === 0) { stateSelect.disabled = true; return; }

                res.data.forEach(s => {
                    const opt = document.createElement('option');
                    opt.value = s.stateId;
                    opt.text = s.stateName.trim();
                    stateSelect.appendChild(opt);
                });

                stateSelect.disabled = false;
                $('#Form_State').select2({ width: '100%', placeholder: 'Select state' });

                if (SavedStateId && SavedStateId !== '0') {
                    $('#Form_State').val(SavedStateId).trigger('change.select2');
                    document.getElementById('Form_StateId').value = SavedStateId;
                    document.getElementById('Form_StateName').value = SavedStateName;
                    loadCities(SavedStateId);
                }

                $('#Form_State').off('change.stateHandler').on('change.stateHandler', function () {
                    document.getElementById('Form_StateId').value = this.value;
                    document.getElementById('Form_StateName').value = this.options[this.selectedIndex]?.text || '';

                    resetLocationDropdown('Form_City', 'Select city');
                    document.getElementById('Form_CityId').value = '';
                    document.getElementById('Form_CityName').value = '';

                    if (this.value) loadCities(this.value);
                });
            })
            .catch(() => {
                stateSelect.innerHTML = '<option value="">Select state</option>';
                stateSelect.disabled = true;
            });
    }

    function loadCities(stateId) {
        const citySelect = document.getElementById('Form_City');
        citySelect.innerHTML = '<option value="">Loading...</option>';

        if ($.fn.select2 && $(citySelect).data('select2')) $('#Form_City').select2('destroy');
        citySelect.disabled = true;

        fetch(`?handler=Cities&stateId=${stateId}`)
            .then(r => r.json())
            .then(res => {
                citySelect.innerHTML = '<option value="">Select city</option>';

                if (!res.success || res.data.length === 0) { citySelect.disabled = true; return; }

                res.data.forEach(c => {
                    const opt = document.createElement('option');
                    opt.value = c.cityId;
                    opt.text = c.cityName.trim();
                    citySelect.appendChild(opt);
                });

                citySelect.disabled = false;
                $('#Form_City').select2({ width: '100%', placeholder: 'Select city' });

                if (SavedCityId && SavedCityId !== '0') {
                    $('#Form_City').val(SavedCityId).trigger('change.select2');
                    document.getElementById('Form_CityId').value = SavedCityId;
                    document.getElementById('Form_CityName').value = SavedCityName;
                }

                $('#Form_City').off('change.cityHandler').on('change.cityHandler', function () {
                    document.getElementById('Form_CityId').value = this.value;
                    document.getElementById('Form_CityName').value = this.options[this.selectedIndex]?.text || '';
                });
            })
            .catch(() => {
                citySelect.innerHTML = '<option value="">Select city</option>';
                citySelect.disabled = true;
            });
    }

    function resetLocationDropdown(id, placeholder) {
        if ($.fn.select2 && $('#' + id).data('select2')) $('#' + id).select2('destroy');
        const el = document.getElementById(id);
        el.innerHTML = `<option value="">${placeholder}</option>`;
        el.disabled = true;
    }

    // ── OTP helpers shared by both sections ──────────────
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;

    function postJson(handler, body) {
        return fetch('?handler=' + handler, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body: JSON.stringify(body)
        }).then(r => r.json());
    }

    function buildOtpInputs(containerId, type) {
        const container = document.getElementById(containerId);
        container.innerHTML = '';

        for (let i = 1; i <= OTP_LENGTH; i++) {
            const input = document.createElement('input');
            input.className = 'otp-input';
            input.type = 'text';
            input.maxLength = 1;
            input.id = type + 'o' + i;
            input.inputMode = 'numeric';
            input.setAttribute('aria-label', `OTP digit ${i}`);
            if (i === 1) input.autocomplete = 'one-time-code';

            const nextId = i < OTP_LENGTH ? type + 'o' + (i + 1) : null;
            const prevId = i > 1 ? type + 'o' + (i - 1) : null;

            input.addEventListener('input', function () {
                this.value = this.value.replace(/\D/g, '');
                if (this.value.length === 1 && nextId) document.getElementById(nextId)?.focus();
                checkOtpComplete(containerId, type);
            });

            input.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && this.value === '' && prevId) document.getElementById(prevId)?.focus();
            });

            // Paste (or SMS autofill) of the whole code into any box
            input.addEventListener('paste', function (e) {
                const digits = ((e.clipboardData || window.clipboardData)?.getData('text') || '').replace(/\D/g, '').slice(0, OTP_LENGTH);
                if (!digits) return;
                e.preventDefault();
                container.querySelectorAll('.otp-input').forEach((el, idx) => { el.value = digits[idx] || ''; });
                container.querySelectorAll('.otp-input')[Math.min(digits.length, OTP_LENGTH) - 1]?.focus();
                checkOtpComplete(containerId, type);
            });

            container.appendChild(input);
        }
        document.getElementById(type + 'o1')?.focus();
    }

    function checkOtpComplete(containerId, type) {
        const inputs = document.querySelectorAll(`#${containerId} .otp-input`);
        const otp = Array.from(inputs).map(i => i.value).join('');
        if (otp.length === OTP_LENGTH) {
            if (type === 'new') verifyOtp(otp);
            else verifyRegisteredOtp(otp);
        }
    }

    function startTimer(displayId, resendBtnId) {
        clearInterval(timers[displayId]);
        let seconds = 30;
        const display = document.getElementById(displayId);
        const resendBtn = document.getElementById(resendBtnId);
        resendBtn.disabled = true;
        display.textContent = `Resend in 0:${String(seconds).padStart(2, '0')}`;

        timers[displayId] = setInterval(() => {
            seconds--;
            display.textContent = seconds > 0 ? `Resend in 0:${String(seconds).padStart(2, '0')}` : '';
            if (seconds <= 0) {
                clearInterval(timers[displayId]);
                resendBtn.disabled = false;
            }
        }, 1000);
    }

    // ── New patient — phone & OTP ────────────────────────
    window.checkPhone = function () {
        const val = document.getElementById('phone-input').value.replace(/\D/g, '');
        document.getElementById('phone-input').value = val;
        const btn = document.getElementById('send-otp-btn');

        if (btn.classList.contains('otp-sent')) {
            btn.classList.remove('otp-sent');
            btn.textContent = 'Send OTP';
            document.getElementById('otp-section').classList.remove('visible');
            document.getElementById('otp-verified').classList.remove('show');
            document.getElementById('verify-badge').classList.remove('show');
            document.getElementById('new-otp-inputs').innerHTML = '';
            document.getElementById('Form_IsMobileVerified').value = 'false';
            clearInterval(timers['timer-display']);
            clearOtpError('new');
        }

        btn.disabled = val.length !== 10;
        clearError('phone-input');
    };

    window.sendOTP = function () {
        const mobile = document.getElementById('phone-input').value;
        const btn = document.getElementById('send-otp-btn');

        if (!mobile || mobile.length !== 10) {
            showError('phone-input', 'Please enter a valid 10-digit mobile number');
            return;
        }

        btn.disabled = true;
        btn.textContent = 'Sending...';

        postJson('SendOtp', { mobileNo: mobile })
            .then(data => {
                if (data.success) {
                    btn.textContent = 'OTP Sent ✓';
                    btn.classList.add('otp-sent');
                    document.getElementById('otp-phone-display').textContent = mobile;
                    document.getElementById('otp-section').classList.add('visible');
                    buildOtpInputs('new-otp-inputs', 'new');
                    startTimer('timer-display', 'resend-btn');
                    clearError('phone-input');
                } else {
                    showError('phone-input', data.message || 'Failed to send OTP');
                    btn.disabled = false;
                    btn.textContent = 'Send OTP';
                }
            })
            .catch(() => {
                showError('phone-input', 'Something went wrong. Please try again.');
                btn.disabled = false;
                btn.textContent = 'Send OTP';
            });
    };

    function verifyOtp(otp) {
        const mobile = document.getElementById('phone-input').value;

        postJson('VerifyOtp', { mobileNo: mobile, otpNo: otp })
            .then(data => {
                if (data.success) {
                    document.getElementById('otp-verified').classList.add('show');
                    document.getElementById('verify-badge').classList.add('show');
                    document.querySelectorAll('#new-otp-inputs .otp-input').forEach(i => i.disabled = true);
                    document.getElementById('Form_IsMobileVerified').value = 'true';
                    document.getElementById('Form_Mobile').value = mobile;
                    clearOtpError('new');
                } else {
                    document.querySelectorAll('#new-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
                    document.getElementById('newo1')?.focus();
                    showOtpError('new-otp-inputs', data.message || 'Invalid OTP. Please try again.');
                }
            })
            .catch(() => showOtpError('new-otp-inputs', 'Something went wrong. Please try again.'));
    }

    window.resendOTP = function () {
        document.querySelectorAll('#new-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
        document.getElementById('otp-verified').classList.remove('show');
        document.getElementById('Form_IsMobileVerified').value = 'false';
        clearOtpError('new');

        const btn = document.getElementById('send-otp-btn');
        btn.disabled = false;
        btn.textContent = 'Send OTP';
        btn.classList.remove('otp-sent');
        sendOTP();
    };

    // ── Registered patient — phone & OTP ─────────────────
    window.checkRegPhone = function () {
        const val = document.getElementById('reg-phone-input').value.replace(/\D/g, '');
        document.getElementById('reg-phone-input').value = val;
        const btn = document.getElementById('reg-send-otp-btn');

        if (btn.classList.contains('otp-sent')) {
            btn.classList.remove('otp-sent');
            btn.textContent = 'Send OTP';
            document.getElementById('reg-otp-section').classList.remove('visible');
            document.getElementById('reg-otp-verified').classList.remove('show');
            document.getElementById('reg-otp-inputs').innerHTML = '';
            document.getElementById('Reg_IsMobileVerified').value = 'false';
            document.getElementById('patient-found-card').style.display = 'none';
            clearInterval(timers['reg-timer-display']);
            clearOtpError('reg');
        }

        btn.disabled = val.length !== 10;
        clearError('reg-phone-input');
    };

    window.sendRegisteredOTP = function () {
        const mobile = document.getElementById('reg-phone-input').value;
        const btn = document.getElementById('reg-send-otp-btn');

        if (!mobile || mobile.length !== 10) {
            showError('reg-phone-input', 'Please enter a valid 10-digit mobile number');
            return;
        }

        btn.disabled = true;
        btn.textContent = 'Sending...';

        postJson('SendRegisteredOtp', { mobileNo: mobile })
            .then(data => {
                if (data.success) {
                    btn.textContent = 'OTP Sent ✓';
                    btn.classList.add('otp-sent');
                    document.getElementById('reg-otp-phone-display').textContent = mobile;
                    document.getElementById('reg-otp-section').classList.add('visible');
                    buildOtpInputs('reg-otp-inputs', 'reg');
                    startTimer('reg-timer-display', 'reg-resend-btn');
                    clearError('reg-phone-input');
                } else {
                    showError('reg-phone-input', data.message || 'Failed to send OTP');
                    btn.disabled = false;
                    btn.textContent = 'Send OTP';
                }
            })
            .catch(() => {
                showError('reg-phone-input', 'Something went wrong. Please try again.');
                btn.disabled = false;
                btn.textContent = 'Send OTP';
            });
    };

    function verifyRegisteredOtp(otp) {
        const mobile = document.getElementById('reg-phone-input').value;

        postJson('VerifyRegisteredOtp', { mobileNo: mobile, otpNo: otp })
            .then(data => {
                if (data.success) {
                    document.getElementById('reg-otp-verified').classList.add('show');
                    document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => i.disabled = true);
                    document.getElementById('Reg_IsMobileVerified').value = 'true';
                    clearOtpError('reg');
                    // single member comes pre-selected by the server; otherwise the user picks a card
                    fillRegisteredHiddenFields(data.selected || {});
                    renderPatientCards(data.patientList || [], data.selected ? data.selected.registrationNo : '');
                } else {
                    document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
                    document.getElementById('rego1')?.focus();
                    showOtpError('reg-otp-inputs', data.message || 'Invalid OTP. Please try again.');
                }
            })
            .catch(() => showOtpError('reg-otp-inputs', 'Something went wrong. Please try again.'));
    }

    function fillRegisteredHiddenFields(patient) {
        document.getElementById('Reg_GuestPatientId').value = patient.guestPatientId || '';
        document.getElementById('Reg_RegistrationNo').value = patient.registrationNo || '0';
        document.getElementById('Reg_TitleName').value = patient.titleName || '';
        document.getElementById('Reg_FirstName').value = patient.firstName || '';
        document.getElementById('Reg_MiddleName').value = patient.middleName || '';
        document.getElementById('Reg_LastName').value = patient.lastName || '';
        document.getElementById('Reg_Gender').value = patient.gender || '';
        document.getElementById('Reg_DOB').value = patient.dob || '';
        document.getElementById('Reg_Email').value = patient.emailId || '';
    }

    // ── Registered patient — family member cards ─────────
    function renderPatientCards(patients, selectedRegNo) {
        const wrap = document.getElementById('patient-found-card');
        const list = document.getElementById('pf-list');
        list.innerHTML = '';
        hidePatientError();

        if (!patients.length) { wrap.style.display = 'none'; return; }
        wrap.style.display = 'block';

        document.getElementById('pf-list-sub').textContent = patients.length === 1
            ? '1 patient registered with this mobile number'
            : `${patients.length} patients registered with this mobile number — choose whose appointment to book`;

        patients.forEach(p => list.appendChild(buildPatientCard(p, p.registrationNo === selectedRegNo)));
    }

    function buildPatientCard(p, selected) {
        const name = cleanName(p.patientName);
        const age = (p.age || '').split('/')[0].trim();          // "31 Yrs/Male" → "31 Yrs"

        const card = el('button', 'pf-card' + (selected ? ' selected' : ''));
        card.type = 'button';
        card.setAttribute('role', 'radio');
        card.setAttribute('aria-checked', selected ? 'true' : 'false');
        card.dataset.reg = p.registrationNo || '';
        card.addEventListener('click', () => selectRegisteredPatient(card));

        card.appendChild(el('span', 'pf-card-avatar', initialsOf(name)));

        const body = el('span', 'pf-card-body');
        body.appendChild(el('span', 'pf-card-name', name));

        const tags = el('span', 'pf-card-tags');
        if (p.gender) tags.appendChild(el('span', 'pf-tag', p.gender));
        if (p.dob) tags.appendChild(el('span', 'pf-tag', formatDob(p.dob)));
        body.appendChild(tags);

        const uhid = el('span', 'pf-card-uhid');
        uhid.appendChild(el('span', 'pf-uhid-label', 'UHID'));
        uhid.appendChild(el('span', 'pf-uhid-value', p.registrationNo || '—'));
        body.appendChild(uhid);
        card.appendChild(body);

        card.appendChild(el('span', 'pf-card-check', '✓'));

        // Full details — visible only on hover / keyboard focus (and for the selected card on touch screens)
        const more = el('span', 'pf-card-more');
        [['Full name', name], ['Gender', p.gender], ['Date of birth', formatDob(p.dob)], ['Age', age],
            ['UHID', p.registrationNo], ['Email', p.emailId]]
            .filter(r => r[1])
            .forEach(r => {
                const row = el('span', 'pf-more-row');
                row.appendChild(el('span', 'pf-more-label', r[0]));
                row.appendChild(el('span', 'pf-more-value', r[1]));
                more.appendChild(row);
            });
        card.appendChild(more);

        return card;
    }

    function selectRegisteredPatient(card) {
        const list = document.getElementById('pf-list');
        if (list.getAttribute('aria-busy') === 'true' || card.classList.contains('selected')) return;

        list.setAttribute('aria-busy', 'true');
        postJson('SelectRegisteredPatient', { registrationNo: card.dataset.reg })
            .then(data => {
                if (!data.success) { showPatientError(data.message || 'Unable to select patient'); return; }
                list.querySelectorAll('.pf-card').forEach(c => {
                    c.classList.toggle('selected', c === card);
                    c.setAttribute('aria-checked', c === card ? 'true' : 'false');
                });
                fillRegisteredHiddenFields(data.selected);
                hidePatientError();
            })
            .catch(() => showPatientError('Something went wrong. Please try again.'))
            .finally(() => list.removeAttribute('aria-busy'));
    }

    function showPatientError(msg) {
        const err = document.getElementById('reg-patient-error');
        err.textContent = msg;
        err.style.display = 'block';
    }

    function hidePatientError() {
        document.getElementById('reg-patient-error').style.display = 'none';
    }

    function el(tag, cls, text) {
        const e = document.createElement(tag);
        if (cls) e.className = cls;
        if (text != null) e.textContent = text;
        return e;
    }

    function cleanName(name) {
        return String(name || '').replace(/\s+/g, ' ').trim();
    }

    function initialsOf(name) {
        const words = name.split(' ').filter(w => !/^(mr|mrs|ms|miss|dr|master|baby)\.?$/i.test(w));
        if (!words.length) return 'P';
        return (words[0][0] + (words.length > 1 ? words[words.length - 1][0] : '')).toUpperCase();
    }

    function formatDob(dob) {
        if (!dob) return '';
        const d = new Date(dob);
        return isNaN(d.getTime()) ? dob : d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
    }

    window.resendRegisteredOTP = function () {
        document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
        document.getElementById('reg-otp-verified').classList.remove('show');
        document.getElementById('Reg_IsMobileVerified').value = 'false';
        document.getElementById('patient-found-card').style.display = 'none';
        clearOtpError('reg');

        const btn = document.getElementById('reg-send-otp-btn');
        btn.disabled = false;
        btn.textContent = 'Send OTP';
        btn.classList.remove('otp-sent');
        sendRegisteredOTP();
    };

    // ── Notes counter ────────────────────────────────────
    window.countChars = function (el) {
        document.getElementById('char-count').textContent = el.value.length;
    };

    // ── Error helpers ────────────────────────────────────
    window.showError = function (fieldId, message) {
        clearError(fieldId);
        const field = document.getElementById(fieldId);
        if (!field) return;
        field.classList.add('input-error');
        const err = document.createElement('span');
        err.className = 'field-error';
        err.id = fieldId + '_err';
        err.setAttribute('role', 'alert');
        err.textContent = message;
        field.closest('.form-group')
            ? field.closest('.form-group').appendChild(err)
            : field.insertAdjacentElement('afterend', err);
    };

    window.clearError = function (fieldId) {
        const field = document.getElementById(fieldId);
        if (field) field.classList.remove('input-error');
        const err = document.getElementById(fieldId + '_err');
        if (err) err.remove();
    };

    function showOtpError(containerId, message) {
        const type = containerId.includes('reg') ? 'reg' : 'new';
        clearOtpError(type);
        const err = document.createElement('div');
        err.className = 'field-error mt-1';
        err.id = type + '-otp-error';
        err.textContent = message;
        document.getElementById(containerId).after(err);
    }

    function clearOtpError(type) {
        const err = document.getElementById(type + '-otp-error');
        if (err) err.remove();
    }

    function closeServerErrorModal() {
        const modal = document.getElementById('serverErrorModal');
        if (modal) modal.style.display = 'none';
    }
    window.closeServerErrorModal = closeServerErrorModal;

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeServerErrorModal();
    });

    // ── Validation ───────────────────────────────────────
    const nameRegex = /^[a-zA-Z\s'\-]+$/;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    // Remove every inline message from the previous attempt, so only problems that exist *now* are shown
    function clearAllErrors() {
        document.querySelectorAll('.field-error[id$="_err"]').forEach(e => e.remove());
        document.querySelectorAll('.input-error').forEach(e => e.classList.remove('input-error'));
        document.getElementById('gender-error').style.display = 'none';
        document.getElementById('consent-error').style.display = 'none';
        hidePatientError();
    }

    window.validateForm = function () {
        clearAllErrors();
        let isValid = true;
        let firstErrEl = null;

        const patientType = document.getElementById('Form_PatientType').value;

        if (patientType === 'new' || !patientType) {
            const checks = [
                ['Form_Title', document.getElementById('Form_Title').value, 'Please select a title'],
                ['Form_FirstName', document.getElementById('Form_FirstName').value.trim(), 'First name is required'],
                ['Form_LastName', document.getElementById('Form_LastName').value.trim(), 'Last name is required'],
                ['Form_DOB', document.getElementById('Form_DOB').value, 'Date of birth is required']
            ];

            checks.forEach(([id, val, msg]) => {
                if (!val) {
                    showError(id, msg);
                    if (!firstErrEl) firstErrEl = document.getElementById(id);
                    isValid = false;
                } else clearError(id);
            });

            const firstName = document.getElementById('Form_FirstName').value.trim();
            if (firstName && !nameRegex.test(firstName)) {
                showError('Form_FirstName', 'First name should contain letters only');
                if (!firstErrEl) firstErrEl = document.getElementById('Form_FirstName');
                isValid = false;
            }

            const lastName = document.getElementById('Form_LastName').value.trim();
            if (lastName && !nameRegex.test(lastName)) {
                showError('Form_LastName', 'Last name should contain letters only');
                if (!firstErrEl) firstErrEl = document.getElementById('Form_LastName');
                isValid = false;
            }

            const middleName = document.getElementById('Form_MiddleName').value.trim();
            if (middleName && !nameRegex.test(middleName)) {
                showError('Form_MiddleName', 'Middle name should contain letters only');
                if (!firstErrEl) firstErrEl = document.getElementById('Form_MiddleName');
                isValid = false;
            }

            const dob = document.getElementById('Form_DOB').value;
            if (dob) {
                const dobDate = new Date(dob);
                const today = new Date(); today.setHours(0, 0, 0, 0);
                if (dobDate >= today) {
                    showError('Form_DOB', 'Date of birth cannot be today or a future date');
                    if (!firstErrEl) firstErrEl = document.getElementById('Form_DOB');
                    isValid = false;
                }
            }

            const gender = document.getElementById('Form_Gender').value;
            const genderErr = document.getElementById('gender-error');
            if (!gender) {
                genderErr.style.display = 'block';
                if (!firstErrEl) firstErrEl = genderErr;
                isValid = false;
            } else genderErr.style.display = 'none';

            const email = document.getElementById('Form_Email')?.value?.trim();
            if (email && !emailRegex.test(email)) {
                showError('Form_Email', 'Please enter a valid email address');
                if (!firstErrEl) firstErrEl = document.getElementById('Form_Email');
                isValid = false;
            }

            const pincode = document.getElementById('Form_Pincode')?.value?.trim();
            if (pincode && !/^\d{6}$/.test(pincode)) {
                showError('Form_Pincode', 'Pincode must be exactly 6 digits');
                if (!firstErrEl) firstErrEl = document.getElementById('Form_Pincode');
                isValid = false;
            }

            const mobile = document.getElementById('phone-input').value.trim();
            const verified = document.getElementById('Form_IsMobileVerified').value;
            if (!mobile) {
                showError('phone-input', 'Mobile number is required');
                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
                isValid = false;
            } else if (!/^\d{10}$/.test(mobile)) {
                showError('phone-input', 'Please enter a valid 10-digit mobile number');
                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
                isValid = false;
            } else if (verified !== 'true') {
                showError('phone-input', 'Please verify your mobile number via OTP');
                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
                isValid = false;
            }
        } else if (patientType === 'registered') {
            const mobile = document.getElementById('reg-phone-input').value.trim();
            const verified = document.getElementById('Reg_IsMobileVerified').value;

            if (!mobile) {
                showError('reg-phone-input', 'Mobile number is required');
                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
                isValid = false;
            } else if (!/^\d{10}$/.test(mobile)) {
                showError('reg-phone-input', 'Please enter a valid 10-digit mobile number');
                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
                isValid = false;
            } else if (verified !== 'true') {
                showError('reg-phone-input', 'Please verify your mobile number via OTP');
                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
                isValid = false;
            } else {
                const regNo = document.getElementById('Reg_RegistrationNo').value;
                if (!regNo || regNo === '0') {
                    showPatientError('Please select the patient for whom you want to book the appointment');
                    if (!firstErrEl) firstErrEl = document.getElementById('pf-list');
                    isValid = false;
                }
            }
        }

        const consent = document.getElementById('Form_ConsentTerms').checked;
        const consentErr = document.getElementById('consent-error');
        if (!consent) {
            consentErr.style.display = 'block';
            if (!firstErrEl) firstErrEl = consentErr;
            isValid = false;
        } else consentErr.style.display = 'none';

        const form = document.getElementById('patientForm');
        if (form) { if (isValid) delete form.dataset.invalid; else form.dataset.invalid = '1'; }

        if (!isValid && firstErrEl) {
            firstErrEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
            if (firstErrEl.tagName === 'INPUT' || firstErrEl.tagName === 'SELECT') firstErrEl.focus();
        }

        return isValid;
    };
}







//function initPatientDetailsPage() {

//    let lockedMode = null;          // 'new' | 'registered' once the type was chosen on the previous page
//    const timers = {};              // one countdown per OTP box (new / registered), they must not cancel each other

//    function onReady(fn) {
//        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
//        else fn();
//    }

//    onReady(() => {
//        loadTitles();
//        loadCountries();
//        prefillCommonFields();
//        limitDobToPast();

//        // Open the right section (also after a server-side validation failure)
//        switchMode(SavedPatientType === 'registered' ? 'registered' : 'new');
//        applyPatientTypeLock(SavedPatientType);

//        document.querySelectorAll('input, select, textarea').forEach(el => {
//            el.addEventListener('input', () => clearError(el.id));
//            el.addEventListener('change', () => clearError(el.id));
//        });

//        const form = document.getElementById('patientForm');
//        if (form) form.addEventListener('submit', () => {
//            // validateForm() already ran in onsubmit; only a valid form reaches this point
//            if (!form.dataset.invalid) setSubmitting(true);
//        });
//        window.addEventListener('pageshow', () => setSubmitting(false));   // coming back with the Back button
//    });

//    // ── Patient type: toggle + lock ──────────────────────
//    window.switchMode = function (mode) {
//        if (lockedMode && mode !== lockedMode) return;       // the other section is locked

//        const newForm = document.getElementById('new-patient-form');
//        const regForm = document.getElementById('registered-form');
//        const btnNew = document.getElementById('btn-new');
//        const btnReg = document.getElementById('btn-existing');
//        const isNew = mode === 'new';

//        newForm.style.display = isNew ? 'block' : 'none';
//        regForm.style.display = isNew ? 'none' : 'block';
//        btnNew.classList.toggle('active', isNew);
//        btnReg.classList.toggle('active', !isNew);
//        btnNew.setAttribute('aria-pressed', String(isNew));
//        btnReg.setAttribute('aria-pressed', String(!isNew));
//        document.getElementById('Form_PatientType').value = isNew ? 'new' : 'registered';
//    };

//    function applyPatientTypeLock(type) {
//        if (type !== 'new' && type !== 'registered') return;     // opened without a choice → both stay clickable
//        lockedMode = type;

//        const other = document.getElementById(type === 'new' ? 'btn-existing' : 'btn-new');
//        other.disabled = true;
//        other.setAttribute('aria-disabled', 'true');
//        other.title = 'Go back to change the patient type';
//        document.getElementById('patientToggle').classList.add('is-locked');

//        document.getElementById('toggleNoteType').textContent = type === 'new' ? 'a new patient' : 'an already registered patient';
//        document.getElementById('toggleNote').hidden = false;
//    }

//    function setSubmitting(on) {
//        const btn = document.getElementById('submitBtn');
//        if (!btn) return;
//        btn.disabled = on;
//        btn.textContent = on ? 'Please wait…' : 'Review & Confirm →';
//    }

//    function limitDobToPast() {
//        const dob = document.getElementById('Form_DOB');
//        if (!dob) return;
//        const y = new Date(); y.setDate(y.getDate() - 1);
//        dob.max = y.getFullYear() + '-' + String(y.getMonth() + 1).padStart(2, '0') + '-' + String(y.getDate()).padStart(2, '0');
//    }

//    // ── Common — Prefill from session ────────────────────
//    function prefillCommonFields() {
//        const verified = document.getElementById('Form_IsMobileVerified').value;
//        const mobile = document.getElementById('phone-input').value;
//        const btn = document.getElementById('send-otp-btn');

//        if (verified === 'true') {
//            document.getElementById('verify-badge').classList.add('show');
//            btn.textContent = 'OTP Sent ✓';
//            btn.classList.add('otp-sent');
//            btn.disabled = true;
//        } else if (mobile && mobile.length === 10) {
//            btn.disabled = false;
//        }

//        const regVerified = document.getElementById('Reg_IsMobileVerified').value;
//        if (regVerified === 'true') {
//            const regBtn = document.getElementById('reg-send-otp-btn');
//            regBtn.textContent = 'OTP Sent ✓';
//            regBtn.classList.add('otp-sent');
//            regBtn.disabled = true;
//            document.getElementById('reg-otp-verified').classList.add('show');

//            showPatientCard({
//                patientName: `${document.getElementById('Reg_TitleName').value} ${document.getElementById('Reg_FirstName').value} ${document.getElementById('Reg_LastName').value}`.trim(),
//                gender: document.getElementById('Reg_Gender').value,
//                dob: document.getElementById('Reg_DOB').value,
//                mobileNo: document.getElementById('reg-phone-input').value,
//                registrationNo: document.getElementById('Reg_RegistrationNo').value,
//                firstName: document.getElementById('Reg_FirstName').value,
//                lastName: document.getElementById('Reg_LastName').value
//            });
//        } else {
//            const regMobile = document.getElementById('reg-phone-input').value;
//            if (regMobile && regMobile.length === 10) document.getElementById('reg-send-otp-btn').disabled = false;
//        }

//        const savedGender = document.getElementById('Form_Gender').value;
//        if (savedGender) {
//            const gbtn = document.querySelector(`.gender-btn[data-value="${savedGender}"]`);
//            if (gbtn) gbtn.classList.add('sel');
//        }
//        document.getElementById('char-count').textContent =
//            (document.getElementById('Form_AppointmentNotes')?.value || '').length;
//    }

//    // ── Gender ───────────────────────────────────────────
//    window.selGender = function (el) {
//        document.querySelectorAll('.gender-btn').forEach(b => b.classList.remove('sel'));
//        el.classList.add('sel');
//        document.getElementById('Form_Gender').value = el.dataset.value;
//        document.getElementById('gender-error').style.display = 'none';
//    };

//    function clearGender() {
//        document.querySelectorAll('.gender-btn').forEach(b => b.classList.remove('sel'));
//        document.getElementById('Form_Gender').value = '';
//    }

//    function autoSelectGender(value) {
//        const btn = document.querySelector(`.gender-btn[data-value="${value}"]`);
//        if (btn) {
//            document.querySelectorAll('.gender-btn').forEach(b => b.classList.remove('sel'));
//            btn.classList.add('sel');
//            document.getElementById('Form_Gender').value = value;
//            document.getElementById('gender-error').style.display = 'none';
//        }
//    }

//    // ── Titles ───────────────────────────────────────────
//    function loadTitles() {
//        fetch('?handler=Titles')
//            .then(r => r.json())
//            .then(res => {
//                if (!res.success) return;

//                const select = document.getElementById('Form_Title');
//                select.innerHTML = '<option value="">--</option>';

//                res.data.forEach(t => {
//                    const opt = document.createElement('option');
//                    opt.value = t.name.trim();
//                    opt.text = t.name.trim();
//                    opt.dataset.gender = t.gender;
//                    opt.dataset.titleId = t.titleId;
//                    select.appendChild(opt);
//                });

//                $('#Form_Title').select2({ width: '100%', placeholder: '--', minimumResultsForSearch: Infinity });

//                if (SavedTitleName) {
//                    $('#Form_Title').val(SavedTitleName).trigger('change.select2');
//                    document.getElementById('Form_TitleId').value = SavedTitleId;
//                }

//                $('#Form_Title').off('change.titleHandler').on('change.titleHandler', function () {
//                    clearError('Form_Title');
//                    const opt = this.options[this.selectedIndex];
//                    document.getElementById('Form_TitleId').value = opt?.dataset?.titleId || '';

//                    const gender = opt?.dataset?.gender;
//                    if (gender === 'M') autoSelectGender('Male');
//                    else if (gender === 'F') autoSelectGender('Female');
//                    else clearGender();
//                });
//            })
//            .catch(err => console.error('Titles load failed:', err));
//    }

//    // ── Countries / States / Cities ──────────────────────
//    function loadCountries() {
//        fetch('?handler=Countries')
//            .then(r => r.json())
//            .then(res => {
//                if (!res.success) return;

//                const select = document.getElementById('Form_Country');
//                select.innerHTML = '<option value="">Select country</option>';

//                res.data.forEach(c => {
//                    const opt = document.createElement('option');
//                    opt.value = c.countryId;
//                    opt.text = c.countryName.trim();
//                    select.appendChild(opt);
//                });

//                $('#Form_Country').select2({ width: '100%', placeholder: 'Select country' });

//                if (SavedCountryName) {
//                    const opt = Array.from(select.options).find(o => o.text === SavedCountryName);
//                    if (opt) {
//                        $('#Form_Country').val(opt.value).trigger('change.select2');
//                        document.getElementById('Form_CountryId').value = opt.value;
//                        document.getElementById('Form_CountryName').value = SavedCountryName;
//                        loadStates(opt.value);
//                    }
//                } else {
//                    $('#Form_Country').val(DefaultCountryId).trigger('change.select2');
//                    document.getElementById('Form_CountryId').value = DefaultCountryId;
//                    loadStates(DefaultCountryId);
//                }

//                $('#Form_Country').off('change').on('change', function () {
//                    const countryId = this.value;
//                    document.getElementById('Form_CountryId').value = countryId;
//                    document.getElementById('Form_CountryName').value = this.options[this.selectedIndex]?.text || '';

//                    resetLocationDropdown('Form_State', 'Select state');
//                    resetLocationDropdown('Form_City', 'Select city');
//                    document.getElementById('Form_StateId').value = '';
//                    document.getElementById('Form_StateName').value = '';
//                    document.getElementById('Form_CityId').value = '';
//                    document.getElementById('Form_CityName').value = '';

//                    if (countryId) loadStates(countryId);
//                });
//            })
//            .catch(err => console.error('Countries load failed:', err));
//    }

//    function loadStates(countryId) {
//        const stateSelect = document.getElementById('Form_State');
//        stateSelect.innerHTML = '<option value="">Loading...</option>';

//        if ($.fn.select2 && $(stateSelect).data('select2')) $('#Form_State').select2('destroy');
//        stateSelect.disabled = true;

//        fetch(`?handler=States&countryId=${countryId}`)
//            .then(r => r.json())
//            .then(res => {
//                stateSelect.innerHTML = '<option value="">Select state</option>';

//                if (!res.success || res.data.length === 0) { stateSelect.disabled = true; return; }

//                res.data.forEach(s => {
//                    const opt = document.createElement('option');
//                    opt.value = s.stateId;
//                    opt.text = s.stateName.trim();
//                    stateSelect.appendChild(opt);
//                });

//                stateSelect.disabled = false;
//                $('#Form_State').select2({ width: '100%', placeholder: 'Select state' });

//                if (SavedStateId && SavedStateId !== '0') {
//                    $('#Form_State').val(SavedStateId).trigger('change.select2');
//                    document.getElementById('Form_StateId').value = SavedStateId;
//                    document.getElementById('Form_StateName').value = SavedStateName;
//                    loadCities(SavedStateId);
//                }

//                $('#Form_State').off('change.stateHandler').on('change.stateHandler', function () {
//                    document.getElementById('Form_StateId').value = this.value;
//                    document.getElementById('Form_StateName').value = this.options[this.selectedIndex]?.text || '';

//                    resetLocationDropdown('Form_City', 'Select city');
//                    document.getElementById('Form_CityId').value = '';
//                    document.getElementById('Form_CityName').value = '';

//                    if (this.value) loadCities(this.value);
//                });
//            })
//            .catch(() => {
//                stateSelect.innerHTML = '<option value="">Select state</option>';
//                stateSelect.disabled = true;
//            });
//    }

//    function loadCities(stateId) {
//        const citySelect = document.getElementById('Form_City');
//        citySelect.innerHTML = '<option value="">Loading...</option>';

//        if ($.fn.select2 && $(citySelect).data('select2')) $('#Form_City').select2('destroy');
//        citySelect.disabled = true;

//        fetch(`?handler=Cities&stateId=${stateId}`)
//            .then(r => r.json())
//            .then(res => {
//                citySelect.innerHTML = '<option value="">Select city</option>';

//                if (!res.success || res.data.length === 0) { citySelect.disabled = true; return; }

//                res.data.forEach(c => {
//                    const opt = document.createElement('option');
//                    opt.value = c.cityId;
//                    opt.text = c.cityName.trim();
//                    citySelect.appendChild(opt);
//                });

//                citySelect.disabled = false;
//                $('#Form_City').select2({ width: '100%', placeholder: 'Select city' });

//                if (SavedCityId && SavedCityId !== '0') {
//                    $('#Form_City').val(SavedCityId).trigger('change.select2');
//                    document.getElementById('Form_CityId').value = SavedCityId;
//                    document.getElementById('Form_CityName').value = SavedCityName;
//                }

//                $('#Form_City').off('change.cityHandler').on('change.cityHandler', function () {
//                    document.getElementById('Form_CityId').value = this.value;
//                    document.getElementById('Form_CityName').value = this.options[this.selectedIndex]?.text || '';
//                });
//            })
//            .catch(() => {
//                citySelect.innerHTML = '<option value="">Select city</option>';
//                citySelect.disabled = true;
//            });
//    }

//    function resetLocationDropdown(id, placeholder) {
//        if ($.fn.select2 && $('#' + id).data('select2')) $('#' + id).select2('destroy');
//        const el = document.getElementById(id);
//        el.innerHTML = `<option value="">${placeholder}</option>`;
//        el.disabled = true;
//    }

//    // ── OTP helpers shared by both sections ──────────────
//    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;

//    function postJson(handler, body) {
//        return fetch('?handler=' + handler, {
//            method: 'POST',
//            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
//            body: JSON.stringify(body)
//        }).then(r => r.json());
//    }

//    function buildOtpInputs(containerId, type) {
//        const container = document.getElementById(containerId);
//        container.innerHTML = '';

//        for (let i = 1; i <= OTP_LENGTH; i++) {
//            const input = document.createElement('input');
//            input.className = 'otp-input';
//            input.type = 'text';
//            input.maxLength = 1;
//            input.id = type + 'o' + i;
//            input.inputMode = 'numeric';
//            input.setAttribute('aria-label', `OTP digit ${i}`);
//            if (i === 1) input.autocomplete = 'one-time-code';

//            const nextId = i < OTP_LENGTH ? type + 'o' + (i + 1) : null;
//            const prevId = i > 1 ? type + 'o' + (i - 1) : null;

//            input.addEventListener('input', function () {
//                this.value = this.value.replace(/\D/g, '');
//                if (this.value.length === 1 && nextId) document.getElementById(nextId)?.focus();
//                checkOtpComplete(containerId, type);
//            });

//            input.addEventListener('keydown', function (e) {
//                if (e.key === 'Backspace' && this.value === '' && prevId) document.getElementById(prevId)?.focus();
//            });

//            // Paste (or SMS autofill) of the whole code into any box
//            input.addEventListener('paste', function (e) {
//                const digits = ((e.clipboardData || window.clipboardData)?.getData('text') || '').replace(/\D/g, '').slice(0, OTP_LENGTH);
//                if (!digits) return;
//                e.preventDefault();
//                container.querySelectorAll('.otp-input').forEach((el, idx) => { el.value = digits[idx] || ''; });
//                container.querySelectorAll('.otp-input')[Math.min(digits.length, OTP_LENGTH) - 1]?.focus();
//                checkOtpComplete(containerId, type);
//            });

//            container.appendChild(input);
//        }
//        document.getElementById(type + 'o1')?.focus();
//    }

//    function checkOtpComplete(containerId, type) {
//        const inputs = document.querySelectorAll(`#${containerId} .otp-input`);
//        const otp = Array.from(inputs).map(i => i.value).join('');
//        if (otp.length === OTP_LENGTH) {
//            if (type === 'new') verifyOtp(otp);
//            else verifyRegisteredOtp(otp);
//        }
//    }

//    function startTimer(displayId, resendBtnId) {
//        clearInterval(timers[displayId]);
//        let seconds = 30;
//        const display = document.getElementById(displayId);
//        const resendBtn = document.getElementById(resendBtnId);
//        resendBtn.disabled = true;
//        display.textContent = `Resend in 0:${String(seconds).padStart(2, '0')}`;

//        timers[displayId] = setInterval(() => {
//            seconds--;
//            display.textContent = seconds > 0 ? `Resend in 0:${String(seconds).padStart(2, '0')}` : '';
//            if (seconds <= 0) {
//                clearInterval(timers[displayId]);
//                resendBtn.disabled = false;
//            }
//        }, 1000);
//    }

//    // ── New patient — phone & OTP ────────────────────────
//    window.checkPhone = function () {
//        const val = document.getElementById('phone-input').value.replace(/\D/g, '');
//        document.getElementById('phone-input').value = val;
//        const btn = document.getElementById('send-otp-btn');

//        if (btn.classList.contains('otp-sent')) {
//            btn.classList.remove('otp-sent');
//            btn.textContent = 'Send OTP';
//            document.getElementById('otp-section').classList.remove('visible');
//            document.getElementById('otp-verified').classList.remove('show');
//            document.getElementById('verify-badge').classList.remove('show');
//            document.getElementById('new-otp-inputs').innerHTML = '';
//            document.getElementById('Form_IsMobileVerified').value = 'false';
//            clearInterval(timers['timer-display']);
//            clearOtpError('new');
//        }

//        btn.disabled = val.length !== 10;
//        clearError('phone-input');
//    };

//    window.sendOTP = function () {
//        const mobile = document.getElementById('phone-input').value;
//        const btn = document.getElementById('send-otp-btn');

//        if (!mobile || mobile.length !== 10) {
//            showError('phone-input', 'Please enter a valid 10-digit mobile number');
//            return;
//        }

//        btn.disabled = true;
//        btn.textContent = 'Sending...';

//        postJson('SendOtp', { mobileNo: mobile })
//            .then(data => {
//                if (data.success) {
//                    btn.textContent = 'OTP Sent ✓';
//                    btn.classList.add('otp-sent');
//                    document.getElementById('otp-phone-display').textContent = mobile;
//                    document.getElementById('otp-section').classList.add('visible');
//                    buildOtpInputs('new-otp-inputs', 'new');
//                    startTimer('timer-display', 'resend-btn');
//                    clearError('phone-input');
//                } else {
//                    showError('phone-input', data.message || 'Failed to send OTP');
//                    btn.disabled = false;
//                    btn.textContent = 'Send OTP';
//                }
//            })
//            .catch(() => {
//                showError('phone-input', 'Something went wrong. Please try again.');
//                btn.disabled = false;
//                btn.textContent = 'Send OTP';
//            });
//    };

//    function verifyOtp(otp) {
//        const mobile = document.getElementById('phone-input').value;

//        postJson('VerifyOtp', { mobileNo: mobile, otpNo: otp })
//            .then(data => {
//                if (data.success) {
//                    document.getElementById('otp-verified').classList.add('show');
//                    document.getElementById('verify-badge').classList.add('show');
//                    document.querySelectorAll('#new-otp-inputs .otp-input').forEach(i => i.disabled = true);
//                    document.getElementById('Form_IsMobileVerified').value = 'true';
//                    document.getElementById('Form_Mobile').value = mobile;
//                    clearOtpError('new');
//                } else {
//                    document.querySelectorAll('#new-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
//                    document.getElementById('newo1')?.focus();
//                    showOtpError('new-otp-inputs', data.message || 'Invalid OTP. Please try again.');
//                }
//            })
//            .catch(() => showOtpError('new-otp-inputs', 'Something went wrong. Please try again.'));
//    }

//    window.resendOTP = function () {
//        document.querySelectorAll('#new-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
//        document.getElementById('otp-verified').classList.remove('show');
//        document.getElementById('Form_IsMobileVerified').value = 'false';
//        clearOtpError('new');

//        const btn = document.getElementById('send-otp-btn');
//        btn.disabled = false;
//        btn.textContent = 'Send OTP';
//        btn.classList.remove('otp-sent');
//        sendOTP();
//    };

//    // ── Registered patient — phone & OTP ─────────────────
//    window.checkRegPhone = function () {
//        const val = document.getElementById('reg-phone-input').value.replace(/\D/g, '');
//        document.getElementById('reg-phone-input').value = val;
//        const btn = document.getElementById('reg-send-otp-btn');

//        if (btn.classList.contains('otp-sent')) {
//            btn.classList.remove('otp-sent');
//            btn.textContent = 'Send OTP';
//            document.getElementById('reg-otp-section').classList.remove('visible');
//            document.getElementById('reg-otp-verified').classList.remove('show');
//            document.getElementById('reg-otp-inputs').innerHTML = '';
//            document.getElementById('Reg_IsMobileVerified').value = 'false';
//            document.getElementById('patient-found-card').style.display = 'none';
//            clearInterval(timers['reg-timer-display']);
//            clearOtpError('reg');
//        }

//        btn.disabled = val.length !== 10;
//        clearError('reg-phone-input');
//    };

//    window.sendRegisteredOTP = function () {
//        const mobile = document.getElementById('reg-phone-input').value;
//        const btn = document.getElementById('reg-send-otp-btn');

//        if (!mobile || mobile.length !== 10) {
//            showError('reg-phone-input', 'Please enter a valid 10-digit mobile number');
//            return;
//        }

//        btn.disabled = true;
//        btn.textContent = 'Sending...';

//        postJson('SendRegisteredOtp', { mobileNo: mobile })
//            .then(data => {
//                if (data.success) {
//                    btn.textContent = 'OTP Sent ✓';
//                    btn.classList.add('otp-sent');
//                    document.getElementById('reg-otp-phone-display').textContent = mobile;
//                    document.getElementById('reg-otp-section').classList.add('visible');
//                    buildOtpInputs('reg-otp-inputs', 'reg');
//                    startTimer('reg-timer-display', 'reg-resend-btn');
//                    clearError('reg-phone-input');
//                } else {
//                    showError('reg-phone-input', data.message || 'Failed to send OTP');
//                    btn.disabled = false;
//                    btn.textContent = 'Send OTP';
//                }
//            })
//            .catch(() => {
//                showError('reg-phone-input', 'Something went wrong. Please try again.');
//                btn.disabled = false;
//                btn.textContent = 'Send OTP';
//            });
//    };

//    function verifyRegisteredOtp(otp) {
//        const mobile = document.getElementById('reg-phone-input').value;

//        postJson('VerifyRegisteredOtp', { mobileNo: mobile, otpNo: otp })
//            .then(data => {
//                if (data.success) {
//                    document.getElementById('reg-otp-verified').classList.add('show');
//                    document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => i.disabled = true);
//                    document.getElementById('Reg_IsMobileVerified').value = 'true';
//                    clearOtpError('reg');
//                    showPatientCard(data.patient);
//                    fillRegisteredHiddenFields(data.patient);
//                } else {
//                    document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
//                    document.getElementById('rego1')?.focus();
//                    showOtpError('reg-otp-inputs', data.message || 'Invalid OTP. Please try again.');
//                }
//            })
//            .catch(() => showOtpError('reg-otp-inputs', 'Something went wrong. Please try again.'));
//    }

//    function fillRegisteredHiddenFields(patient) {
//        document.getElementById('Reg_GuestPatientId').value = patient.guestPatientId || '';
//        document.getElementById('Reg_RegistrationNo').value = patient.registrationNo || '0';
//        document.getElementById('Reg_TitleName').value = patient.titleName || '';
//        document.getElementById('Reg_FirstName').value = patient.firstName || '';
//        document.getElementById('Reg_MiddleName').value = patient.middleName || '';
//        document.getElementById('Reg_LastName').value = patient.lastName || '';
//        document.getElementById('Reg_Gender').value = patient.gender || '';
//        document.getElementById('Reg_DOB').value = patient.dob || '';
//        document.getElementById('Reg_Email').value = patient.emailId || '';
//    }

//    function showPatientCard(patient) {
//        const card = document.getElementById('patient-found-card');
//        card.style.display = 'block';

//        const initials = (patient.firstName?.[0] || '') + (patient.lastName?.[0] || '');
//        document.getElementById('pf-avatar').textContent = initials || 'P';
//        document.getElementById('pf-name').textContent = patient.patientName || `${patient.firstName} ${patient.lastName}`.trim();

//        const uhid = (patient.registrationNo && patient.registrationNo !== '0') ? `UHID: ${patient.registrationNo}` : 'UHID: 0';
//        document.getElementById('pf-details').textContent =
//            `${patient.gender} · ${formatDob(patient.dob)} · ${patient.mobileNo} · ${uhid}`;
//    }

//    function formatDob(dob) {
//        if (!dob) return '';
//        const d = new Date(dob);
//        return isNaN(d.getTime()) ? dob : d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
//    }

//    window.resendRegisteredOTP = function () {
//        document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
//        document.getElementById('reg-otp-verified').classList.remove('show');
//        document.getElementById('Reg_IsMobileVerified').value = 'false';
//        document.getElementById('patient-found-card').style.display = 'none';
//        clearOtpError('reg');

//        const btn = document.getElementById('reg-send-otp-btn');
//        btn.disabled = false;
//        btn.textContent = 'Send OTP';
//        btn.classList.remove('otp-sent');
//        sendRegisteredOTP();
//    };

//    // ── Notes counter ────────────────────────────────────
//    window.countChars = function (el) {
//        document.getElementById('char-count').textContent = el.value.length;
//    };

//    // ── Error helpers ────────────────────────────────────
//    window.showError = function (fieldId, message) {
//        clearError(fieldId);
//        const field = document.getElementById(fieldId);
//        if (!field) return;
//        field.classList.add('input-error');
//        const err = document.createElement('span');
//        err.className = 'field-error';
//        err.id = fieldId + '_err';
//        err.setAttribute('role', 'alert');
//        err.textContent = message;
//        field.closest('.form-group')
//            ? field.closest('.form-group').appendChild(err)
//            : field.insertAdjacentElement('afterend', err);
//    };

//    window.clearError = function (fieldId) {
//        const field = document.getElementById(fieldId);
//        if (field) field.classList.remove('input-error');
//        const err = document.getElementById(fieldId + '_err');
//        if (err) err.remove();
//    };

//    function showOtpError(containerId, message) {
//        const type = containerId.includes('reg') ? 'reg' : 'new';
//        clearOtpError(type);
//        const err = document.createElement('div');
//        err.className = 'field-error mt-1';
//        err.id = type + '-otp-error';
//        err.textContent = message;
//        document.getElementById(containerId).after(err);
//    }

//    function clearOtpError(type) {
//        const err = document.getElementById(type + '-otp-error');
//        if (err) err.remove();
//    }

//    function closeServerErrorModal() {
//        const modal = document.getElementById('serverErrorModal');
//        if (modal) modal.style.display = 'none';
//    }
//    window.closeServerErrorModal = closeServerErrorModal;

//    document.addEventListener('keydown', function (e) {
//        if (e.key === 'Escape') closeServerErrorModal();
//    });

//    // ── Validation ───────────────────────────────────────
//    const nameRegex = /^[a-zA-Z\s'\-]+$/;
//    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

//    // Remove every inline message from the previous attempt, so only problems that exist *now* are shown
//    function clearAllErrors() {
//        document.querySelectorAll('.field-error[id$="_err"]').forEach(e => e.remove());
//        document.querySelectorAll('.input-error').forEach(e => e.classList.remove('input-error'));
//        document.getElementById('gender-error').style.display = 'none';
//        document.getElementById('consent-error').style.display = 'none';
//    }

//    window.validateForm = function () {
//        clearAllErrors();
//        let isValid = true;
//        let firstErrEl = null;

//        const patientType = document.getElementById('Form_PatientType').value;

//        if (patientType === 'new' || !patientType) {
//            const checks = [
//                ['Form_Title', document.getElementById('Form_Title').value, 'Please select a title'],
//                ['Form_FirstName', document.getElementById('Form_FirstName').value.trim(), 'First name is required'],
//                ['Form_LastName', document.getElementById('Form_LastName').value.trim(), 'Last name is required'],
//                ['Form_DOB', document.getElementById('Form_DOB').value, 'Date of birth is required']
//            ];

//            checks.forEach(([id, val, msg]) => {
//                if (!val) {
//                    showError(id, msg);
//                    if (!firstErrEl) firstErrEl = document.getElementById(id);
//                    isValid = false;
//                } else clearError(id);
//            });

//            const firstName = document.getElementById('Form_FirstName').value.trim();
//            if (firstName && !nameRegex.test(firstName)) {
//                showError('Form_FirstName', 'First name should contain letters only');
//                if (!firstErrEl) firstErrEl = document.getElementById('Form_FirstName');
//                isValid = false;
//            }

//            const lastName = document.getElementById('Form_LastName').value.trim();
//            if (lastName && !nameRegex.test(lastName)) {
//                showError('Form_LastName', 'Last name should contain letters only');
//                if (!firstErrEl) firstErrEl = document.getElementById('Form_LastName');
//                isValid = false;
//            }

//            const middleName = document.getElementById('Form_MiddleName').value.trim();
//            if (middleName && !nameRegex.test(middleName)) {
//                showError('Form_MiddleName', 'Middle name should contain letters only');
//                if (!firstErrEl) firstErrEl = document.getElementById('Form_MiddleName');
//                isValid = false;
//            }

//            const dob = document.getElementById('Form_DOB').value;
//            if (dob) {
//                const dobDate = new Date(dob);
//                const today = new Date(); today.setHours(0, 0, 0, 0);
//                if (dobDate >= today) {
//                    showError('Form_DOB', 'Date of birth cannot be today or a future date');
//                    if (!firstErrEl) firstErrEl = document.getElementById('Form_DOB');
//                    isValid = false;
//                }
//            }

//            const gender = document.getElementById('Form_Gender').value;
//            const genderErr = document.getElementById('gender-error');
//            if (!gender) {
//                genderErr.style.display = 'block';
//                if (!firstErrEl) firstErrEl = genderErr;
//                isValid = false;
//            } else genderErr.style.display = 'none';

//            const email = document.getElementById('Form_Email')?.value?.trim();
//            if (email && !emailRegex.test(email)) {
//                showError('Form_Email', 'Please enter a valid email address');
//                if (!firstErrEl) firstErrEl = document.getElementById('Form_Email');
//                isValid = false;
//            }

//            const pincode = document.getElementById('Form_Pincode')?.value?.trim();
//            if (pincode && !/^\d{6}$/.test(pincode)) {
//                showError('Form_Pincode', 'Pincode must be exactly 6 digits');
//                if (!firstErrEl) firstErrEl = document.getElementById('Form_Pincode');
//                isValid = false;
//            }

//            const mobile = document.getElementById('phone-input').value.trim();
//            const verified = document.getElementById('Form_IsMobileVerified').value;
//            if (!mobile) {
//                showError('phone-input', 'Mobile number is required');
//                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
//                isValid = false;
//            } else if (!/^\d{10}$/.test(mobile)) {
//                showError('phone-input', 'Please enter a valid 10-digit mobile number');
//                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
//                isValid = false;
//            } else if (verified !== 'true') {
//                showError('phone-input', 'Please verify your mobile number via OTP');
//                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
//                isValid = false;
//            }
//        } else if (patientType === 'registered') {
//            const mobile = document.getElementById('reg-phone-input').value.trim();
//            const verified = document.getElementById('Reg_IsMobileVerified').value;

//            if (!mobile) {
//                showError('reg-phone-input', 'Mobile number is required');
//                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
//                isValid = false;
//            } else if (!/^\d{10}$/.test(mobile)) {
//                showError('reg-phone-input', 'Please enter a valid 10-digit mobile number');
//                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
//                isValid = false;
//            } else if (verified !== 'true') {
//                showError('reg-phone-input', 'Please verify your mobile number via OTP');
//                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
//                isValid = false;
//            }
//        }

//        const consent = document.getElementById('Form_ConsentTerms').checked;
//        const consentErr = document.getElementById('consent-error');
//        if (!consent) {
//            consentErr.style.display = 'block';
//            if (!firstErrEl) firstErrEl = consentErr;
//            isValid = false;
//        } else consentErr.style.display = 'none';

//        const form = document.getElementById('patientForm');
//        if (form) { if (isValid) delete form.dataset.invalid; else form.dataset.invalid = '1'; }

//        if (!isValid && firstErrEl) {
//            firstErrEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
//            if (firstErrEl.tagName === 'INPUT' || firstErrEl.tagName === 'SELECT') firstErrEl.focus();
//        }

//        return isValid;
//    };
//}
