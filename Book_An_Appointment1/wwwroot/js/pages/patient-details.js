function initPatientDetailsPage() {

    const timers = {};              // one countdown per OTP box (new / registered), they must not cancel each other
    let currentMode = null;         // 'new' | 'registered' — the open section
    let titleList = [];             // titles from ?handler=Titles (new patient form + family member form)

    // Both OTP boxes run the same flow; only element ids / handler differ
    const OTP_BOX = {
        new: {
            type: 'new', switchTo: 'registered', handler: 'SendOtp', otpType: 'new',
            input: 'phone-input', button: 'send-otp-btn', phoneDisplay: 'otp-phone-display',
            section: 'otp-section', inputs: 'new-otp-inputs', timer: 'timer-display', resend: 'resend-btn'
        },
        registered: {
            type: 'registered', switchTo: 'new', handler: 'SendRegisteredOtp', otpType: 'reg',
            input: 'reg-phone-input', button: 'reg-send-otp-btn', phoneDisplay: 'reg-otp-phone-display',
            section: 'reg-otp-section', inputs: 'reg-otp-inputs', timer: 'reg-timer-display', resend: 'reg-resend-btn'
        }
    };

    function onReady(fn) {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
    }

    onReady(() => {
        loadTitles();
        loadCountries();
        prefillCommonFields();
        limitDobToPast();
        rememberExtrasOnChange();
        initFamilyMemberForm();

        // Open the section chosen on page 1 (also after a server-side validation failure)
        applyMode(SavedPatientType === 'registered' ? 'registered' : 'new');

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

    // ── Patient type: locked (New / Already registered) ──
    // The type comes from page 1 and the patient cannot toggle it. It only changes through the
    // "Continue as …" button, after the OTP API says the number belongs to the other type.
    function applyMode(mode) {
        const isNew = mode === 'new';
        currentMode = mode;

        document.getElementById('new-patient-form').style.display = isNew ? 'block' : 'none';
        document.getElementById('registered-form').style.display = isNew ? 'none' : 'block';

        [['btn-new', isNew], ['btn-existing', !isNew]].forEach(([id, active]) => {
            const btn = document.getElementById(id);
            btn.classList.toggle('active', active);
            btn.setAttribute('aria-pressed', String(active));
            btn.disabled = !active;                       // the other type stays locked
            btn.setAttribute('aria-disabled', String(!active));
        });
        document.getElementById('patientToggle').classList.add('is-locked');
        document.getElementById('Form_PatientType').value = mode;
    }

    // Used only by the "Continue as …" hint. The server accepts it only right after the OTP API
    // reported this number as the other type (submit + Review page trust the session type).
    // Resolves true when the switch is saved.
    function switchPatientType(mode) {
        clearError('patientToggle');
        return postJson('SwitchPatientType', { patientType: mode })
            .then(data => {
                if (data.success) { applyMode(mode); return true; }
                if (data.redirectUrl) { window.location.href = data.redirectUrl; return false; }
                throw new Error(data.message);
            })
            .catch(() => {
                showError('patientToggle', t('Js.SomethingWrong'));
                return false;
            });
    }

    function setSubmitting(on) {
        const btn = document.getElementById('submitBtn');
        if (!btn) return;
        btn.disabled = on;
        btn.textContent = on ? t('Js.PleaseWait') : t('Js.ReviewConfirm');
    }

    function limitDobToPast() {
        const dob = document.getElementById('Form_DOB');
        if (!dob) return;
        const y = new Date(); y.setDate(y.getDate() - 1);
        dob.max = y.getFullYear() + '-' + String(y.getMonth() + 1).padStart(2, '0') + '-' + String(y.getDate()).padStart(2, '0');
    }

    // ── Common — Prefill from session ────────────────────
    function prefillCommonFields() {
        const verified = document.getElementById('Form_IsMobileVerified').value.toLowerCase();
        const mobile = document.getElementById('phone-input').value;
        const btn = document.getElementById('send-otp-btn');

        if (verified === 'true') {
            document.getElementById('verify-badge').classList.add('show');
            btn.textContent = t('Js.OtpSent');
            btn.classList.add('otp-sent');
            btn.disabled = true;
        } else if (mobile && mobile.length === 10) {
            btn.disabled = false;
        }

        const regVerified = document.getElementById('Reg_IsMobileVerified').value.toLowerCase();
        if (regVerified === 'true') {
            const regBtn = document.getElementById('reg-send-otp-btn');
            regBtn.textContent = t('Js.OtpSent');
            regBtn.classList.add('otp-sent');
            regBtn.disabled = true;
            document.getElementById('reg-otp-verified').classList.add('show');

            // Back / validation failure — rebuild the family cards from session, keep the selection
            renderPatientCards(window.SavedRegisteredPatients || [], window.SavedSelectedPatient ?? -1);
        } else {
            const regMobile = document.getElementById('reg-phone-input').value;
            if (regMobile && regMobile.length === 10) document.getElementById('reg-send-otp-btn').disabled = false;
        }

        const savedGender = document.getElementById('Form_Gender').value;
        if (savedGender) {
            const gbtn = document.querySelector(`#np-gender .gender-btn[data-value="${savedGender}"]`);
            if (gbtn) gbtn.classList.add('sel');
        }
        document.getElementById('char-count').textContent =
            (document.getElementById('Form_AppointmentNotes')?.value || '').length;
    }

    // ── Gender ───────────────────────────────────────────
    // New-patient gender buttons only (the family form has its own row, #fm-gender)
    const npGenderBtns = () => document.querySelectorAll('#np-gender .gender-btn');

    window.selGender = function (el) {
        if (el.disabled) return;
        npGenderBtns().forEach(b => b.classList.remove('sel'));
        el.classList.add('sel');
        document.getElementById('Form_Gender').value = el.dataset.value;
        document.getElementById('gender-error').style.display = 'none';
    };

    function autoSelectGender(value) {
        const btn = document.querySelector(`#np-gender .gender-btn[data-value="${value}"]`);
        if (btn) {
            npGenderBtns().forEach(b => b.classList.remove('sel'));
            btn.classList.add('sel');
            document.getElementById('Form_Gender').value = value;
            document.getElementById('gender-error').style.display = 'none';
        }
    }

    // Title master "Gender": M → only Male, F → only Female, B (Dr., Baby Of) or no title → all three.
    // The other buttons are disabled; returns the one allowed gender (or null when any is allowed).
    window.lockGenderToTitle = function (row, titleGender) {
        const only = titleGender === 'M' ? 'Male' : titleGender === 'F' ? 'Female' : null;
        row.querySelectorAll('.gender-btn').forEach(b => {
            const off = only !== null && b.dataset.value !== only;
            b.disabled = off;
            b.setAttribute('aria-disabled', String(off));
            b.title = off ? t('Validation.GenderTitleMismatch') : '';
        });
        return only;
    };

    function applyTitleGender(gender) {
        const only = lockGenderToTitle(document.getElementById('np-gender'), gender);
        if (only) autoSelectGender(only);   // B / no title: the patient's own choice is kept
    }

    // ── Titles ───────────────────────────────────────────
    function loadTitles() {
        fetch('?handler=Titles')
            .then(r => r.json())
            .then(res => {
                if (!res.success) return;
                titleList = res.data || [];          // reused by the "Add family member" form

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

                $('#Form_Title').select2({ width: '100%', placeholder: '--', minimumResultsForSearch: Infinity, language: AppI18n.select2Language() });

                if (SavedTitleName) {
                    $('#Form_Title').val(SavedTitleName).trigger('change.select2');
                    document.getElementById('Form_TitleId').value = SavedTitleId;
                    // Reload / Back: lock the buttons again for the saved title
                    applyTitleGender(select.options[select.selectedIndex]?.dataset?.gender);
                }

                $('#Form_Title').off('change.titleHandler').on('change.titleHandler', function () {
                    clearError('Form_Title');
                    const opt = this.options[this.selectedIndex];
                    document.getElementById('Form_TitleId').value = opt?.dataset?.titleId || '';
                    applyTitleGender(opt?.dataset?.gender);
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

                $('#Form_Country').select2({ width: '100%', placeholder: t('Js.SelectCountry'), language: AppI18n.select2Language() });

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

                    resetLocationDropdown('Form_State', t('Js.SelectState'));
                    resetLocationDropdown('Form_City', t('Js.SelectCity'));
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
                $('#Form_State').select2({ width: '100%', placeholder: t('Js.SelectState'), language: AppI18n.select2Language() });

                if (SavedStateId && SavedStateId !== '0') {
                    $('#Form_State').val(SavedStateId).trigger('change.select2');
                    document.getElementById('Form_StateId').value = SavedStateId;
                    document.getElementById('Form_StateName').value = SavedStateName;
                    loadCities(SavedStateId);
                }

                $('#Form_State').off('change.stateHandler').on('change.stateHandler', function () {
                    document.getElementById('Form_StateId').value = this.value;
                    document.getElementById('Form_StateName').value = this.options[this.selectedIndex]?.text || '';

                    resetLocationDropdown('Form_City', t('Js.SelectCity'));
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
                $('#Form_City').select2({ width: '100%', placeholder: t('Js.SelectCity'), language: AppI18n.select2Language() });

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
        el.innerHTML = '';
        el.appendChild(new Option(placeholder, ''));
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
            input.setAttribute('aria-label', t('Js.OtpDigit', i));
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
        display.textContent = t('Js.ResendIn', '0:' + String(seconds).padStart(2, '0'));

        timers[displayId] = setInterval(() => {
            seconds--;
            display.textContent = seconds > 0 ? t('Js.ResendIn', '0:' + String(seconds).padStart(2, '0')) : '';
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
            btn.textContent = t('Js.SendOtp');
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

    // ── Send OTP (shared by both patient types) ──────────
    function sendOtpFor(box) {
        const mobile = document.getElementById(box.input).value;
        const btn = document.getElementById(box.button);

        if (!mobile || mobile.length !== 10) {
            showError(box.input, t('Validation.MobileInvalid'));
            return;
        }

        clearError(box.input);
        btn.disabled = true;
        btn.textContent = t('Js.Sending');

        postJson(box.handler, { mobileNo: mobile })
            .then(data => {
                if (data.success) {
                    btn.textContent = t('Js.OtpSent');
                    btn.classList.add('otp-sent');
                    document.getElementById(box.phoneDisplay).textContent = mobile;
                    document.getElementById(box.section).classList.add('visible');
                    buildOtpInputs(box.inputs, box.otpType);
                    startTimer(box.timer, box.resend);
                    return;
                }
                resetSendButton(btn);
                if (data.wrongType) showSwitchHint(box, mobile, data.message);
                else showError(box.input, data.message || t('Errors.OtpSendFailed'));
            })
            .catch(() => {
                resetSendButton(btn);
                showError(box.input, t('Js.SomethingWrong'));
            });
    }

    function resetSendButton(btn) {
        btn.disabled = false;
        btn.textContent = t('Js.SendOtp');
    }

    // Number belongs to the other patient type (e.g. "not registered") — the only way to change type:
    // one click switches (and locks) the section, carries the number across and sends the OTP there.
    function showSwitchHint(box, mobile, message) {
        const target = OTP_BOX[box.switchTo];
        const field = document.getElementById(box.input);

        clearError(box.input);
        field.classList.add('input-error');

        const hint = el('div', 'switch-hint');
        hint.id = box.input + '_err';                 // removed by clearError() as soon as the number is edited
        hint.setAttribute('role', 'alert');
        hint.appendChild(el('span', 'switch-hint-text', message));

        const action = el('button', 'switch-hint-btn', t(target.type === 'new' ? 'Js.ContinueAsNew' : 'Js.ContinueAsRegistered'));
        action.type = 'button';
        action.addEventListener('click', () => {
            action.disabled = true;
            switchPatientType(target.type).then(saved => {
                if (!saved) { action.disabled = false; return; }
                clearError(box.input);

                const targetInput = document.getElementById(target.input);
                targetInput.value = mobile;
                (target.type === 'new' ? window.checkPhone : window.checkRegPhone)();   // reset that box for this number
                sendOtpFor(target);
            });
        });
        hint.appendChild(action);

        field.closest('.phone-row').insertAdjacentElement('afterend', hint);
    }

    window.sendOTP = () => sendOtpFor(OTP_BOX.new);

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
                    showOtpError('new-otp-inputs', data.message || t('Errors.InvalidOtpRetry'));
                }
            })
            .catch(() => showOtpError('new-otp-inputs', t('Js.SomethingWrong')));
    }

    window.resendOTP = function () {
        document.querySelectorAll('#new-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
        document.getElementById('otp-verified').classList.remove('show');
        document.getElementById('Form_IsMobileVerified').value = 'false';
        clearOtpError('new');

        const btn = document.getElementById('send-otp-btn');
        btn.disabled = false;
        btn.textContent = t('Js.SendOtp');
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
            btn.textContent = t('Js.SendOtp');
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

    window.sendRegisteredOTP = () => sendOtpFor(OTP_BOX.registered);

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
                    renderPatientCards(data.patientList || [], data.selected ? data.selected.selectedIndex : -1);
                } else {
                    document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
                    document.getElementById('rego1')?.focus();
                    showOtpError('reg-otp-inputs', data.message || t('Errors.InvalidOtpRetry'));
                }
            })
            .catch(() => showOtpError('reg-otp-inputs', t('Js.SomethingWrong')));
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
    // selectedIndex = position in the list (not the UHID: unregistered members all have UHID "0")
    function renderPatientCards(patients, selectedIndex) {
        const wrap = document.getElementById('patient-found-card');
        const list = document.getElementById('pf-list');
        list.innerHTML = '';
        hidePatientError();

        // Verified mobile with nobody on it yet → only "Add family member" is offered
        wrap.style.display = 'block';

        document.getElementById('pf-list-sub').textContent = !patients.length
            ? t('Js.NoFamilyMembers')
            : patients.length === 1
                ? t('Js.OnePatientFound')
                : t('Js.PatientsFound', patients.length);

        patients.forEach((p, i) => list.appendChild(buildPatientCard(p, i, i === selectedIndex)));
    }

    function buildPatientCard(p, index, selected) {
        const name = cleanName(p.patientName);
        const uhid = uhidText(p.registrationNo);
        const age = (p.age || '').split('/')[0].trim();          // "31 Yrs/Male" → "31 Yrs"

        const card = el('button', 'pf-card' + (selected ? ' selected' : ''));
        card.type = 'button';
        card.setAttribute('role', 'radio');
        card.setAttribute('aria-checked', selected ? 'true' : 'false');
        card.dataset.index = index;
        card.addEventListener('click', () => selectRegisteredPatient(card));

        card.appendChild(el('span', 'pf-card-avatar', initialsOf(name)));

        const body = el('span', 'pf-card-body');
        body.appendChild(el('span', 'pf-card-name', name));

        const tags = el('span', 'pf-card-tags');
        if (p.gender) tags.appendChild(el('span', 'pf-tag', genderText(p.gender)));
        if (p.dob) tags.appendChild(el('span', 'pf-tag', formatDob(p.dob)));
        body.appendChild(tags);

        const uhidRow = el('span', 'pf-card-uhid');
        uhidRow.appendChild(el('span', 'pf-uhid-label', t('Js.Uhid')));
        uhidRow.appendChild(el('span', 'pf-uhid-value', uhid || '—'));
        body.appendChild(uhidRow);
        card.appendChild(body);

        card.appendChild(el('span', 'pf-card-check', '✓'));

        // Full details — visible only on hover / keyboard focus (and for the selected card on touch screens)
        const more = el('span', 'pf-card-more');
        [[t('Js.FullName'), name], [t('Js.Gender'), genderText(p.gender)], [t('Js.DateOfBirth'), formatDob(p.dob)], [t('Js.Age'), age],
            [t('Js.Uhid'), uhid], [t('Js.Email'), p.emailId]]
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
        postJson('SelectRegisteredPatient', { index: Number(card.dataset.index) })
            .then(data => {
                if (!data.success) { showPatientError(data.message || t('Errors.PatientSelectFailed')); return; }
                list.querySelectorAll('.pf-card').forEach(c => {
                    c.classList.toggle('selected', c === card);
                    c.setAttribute('aria-checked', c === card ? 'true' : 'false');
                });
                fillRegisteredHiddenFields(data.selected);
                hidePatientError();
            })
            .catch(() => showPatientError(t('Js.SomethingWrong')))
            .finally(() => list.removeAttribute('aria-busy'));
    }

    // UHID as text — 0 is a real value (account created, no appointment booked yet), so it is shown as "0"
    function uhidText(value) {
        return value === null || value === undefined ? '' : String(value).trim();
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

    // Date in the selected portal language (i18n.js)
    function formatDob(dob) {
        return AppI18n.formatDate(dob);
    }

    // API sends "Male" / "Female" — show it in the portal language, unknown values as-is
    function genderText(gender) {
        const key = 'Gender.' + String(gender || '').trim();
        const text = t(key);
        return text === key ? gender : text;
    }

    window.resendRegisteredOTP = function () {
        document.querySelectorAll('#reg-otp-inputs .otp-input').forEach(i => { i.value = ''; i.disabled = false; });
        document.getElementById('reg-otp-verified').classList.remove('show');
        document.getElementById('Reg_IsMobileVerified').value = 'false';
        document.getElementById('patient-found-card').style.display = 'none';
        clearOtpError('reg');

        const btn = document.getElementById('reg-send-otp-btn');
        btn.disabled = false;
        btn.textContent = t('Js.SendOtp');
        btn.classList.remove('otp-sent');
        sendRegisteredOTP();
    };

    // ── Add family member (registered patient) ───────────
    // Form in #familyModal → ?handler=AddFamilyMember (MobSaveRegistrationForPatient). On success the server
    // adds the member to the verified list and selects it; the cards are redrawn from its answer.

    
    function initFamilyMemberForm() {
        const modal = document.getElementById('familyModal');
        const addBtn = document.getElementById('pf-add-btn');
        if (!modal || !addBtn) return;

        const $ = id => document.getElementById(id);
        const form = $('familyForm');
        const saveBtn = $('fm-save');
        let opener = null, busy = false, statesLoaded = false;

        addBtn.addEventListener('click', open);
        modal.querySelectorAll('[data-fm-close]').forEach(b => b.addEventListener('click', close));
        modal.addEventListener('click', e => { if (e.target === modal) close(); });
        modal.addEventListener('keydown', e => {
            if (e.key === 'Escape') { e.stopPropagation(); close(); return; }
            if (e.key !== 'Tab') return;                                  // keep focus inside the dialog
            const items = Array.from(modal.querySelectorAll('button:not([disabled]), input:not([disabled]), select:not([disabled])'))
                .filter(x => x.offsetParent !== null);
            if (!items.length) return;
            const first = items[0], last = items[items.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        });

        // Title → gender (same as the new patient form): M → only Male, F → only Female, B → any
        const titleGender = () => { const s = $('fm-title'); return s.value ? (s.options[s.selectedIndex]?.dataset.gender || '') : ''; };
        $('fm-title').addEventListener('change', () => {
            const only = lockGenderToTitle($('fm-gender'), titleGender());
            if (only) setGender(only);
        });
        $('fm-gender').querySelectorAll('.gender-btn').forEach(b => b.addEventListener('click', () => setGender(b.dataset.value)));
        $('fm-state').addEventListener('change', function () { loadFmCities(this.value); });
        $('fm-pincode').addEventListener('input', function () { this.value = this.value.replace(/\D/g, ''); });
        form.querySelectorAll('[data-field]').forEach(f => ['input', 'change'].forEach(ev => f.addEventListener(ev, () => fieldError(f.dataset.field, ''))));

        form.addEventListener('submit', e => { e.preventDefault(); save(); });

        function open() {
            opener = document.activeElement;
            form.reset();
            lockGenderToTitle($('fm-gender'), '');   // no title yet → all buttons free
            setGender('');
            showFormError('');
            form.querySelectorAll('[data-field]').forEach(f => fieldError(f.dataset.field, ''));
            $('fm-mobile').textContent = '+91 ' + (document.getElementById('reg-phone-input').value || '');
            $('fm-dob').max = document.getElementById('Form_DOB')?.max || '';
            fillTitles();
            if (!statesLoaded) loadFmStates();
            modal.hidden = false;
            document.body.style.overflow = 'hidden';
            setTimeout(() => $('fm-title').focus(), 30);
        }

        function close() {
            if (busy || modal.hidden) return;
            modal.hidden = true;
            document.body.style.overflow = '';
            if (opener && opener.focus) opener.focus();
        }

        function fillTitles() {
            const select = $('fm-title');
            if (select.options.length > 1) return;
            const add = list => list.forEach(x => {
                const opt = new Option((x.name || '').trim(), x.titleId);
                opt.dataset.gender = x.gender || '';
                select.appendChild(opt);
            });
            if (titleList.length) { add(titleList); return; }
            fetch('?handler=Titles').then(r => r.json()).then(res => { if (res.success) { titleList = res.data || []; add(titleList); } }).catch(() => { });
        }

        function loadFmStates() {
            const select = $('fm-state');
            fetch(`?handler=States&countryId=${DefaultCountryId || 1}`).then(r => r.json()).then(res => {
                if (!res.success) return;
                (res.data || []).forEach(s => select.appendChild(new Option(s.stateName.trim(), s.stateId)));
                statesLoaded = true;
            }).catch(() => { });
        }

        function loadFmCities(stateId) {
            const select = $('fm-city');
            select.innerHTML = '';
            select.appendChild(new Option(t('Js.SelectCity'), ''));
            select.disabled = true;
            if (!stateId) return;
            fetch(`?handler=Cities&stateId=${encodeURIComponent(stateId)}`).then(r => r.json()).then(res => {
                if (!res.success || $('fm-state').value !== stateId) return;
                (res.data || []).forEach(c => select.appendChild(new Option(c.cityName.trim(), c.cityId)));
                select.disabled = !(res.data || []).length;
            }).catch(() => { });
        }

        function setGender(value) {
            $('fm-gender').dataset.value = value;
            $('fm-gender').querySelectorAll('.gender-btn').forEach(b => {
                const on = b.dataset.value === value;
                b.classList.toggle('sel', on);
                b.setAttribute('aria-checked', String(on));
            });
            if (value) fieldError('gender', '');
        }

        function values() {
            const title = $('fm-title');
            return {
                titleId: Number(title.value) || 0,
                titleName: title.value ? title.options[title.selectedIndex].text : '',
                firstName: $('fm-first').value.trim(),
                middleName: $('fm-middle').value.trim(),
                lastName: $('fm-last').value.trim(),
                dob: $('fm-dob').value,
                gender: $('fm-gender').dataset.value || '',
                email: $('fm-email').value.trim(),
                address: $('fm-address').value.trim(),
                countryId: Number(DefaultCountryId) || 0,
                stateId: Number($('fm-state').value) || 0,
                cityId: Number($('fm-city').value) || 0,
                pincode: $('fm-pincode').value.trim()
            };
        }

        // Same rules as the server (OnPostAddFamilyMemberAsync) — the first problem is shown at its field
        function validate(v) {
            const today = new Date(); today.setHours(0, 0, 0, 0);
            const checks = [
                ['title', !v.titleId, 'Validation.SelectTitle'],
                ['firstName', !v.firstName, 'Validation.FirstNameRequired'],
                ['firstName', !nameRegex.test(v.firstName), 'Validation.FirstNameLetters'],
                ['middleName', v.middleName && !nameRegex.test(v.middleName), 'Validation.MiddleNameLetters'],
                ['lastName', !v.lastName, 'Validation.LastNameRequired'],
                ['lastName', !nameRegex.test(v.lastName), 'Validation.LastNameLetters'],
                ['dob', !v.dob, 'Validation.DobRequired'],
                ['dob', v.dob && new Date(v.dob) >= today, 'Validation.DobPast'],
                ['gender', !v.gender, 'Validation.SelectGender'],
                ['gender', (titleGender() === 'M' && v.gender !== 'Male') || (titleGender() === 'F' && v.gender !== 'Female'), 'Validation.GenderTitleMismatch'],
                ['state', !v.stateId, 'Validation.SelectState'],
                ['city', !v.cityId, 'Validation.SelectCity'],
                ['email', v.email && !emailRegex.test(v.email), 'Validation.EmailInvalid'],
                ['pincode', v.pincode && !/^\d{6}$/.test(v.pincode), 'Validation.PincodeInvalid']
            ];
            const seen = new Set();
            let ok = true;
            checks.forEach(([field, bad, key]) => {
                if (!bad || seen.has(field)) return;
                seen.add(field);
                fieldError(field, t(key));
                ok = false;
            });
            if (!ok) form.querySelector('.fm-invalid')?.focus();
            return ok;
        }

        function save() {
            if (busy) return;
            showFormError('');
            const v = values();
            if (!validate(v)) return;

            setBusy(true);
            postJson('AddFamilyMember', v)
                .then(data => {
                    setBusy(false);
                    if (data.success) {
                        close();
                        renderPatientCards(data.patientList || [], data.selected ? data.selected.selectedIndex : -1);
                        fillRegisteredHiddenFields(data.selected || {});
                        const card = document.querySelector('#pf-list .pf-card.selected');
                        if (card) {
                            card.classList.add('pf-card-new');
                            card.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        }
                        document.getElementById('pf-list-sub').textContent = t('Js.FamilyMemberAdded');
                        return;
                    }
                    if (data.field) { fieldError(data.field, data.message); return; }
                    if (data.redirectUrl) { window.location.href = data.redirectUrl; return; }
                    showFormError(data.message || t('Errors.FamilyMemberFailed'));
                })
                .catch(() => { setBusy(false); showFormError(t('Js.SomethingWrong')); });
        }

        function setBusy(on) {
            busy = on;
            saveBtn.disabled = on;
            saveBtn.textContent = on ? t('Js.PleaseWait') : t('Js.SaveSelect');
            modal.querySelectorAll('[data-fm-close]').forEach(b => b.disabled = on);
        }

        function showFormError(msg) {
            const box = $('fm-error');
            box.textContent = msg || '';
            box.hidden = !msg;
            if (msg) box.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }

        function fieldError(field, msg) {
            const input = form.querySelector(`[data-field="${field}"]`);
            if (!input) { if (msg) showFormError(msg); return; }
            const group = input.closest('.form-group');
            group?.querySelector('.fm-field-error')?.remove();
            input.classList.toggle('fm-invalid', !!msg);
            input.classList.toggle('input-error', !!msg && input.tagName !== 'DIV');
            if (!msg) return;
            const err = el('span', 'field-error fm-field-error', msg);
            err.setAttribute('role', 'alert');
            (group || input.parentNode).appendChild(err);
        }
    }

    // ── Notes + consent survive a page reload ────────────
    // Two layers (draft only — the Review & Confirm submit still validates everything):
    //  • browser copy, keyed by this page's one-time URL → instant, so even a reload right after typing keeps it
    //  • session copy on the server (one request at a time, so an older save never overwrites a newer one)
    function rememberExtrasOnChange() {
        const notes = document.getElementById('Form_AppointmentNotes');
        const terms = document.getElementById('Form_ConsentTerms');
        const marketing = document.getElementById('Form_ConsentMarketing');
        if (!notes || !terms || !marketing) return;

        const key = 'pd-extras:' + location.pathname;
        const values = () => ({ notes: notes.value, consentTerms: terms.checked, consentMarketing: marketing.checked });

        // Reload of this same page: the browser copy is the newest
        try {
            const draft = JSON.parse(sessionStorage.getItem(key) || 'null');
            if (draft) {
                notes.value = (draft.notes || '').slice(0, 300);
                terms.checked = !!draft.consentTerms;
                marketing.checked = !!draft.consentMarketing;
                const counter = document.getElementById('char-count');
                if (counter) counter.textContent = notes.value.length;
            }
        } catch { /* storage blocked — the server copy is still used */ }

        let timer = null, busy = false, pending = false;
        const send = () => {
            clearTimeout(timer);
            if (busy) { pending = true; return; }
            busy = true; pending = false;
            fetch('?handler=RememberExtras', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify(values())
            }).catch(() => { }).finally(() => { busy = false; if (pending) send(); });
        };
        const changed = (delay) => {
            try { sessionStorage.setItem(key, JSON.stringify(values())); } catch { }
            clearTimeout(timer);
            timer = setTimeout(send, delay);
        };

        notes.addEventListener('input', () => changed(500));
        terms.addEventListener('change', () => changed(0));
        marketing.addEventListener('change', () => changed(0));
    }

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
                ['Form_Title', document.getElementById('Form_Title').value, t('Validation.SelectTitle')],
                ['Form_FirstName', document.getElementById('Form_FirstName').value.trim(), t('Validation.FirstNameRequired')],
                ['Form_LastName', document.getElementById('Form_LastName').value.trim(), t('Validation.LastNameRequired')],
                ['Form_DOB', document.getElementById('Form_DOB').value, t('Validation.DobRequired')]
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
                showError('Form_FirstName', t('Validation.FirstNameLetters'));
                if (!firstErrEl) firstErrEl = document.getElementById('Form_FirstName');
                isValid = false;
            }

            const lastName = document.getElementById('Form_LastName').value.trim();
            if (lastName && !nameRegex.test(lastName)) {
                showError('Form_LastName', t('Validation.LastNameLetters'));
                if (!firstErrEl) firstErrEl = document.getElementById('Form_LastName');
                isValid = false;
            }

            const middleName = document.getElementById('Form_MiddleName').value.trim();
            if (middleName && !nameRegex.test(middleName)) {
                showError('Form_MiddleName', t('Validation.MiddleNameLetters'));
                if (!firstErrEl) firstErrEl = document.getElementById('Form_MiddleName');
                isValid = false;
            }

            const dob = document.getElementById('Form_DOB').value;
            if (dob) {
                const dobDate = new Date(dob);
                const today = new Date(); today.setHours(0, 0, 0, 0);
                if (dobDate >= today) {
                    showError('Form_DOB', t('Validation.DobPast'));
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
                showError('Form_Email', t('Validation.EmailInvalid'));
                if (!firstErrEl) firstErrEl = document.getElementById('Form_Email');
                isValid = false;
            }

            const pincode = document.getElementById('Form_Pincode')?.value?.trim();
            if (pincode && !/^\d{6}$/.test(pincode)) {
                showError('Form_Pincode', t('Validation.PincodeInvalid'));
                if (!firstErrEl) firstErrEl = document.getElementById('Form_Pincode');
                isValid = false;
            }

            const mobile = document.getElementById('phone-input').value.trim();
            const verified = document.getElementById('Form_IsMobileVerified').value.toLowerCase();
            if (!mobile) {
                showError('phone-input', t('Validation.MobileRequired'));
                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
                isValid = false;
            } else if (!/^\d{10}$/.test(mobile)) {
                showError('phone-input', t('Validation.MobileInvalid'));
                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
                isValid = false;
            } else if (verified !== 'true') {
                showError('phone-input', t('Validation.VerifyMobile'));
                if (!firstErrEl) firstErrEl = document.getElementById('phone-input');
                isValid = false;
            }
        } else if (patientType === 'registered') {
            const mobile = document.getElementById('reg-phone-input').value.trim();
            const verified = document.getElementById('Reg_IsMobileVerified').value.toLowerCase();

            if (!mobile) {
                showError('reg-phone-input', t('Validation.MobileRequired'));
                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
                isValid = false;
            } else if (!/^\d{10}$/.test(mobile)) {
                showError('reg-phone-input', t('Validation.MobileInvalid'));
                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
                isValid = false;
            } else if (verified !== 'true') {
                showError('reg-phone-input', t('Validation.VerifyMobile'));
                if (!firstErrEl) firstErrEl = document.getElementById('reg-phone-input');
                isValid = false;
            } else {
                // a card must be selected — its UHID may legitimately be 0 (account created, no appointment booked yet)
                if (!document.querySelector('#pf-list .pf-card.selected')) {
                    showPatientError(t('Validation.SelectPatient'));
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
