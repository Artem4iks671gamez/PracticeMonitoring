/* Обновлённый весь файл: добавлена клиентская валидация и небольшие улучшения UX */
document.addEventListener('DOMContentLoaded', function () {
    const initialPractices = window.departmentStaffInitialPractices || [];

    const practiceSearchInput = document.getElementById('practiceSearchInput');
    const practicesList = document.getElementById('practicesList');
    const practicesCountLabel = document.getElementById('practicesCountLabel');
    const dateFromFilter = document.getElementById('dateFromFilter');
    const dateToFilter = document.getElementById('dateToFilter');
    const specialtyFilterSelect = document.getElementById('specialtyFilterSelect');
    const practiceSortSelect = document.getElementById('practiceSortSelect');

    const editModalBackdrop = document.getElementById('practiceEditModalBackdrop');
    const detailsModalBackdrop = document.getElementById('practiceDetailsModalBackdrop');

    const openCreatePracticeButton = document.getElementById('openCreatePracticeButton');
    const closePracticeEditModalButton = document.getElementById('closePracticeEditModalButton');
    const cancelPracticeEditModalButton = document.getElementById('cancelPracticeEditModalButton');
    const closePracticeDetailsModalButton = document.getElementById('closePracticeDetailsModalButton');

    const practiceEditModalTitle = document.getElementById('practiceEditModalTitle');
    const practiceEditModalSubtitle = document.getElementById('practiceEditModalSubtitle');
    const practiceGlobalError = document.getElementById('practiceGlobalError');

    const practiceForm = document.getElementById('practiceForm');
    const practiceIdInput = document.getElementById('practiceId');
    const practiceIndexInput = document.getElementById('practiceIndex');
    const practiceNameInput = document.getElementById('practiceName');
    const practiceSpecialtySelect = document.getElementById('practiceSpecialtySelect');
    const practiceHoursInput = document.getElementById('practiceHours');
    const professionalModuleCodeInput = document.getElementById('professionalModuleCode');
    const professionalModuleNameInput = document.getElementById('professionalModuleName');
    const practiceStartDateInput = document.getElementById('practiceStartDate');
    const practiceEndDateInput = document.getElementById('practiceEndDate');

    const competenciesContainer = document.getElementById('competenciesContainer');
    const assignmentsContainer = document.getElementById('assignmentsContainer');
    const addCompetencyButton = document.getElementById('addCompetencyButton');
    const addAssignmentButton = document.getElementById('addAssignmentButton');

    const competencyTemplate = document.getElementById('competencyItemTemplate');
    const assignmentTemplate = document.getElementById('assignmentItemTemplate');

    const practiceDetailsTitle = document.getElementById('practiceDetailsTitle');
    const practiceDetailsSubtitle = document.getElementById('practiceDetailsSubtitle');
    const practiceDetailsInfo = document.getElementById('practiceDetailsInfo');
    const practiceDetailsAssignments = document.getElementById('practiceDetailsAssignments');
    const practiceDetailsCompetencies = document.getElementById('practiceDetailsCompetencies');
    const editPracticeFromDetailsButton = document.getElementById('editPracticeFromDetailsButton');
    const generateAttestationSheetButton = document.getElementById('generateAttestationSheetButton');
    const deletePracticeButton = document.getElementById('deletePracticeButton');

    let practices = Array.isArray(initialPractices) ? initialPractices : [];
    let specialties = [];
    let students = [];
    let supervisors = [];
    let currentDetails = null;

    function clearFieldErrors() {
        document.querySelectorAll('.field-error').forEach(x => {
            x.textContent = '';
        });

        document.querySelectorAll('.input-error').forEach(x => {
            x.classList.remove('input-error');
        });

        if (practiceGlobalError) {
            practiceGlobalError.textContent = '';
            practiceGlobalError.classList.remove('show');
        }
    }

    function showGlobalError(message) {
        if (!practiceGlobalError) return;
        practiceGlobalError.textContent = message || 'Исправьте ошибки формы.';
        practiceGlobalError.classList.add('show');
    }

    function setSimpleFieldError(fieldName, message) {
        const errorBlock = document.querySelector(`[data-error-for="${fieldName}"]`);
        if (errorBlock) {
            errorBlock.textContent = message;
        }

        const inputMap = {
            PracticeIndex: practiceIndexInput,
            Name: practiceNameInput,
            SpecialtyId: practiceSpecialtySelect,
            Hours: practiceHoursInput,
            ProfessionalModuleCode: professionalModuleCodeInput,
            ProfessionalModuleName: professionalModuleNameInput,
            StartDate: practiceStartDateInput,
            EndDate: practiceEndDateInput
        };

        const input = inputMap[fieldName];
        if (!input) return;

        input.classList.add('input-error');

        if (input.tagName.toLowerCase() === 'select') {
            const customTrigger = document.querySelector(`.custom-select[data-target="${input.id}"] .custom-select-trigger`);
            customTrigger?.classList.add('input-error');
        }
    }

    function applyValidationErrors(errors, globalMessage) {
        clearFieldErrors();

        if (globalMessage) {
            showGlobalError(globalMessage);
        }

        Object.entries(errors || {}).forEach(([key, messages]) => {
            if (!messages || !messages.length) return;

            if (key.startsWith('Competencies[')) {
                const match = key.match(/^Competencies\[(\d+)\]\.(.+)$/);
                if (!match) return;

                const index = Number(match[1]);
                const field = match[2];
                const card = competenciesContainer.querySelectorAll('.competency-item')[index];
                if (!card) return;

                const inputMap = {
                    CompetencyCode: '.competency-code-input',
                    CompetencyDescription: '.competency-description-input',
                    WorkTypes: '.competency-worktypes-input',
                    Hours: '.competency-hours-input'
                };

                const errorMap = {
                    CompetencyCode: '.competency-code-error',
                    CompetencyDescription: '.competency-description-error',
                    WorkTypes: '.competency-worktypes-error',
                    Hours: '.competency-hours-error'
                };

                const input = card.querySelector(inputMap[field]);
                const errorBlock = card.querySelector(errorMap[field]);

                if (input) input.classList.add('input-error');
                if (errorBlock) errorBlock.textContent = messages[0];
                return;
            }

            if (key.startsWith('StudentAssignments[')) {
                const match = key.match(/^StudentAssignments\[(\d+)\]\.(.+)$/);
                if (!match) return;

                const index = Number(match[1]);
                const field = match[2];
                const card = assignmentsContainer.querySelectorAll('.assignment-item')[index];
                if (!card) return;

                const inputMap = {
                    StudentId: '.assignment-student-select',
                    SupervisorId: '.assignment-supervisor-select'
                };

                const errorMap = {
                    StudentId: '.assignment-student-error',
                    SupervisorId: '.assignment-supervisor-error'
                };

                const input = card.querySelector(inputMap[field]);
                const errorBlock = card.querySelector(errorMap[field]);

                if (input) {
                    input.classList.add('input-error');

                    if (input.tagName.toLowerCase() === 'select') {
                        const customTrigger = card.querySelector(`.custom-select[data-target="${input.id}"] .custom-select-trigger`);
                        customTrigger?.classList.add('input-error');
                    }
                }

                if (errorBlock) errorBlock.textContent = messages[0];
                return;
            }

            setSimpleFieldError(key, messages[0]);
        });

        // фокус на первую ошибку
        const firstErrorInput = document.querySelector('.input-error');
        if (firstErrorInput) {
            firstErrorInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
            firstErrorInput.focus?.();
        }
    }

    function buildCustomSelect(selectId, onChange) {
        const nativeSelect = document.getElementById(selectId);
        const custom = document.querySelector(`.custom-select[data-target="${selectId}"]`);

        if (!nativeSelect || !custom) return;

        const trigger = custom.querySelector('.custom-select-trigger');
        const menu = custom.querySelector('.custom-select-menu');
        if (!trigger || !menu) return;

        const rebuild = () => {
            menu.innerHTML = '';
            const options = Array.from(nativeSelect.options);

            options.forEach(opt => {
                const item = document.createElement('div');
                item.className = 'custom-select-option';
                item.textContent = opt.textContent;
                item.dataset.value = opt.value;

                if (opt.selected) {
                    item.classList.add('selected');
                    trigger.textContent = opt.textContent;
                }

                item.addEventListener('click', () => {
                    nativeSelect.value = opt.value;
                    nativeSelect.dispatchEvent(new Event('change', { bubbles: true }));
                    custom.classList.remove('open');
                });

                menu.appendChild(item);
            });

            const selected = options.find(x => x.selected) || options[0];
            if (selected) {
                trigger.textContent = selected.textContent;
                menu.querySelectorAll('.custom-select-option').forEach(x => {
                    x.classList.toggle('selected', x.dataset.value === selected.value);
                });
            }
        };

        trigger.addEventListener('click', () => {
            document.querySelectorAll('.custom-select.open').forEach(x => {
                if (x !== custom) x.classList.remove('open');
            });
            custom.classList.toggle('open');
        });

        nativeSelect.addEventListener('change', () => {
            rebuild();
            if (onChange) onChange(nativeSelect.value);
        });

        rebuild();
        return rebuild;
    }

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.custom-select')) {
            document.querySelectorAll('.custom-select.open').forEach(x => x.classList.remove('open'));
        }
    });

    function rebuildSimpleCustomSelect(customRoot, nativeSelect) {
        const trigger = customRoot.querySelector('.custom-select-trigger');
        const menu = customRoot.querySelector('.custom-select-menu');
        if (!trigger || !menu) return;

        menu.innerHTML = '';

        Array.from(nativeSelect.options).forEach(opt => {
            const item = document.createElement('div');
            item.className = 'custom-select-option';
            item.textContent = opt.textContent;
            item.dataset.value = opt.value;

            if (opt.selected) {
                item.classList.add('selected');
                trigger.textContent = opt.textContent;
            }

            item.addEventListener('click', () => {
                nativeSelect.value = opt.value;
                nativeSelect.dispatchEvent(new Event('change', { bubbles: true }));
                customRoot.classList.remove('open');
            });

            menu.appendChild(item);
        });

        const selected = Array.from(nativeSelect.options).find(x => x.selected) || nativeSelect.options[0];
        if (selected) {
            trigger.textContent = selected.textContent;
            menu.querySelectorAll('.custom-select-option').forEach(x => {
                x.classList.toggle('selected', x.dataset.value === selected.value);
            });
        }
    }

    function wireSimpleCustomSelect(customRoot, nativeSelect) {
        const trigger = customRoot.querySelector('.custom-select-trigger');
        if (!trigger) return;

        trigger.addEventListener('click', () => {
            document.querySelectorAll('.custom-select.open').forEach(x => {
                if (x !== customRoot) x.classList.remove('open');
            });
            customRoot.classList.toggle('open');
        });

        nativeSelect.addEventListener('change', () => rebuildSimpleCustomSelect(customRoot, nativeSelect));
        rebuildSimpleCustomSelect(customRoot, nativeSelect);
    }

    function getPracticeCards() {
        return Array.from(document.querySelectorAll('.practice-card'));
    }

    function getPracticeSearchText(card) {
        return [
            card.dataset.practiceIndex,
            card.dataset.name,
            card.dataset.specialtyCode,
            card.dataset.specialtyName
        ]
            .filter(Boolean)
            .join(' ')
            .toLowerCase();
    }

    function applyPracticeFilters() {
        const search = (practiceSearchInput?.value || '').trim().toLowerCase();
        const specialtyId = specialtyFilterSelect?.value || '';
        const dateFrom = dateFromFilter?.value || '';
        const dateTo = dateToFilter?.value || '';
        const sort = practiceSortSelect?.value || 'date-asc';

        let cards = getPracticeCards();

        cards.forEach(card => {
            const cardSpecialtyId = card.dataset.specialtyId || '';
            const startDate = card.dataset.startDate || '';
            const endDate = card.dataset.endDate || '';

            const matchesSearch = !search || getPracticeSearchText(card).includes(search);
            const matchesSpecialty = !specialtyId || specialtyId === cardSpecialtyId;
            const matchesDateFrom = !dateFrom || endDate >= dateFrom;
            const matchesDateTo = !dateTo || startDate <= dateTo;

            card.style.display = matchesSearch && matchesSpecialty && matchesDateFrom && matchesDateTo ? '' : 'none';
        });

        cards = cards.filter(card => card.style.display !== 'none');

        cards.sort((a, b) => {
            const aStart = a.dataset.startDate || '';
            const bStart = b.dataset.startDate || '';
            const aEnd = a.dataset.endDate || '';
            const bEnd = b.dataset.endDate || '';
            const aIndex = (a.dataset.practiceIndex || '').toLowerCase();
            const bIndex = (b.dataset.practiceIndex || '').toLowerCase();
            const aHours = Number(a.dataset.hours || 0);
            const bHours = Number(b.dataset.hours || 0);

            switch (sort) {
                case 'date-desc':
                    return bStart.localeCompare(aStart) || bEnd.localeCompare(aEnd);
                case 'index-asc':
                    return aIndex.localeCompare(bIndex, 'ru');
                case 'hours-desc':
                    return bHours - aHours || aStart.localeCompare(bStart);
                case 'date-asc':
                default:
                    return aStart.localeCompare(bStart) || aEnd.localeCompare(bEnd);
            }
        });

        cards.forEach(card => practicesList.appendChild(card));

        if (practicesCountLabel) {
            practicesCountLabel.textContent = `Показано практик: ${cards.length}`;
        }
    }

    function openModal(backdrop) {
        backdrop?.classList.add('open');
    }

    function closeModal(backdrop) {
        backdrop?.classList.remove('open');
    }

    closePracticeEditModalButton?.addEventListener('click', () => closeModal(editModalBackdrop));
    cancelPracticeEditModalButton?.addEventListener('click', () => closeModal(editModalBackdrop));
    closePracticeDetailsModalButton?.addEventListener('click', () => closeModal(detailsModalBackdrop));

    editModalBackdrop?.addEventListener('click', function (e) {
        if (e.target === editModalBackdrop) closeModal(editModalBackdrop);
    });

    detailsModalBackdrop?.addEventListener('click', function (e) {
        if (e.target === detailsModalBackdrop) closeModal(detailsModalBackdrop);
    });

    async function fetchJson(url, options = {}) {
        const response = await fetch(url, {
            headers: options.body ? { 'Content-Type': 'application/json' } : {},
            ...options
        });

        if (response.status === 204) return null;

        const text = await response.text();
        const data = text ? JSON.parse(text) : null;

        if (!response.ok) {
            const error = new Error((data && data.message) || 'Произошла ошибка.');
            error.validationErrors = data && data.errors ? data.errors : {};
            throw error;
        }

        return data;
    }

    async function loadSpecialties() {
        const data = await fetchJson('/DepartmentStaff/GetFormData');
        specialties = data && data.specialties ? data.specialties : [];
        supervisors = data && data.supervisors ? data.supervisors : [];

        specialtyFilterSelect.innerHTML = '<option value="">Все специальности</option>';
        practiceSpecialtySelect.innerHTML = '<option value="">-- выберите специальность --</option>';

        specialties.forEach(item => {
            const filterOption = document.createElement('option');
            filterOption.value = item.id;
            filterOption.textContent = item.label;
            specialtyFilterSelect.appendChild(filterOption);

            const formOption = document.createElement('option');
            formOption.value = item.id;
            formOption.textContent = item.label;
            practiceSpecialtySelect.appendChild(formOption);
        });

        specialtyFilterSelect.dispatchEvent(new Event('change', { bubbles: true }));
        practiceSpecialtySelect.dispatchEvent(new Event('change', { bubbles: true }));
    }

    async function loadStudentsForSpecialty(specialtyId) {
        if (!specialtyId) return [];
        const data = await fetchJson(`/DepartmentStaff/GetFormData?specialtyId=${specialtyId}`);
        students = data && data.students ? data.students : [];
        supervisors = data && data.supervisors ? data.supervisors : supervisors;
        return students;
    }

    function createCompetencyItem(value = {}) {
        const fragment = competencyTemplate.content.cloneNode(true);
        const element = fragment.querySelector('.competency-item');

        element.querySelector('.competency-code-input').value = value.competencyCode || '';
        element.querySelector('.competency-description-input').value = value.competencyDescription || '';
        element.querySelector('.competency-worktypes-input').value = value.workTypes || '';
        element.querySelector('.competency-hours-input').value = value.hours || '';

        element.querySelector('.remove-competency-button').addEventListener('click', () => {
            element.remove();
            clearFieldErrors();
            reindexPracticeForm();
        });

        competenciesContainer.appendChild(fragment);
        reindexPracticeForm();
    }

    function createAssignmentItem(studentsSource, value = {}) {
        const fragment = assignmentTemplate.content.cloneNode(true);
        const element = fragment.querySelector('.assignment-item');

        const studentSelect = element.querySelector('.assignment-student-select');
        const supervisorSelect = element.querySelector('.assignment-supervisor-select');
        const studentCustom = element.querySelector('.assignment-student-custom-select');
        const supervisorCustom = element.querySelector('.assignment-supervisor-custom-select');

        studentSelect.innerHTML = '<option value="">-- выберите студента --</option>';
        supervisorSelect.innerHTML = '<option value="">-- выберите руководителя --</option>';

        studentsSource.forEach(student => {
            const option = document.createElement('option');
            option.value = student.id;
            option.textContent = student.groupName
                ? `${student.fullName} (${student.groupName})`
                : student.fullName;

            if (Number(value.studentId || 0) === Number(student.id)) {
                option.selected = true;
            }

            studentSelect.appendChild(option);
        });

        supervisors.forEach(supervisor => {
            const option = document.createElement('option');
            option.value = supervisor.id;
            option.textContent = supervisor.fullName;

            if (Number(value.supervisorId || 0) === Number(supervisor.id)) {
                option.selected = true;
            }

            supervisorSelect.appendChild(option);
        });

        wireSimpleCustomSelect(studentCustom, studentSelect);
        wireSimpleCustomSelect(supervisorCustom, supervisorSelect);

        element.querySelector('.remove-assignment-button').addEventListener('click', () => {
            element.remove();
            clearFieldErrors();
            reindexPracticeForm();
        });

        assignmentsContainer.appendChild(fragment);
        reindexPracticeForm();
    }

    function reindexPracticeForm() {
        const competencyItems = Array.from(competenciesContainer.querySelectorAll('.competency-item'));
        competencyItems.forEach((item, index) => {
            item.dataset.index = String(index);
        });

        const assignmentItems = Array.from(assignmentsContainer.querySelectorAll('.assignment-item'));
        assignmentItems.forEach((item, index) => {
            item.dataset.index = String(index);
        });
    }

    function resetPracticeForm() {
        practiceIdInput.value = '';
        practiceIndexInput.value = '';
        practiceNameInput.value = '';
        practiceSpecialtySelect.value = '';
        practiceHoursInput.value = '';
        professionalModuleCodeInput.value = '';
        professionalModuleNameInput.value = '';
        practiceStartDateInput.value = '';
        practiceEndDateInput.value = '';
        competenciesContainer.innerHTML = '';
        assignmentsContainer.innerHTML = '';
        clearFieldErrors();
        practiceSpecialtySelect.dispatchEvent(new Event('change', { bubbles: true }));
    }

    async function refillAssignmentsForSpecialty() {
        clearFieldErrors();

        const selectedSpecialtyId = practiceSpecialtySelect.value;
        if (!selectedSpecialtyId) {
            assignmentsContainer.innerHTML = '';
            return;
        }

        const existingValues = Array.from(assignmentsContainer.querySelectorAll('.assignment-item')).map(item => ({
            studentId: item.querySelector('.assignment-student-select').value,
            supervisorId: item.querySelector('.assignment-supervisor-select').value
        }));

        const studentsForSpecialty = await loadStudentsForSpecialty(selectedSpecialtyId);

        assignmentsContainer.innerHTML = '';
        existingValues.forEach(item => createAssignmentItem(studentsForSpecialty, item));
    }

    practiceSpecialtySelect?.addEventListener('change', refillAssignmentsForSpecialty);

    addCompetencyButton?.addEventListener('click', () => {
        createCompetencyItem();
    });

    addAssignmentButton?.addEventListener('click', async () => {
        clearFieldErrors();

        const specialtyId = Number(practiceSpecialtySelect.value);
        if (!specialtyId) {
            applyValidationErrors({ SpecialtyId: ['Сначала выберите специальность.'] }, 'Сначала выберите специальность.');
            return;
        }

        const studentsForSpecialty = await loadStudentsForSpecialty(specialtyId);
        createAssignmentItem(studentsForSpecialty);
    });

    openCreatePracticeButton?.addEventListener('click', async () => {
        resetPracticeForm();
        practiceEditModalTitle.textContent = 'Создание производственной практики';
        practiceEditModalSubtitle.textContent = 'Заполните основную информацию, компетенции и назначения.';
        createCompetencyItem();
        openModal(editModalBackdrop);
    });

    async function openDetails(practiceId) {
        const details = await fetchJson(`/DepartmentStaff/GetPracticeDetails?id=${practiceId}`);
        if (!details) return;

        currentDetails = details;

        practiceDetailsTitle.textContent = `${details.practiceIndex} — ${details.name}`;
        practiceDetailsSubtitle.textContent = `${details.specialtyCode} ${details.specialtyName}`;

        practiceDetailsInfo.innerHTML = `
            <div class="department-details-item">
                <span class="department-details-label">Индекс ПП</span>
                <span class="department-details-value">${escapeHtml(details.practiceIndex)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Количество часов</span>
                <span class="department-details-value">${details.hours}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Код ПМ</span>
                <span class="department-details-value">${escapeHtml(details.professionalModuleCode)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Название ПМ</span>
                <span class="department-details-value">${escapeHtml(details.professionalModuleName)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Дата начала</span>
                <span class="department-details-value">${formatDate(details.startDate)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Дата окончания</span>
                <span class="department-details-value">${formatDate(details.endDate)}</span>
            </div>
        `;

        practiceDetailsAssignments.innerHTML = details.studentAssignments.length
            ? details.studentAssignments.map(x => `
                <div class="department-details-card">
                    <div class="department-details-card-title">${escapeHtml(x.studentFullName)}</div>
                    <div class="department-details-card-text">
                        Руководитель от техникума: ${escapeHtml(x.supervisorFullName || 'Не назначен')}
                    </div>
                </div>
            `).join('')
            : '<div class="department-details-card"><div class="department-details-card-text">Студенты пока не назначены.</div></div>';

        practiceDetailsCompetencies.innerHTML = details.competencies.length
            ? details.competencies.map(x => `
                <div class="department-details-card">
                    <div class="department-details-card-title">${escapeHtml(x.competencyCode)} — ${escapeHtml(x.competencyDescription)}</div>
                    <div class="department-details-card-text">${escapeHtml(x.workTypes)}</div>
                    <div class="department-details-card-text">Часы: ${x.hours}</div>
                </div>
            `).join('')
            : '<div class="department-details-card"><div class="department-details-card-text">Компетенции пока не добавлены.</div></div>';

        openModal(detailsModalBackdrop);
    }

    async function fillEditFormFromDetails(details) {
        resetPracticeForm();

        practiceIdInput.value = details.id;
        practiceIndexInput.value = details.practiceIndex || '';
        practiceNameInput.value = details.name || '';
        practiceSpecialtySelect.value = String(details.specialtyId || '');
        practiceHoursInput.value = details.hours || '';
        professionalModuleCodeInput.value = details.professionalModuleCode || '';
        professionalModuleNameInput.value = details.professionalModuleName || '';
        practiceStartDateInput.value = (details.startDate || '').slice(0, 10);
        practiceEndDateInput.value = (details.endDate || '').slice(0, 10);

        practiceSpecialtySelect.dispatchEvent(new Event('change', { bubbles: true }));

        (details.competencies || []).forEach(item => createCompetencyItem(item));

        const studentsForSpecialty = await loadStudentsForSpecialty(details.specialtyId);
        (details.studentAssignments || []).forEach(item => createAssignmentItem(studentsForSpecialty, item));

        practiceEditModalTitle.textContent = 'Редактирование производственной практики';
        practiceEditModalSubtitle.textContent = 'Измените данные практики, компетенции и назначения.';
    }

    editPracticeFromDetailsButton?.addEventListener('click', async () => {
        if (!currentDetails) return;
        closeModal(detailsModalBackdrop);
        await fillEditFormFromDetails(currentDetails);
        openModal(editModalBackdrop);
    });

    generateAttestationSheetButton?.addEventListener('click', () => {
        alert('Следующим шагом сюда подключим формирование аттестационного листа по шаблону.');
    });

    deletePracticeButton?.addEventListener('click', async () => {
        if (!currentDetails) return;

        const confirmed = confirm('Вы уверены, что хотите удалить производственную практику?');
        if (!confirmed) return;

        try {
            await fetchJson('/DepartmentStaff/DeletePractice', {
                method: 'POST',
                body: JSON.stringify(currentDetails.id)
            });

            closeModal(detailsModalBackdrop);

            const card = practicesList.querySelector(`.practice-card[data-id="${currentDetails.id}"]`);
            if (card) {
                card.remove();
            }

            practices = practices.filter(x => Number(x.id) !== Number(currentDetails.id));
            applyPracticeFilters();
        } catch (error) {
            alert(error.message || 'Не удалось удалить практику.');
        }
    });

    function buildPayload() {
        return {
            id: practiceIdInput.value ? Number(practiceIdInput.value) : null,
            practiceIndex: practiceIndexInput.value.trim(),
            name: practiceNameInput.value.trim(),
            specialtyId: Number(practiceSpecialtySelect.value),
            professionalModuleCode: professionalModuleCodeInput.value.trim(),
            professionalModuleName: professionalModuleNameInput.value.trim(),
            hours: Number(practiceHoursInput.value),
            startDate: practiceStartDateInput.value,
            endDate: practiceEndDateInput.value,
            competencies: Array.from(competenciesContainer.querySelectorAll('.competency-item')).map(item => ({
                competencyCode: item.querySelector('.competency-code-input').value.trim(),
                competencyDescription: item.querySelector('.competency-description-input').value.trim(),
                workTypes: item.querySelector('.competency-worktypes-input').value.trim(),
                hours: Number(item.querySelector('.competency-hours-input').value)
            })),
            studentAssignments: Array.from(assignmentsContainer.querySelectorAll('.assignment-item')).map(item => {
                const studentValue = item.querySelector('.assignment-student-select').value;
                const supervisorValue = item.querySelector('.assignment-supervisor-select').value;

                return {
                    studentId: studentValue ? Number(studentValue) : 0,
                    supervisorId: supervisorValue ? Number(supervisorValue) : null
                };
            })
        };
    }

    function validatePayload(payload) {
        const errors = {};
        let global = null;

        // helpers
        const add = (key, msg) => {
            if (!errors[key]) errors[key] = [];
            errors[key].push(msg);
        };

        const isIndexValid = (v) => /^\d+(\.\d+)*$/.test(v);
        const isCodeValid = (v) => /^\d+(\.\d+)*$/.test(v); // код: цифры и точки

        // Основные поля
        if (!payload.practiceIndex) add('PracticeIndex', 'Индекс ПП обязателен.');
        else if (!isIndexValid(payload.practiceIndex)) add('PracticeIndex', 'Индекс должен содержать только цифры и точки, напр. 12.3.');

        if (!payload.name) add('Name', 'Название практики обязательно.');
        else if (payload.name.length < 3) add('Name', 'Название слишком короткое.');

        if (!payload.specialtyId || payload.specialtyId <= 0) add('SpecialtyId', 'Выберите специальность.');

        if (!payload.hours || !Number.isFinite(payload.hours) || payload.hours < 1) add('Hours', 'Количество часов должно быть положительным числом.');

        if (!payload.professionalModuleCode) add('ProfessionalModuleCode', 'Код ПМ обязателен.');
        else if (!isCodeValid(payload.professionalModuleCode)) add('ProfessionalModuleCode', 'Код ПМ должен содержать только цифры и точки.');

        if (!payload.professionalModuleName) add('ProfessionalModuleName', 'Название ПМ обязательно.');

        if (!payload.startDate) add('StartDate', 'Укажите дату начала.');
        if (!payload.endDate) add('EndDate', 'Укажите дату окончания.');

        if (payload.startDate && payload.endDate) {
            const s = new Date(payload.startDate);
            const e = new Date(payload.endDate);
            if (isNaN(s) || isNaN(e)) add('StartDate', 'Неверный формат даты.');
            else if (e < s) add('EndDate', 'Дата окончания не может быть раньше даты начала.');
        }

        // Компетенции
        if (!Array.isArray(payload.competencies) || payload.competencies.length === 0) {
            add('Competencies', 'Добавьте хотя бы одну компетенцию.');
        } else {
            payload.competencies.forEach((c, i) => {
                const base = `Competencies[${i}]`;
                if (!c.competencyCode) add(`${base}.CompetencyCode`, 'Код компетенции обязателен.');
                else if (!/^[\w\-\d\.]+$/.test(c.competencyCode)) add(`${base}.CompetencyCode`, 'Код содержит недопустимые символы.');

                if (!c.competencyDescription) add(`${base}.CompetencyDescription`, 'Описание компетенции обязательно.');
                if (!c.workTypes) add(`${base}.WorkTypes`, 'Укажите виды работ.');
                if (!c.hours || !Number.isFinite(c.hours) || c.hours < 1) add(`${base}.Hours`, 'Часы должны быть положительным числом.');
            });
        }

        // Назначения
        if (Array.isArray(payload.studentAssignments)) {
            payload.studentAssignments.forEach((a, i) => {
                const base = `StudentAssignments[${i}]`;
                if (!a.studentId || a.studentId <= 0) add(`${base}.StudentId`, 'Выберите студента.');
                // supervisorId может быть null на этапе создания — не делаем обязательным
            });
        }

        if (Object.keys(errors).length) global = 'Есть ошибки в форме. Исправьте их и попробуйте снова.';
        return { valid: Object.keys(errors).length === 0, errors, global };
    }

    practiceForm?.addEventListener('submit', async function (e) {
        e.preventDefault();
        clearFieldErrors();

        try {
            const payload = buildPayload();

            // локальная валидация перед отправкой
            const { valid, errors, global } = validatePayload(payload);
            if (!valid) {
                applyValidationErrors(errors, global);
                return;
            }

            await fetchJson('/DepartmentStaff/SavePractice', {
                method: 'POST',
                body: JSON.stringify(payload)
            });

            closeModal(editModalBackdrop);
            window.location.reload();
        } catch (error) {
            applyValidationErrors(error.validationErrors || {}, error.message || 'Не удалось сохранить производственную практику.');
        }
    });

    document.querySelectorAll('.practice-details-button').forEach(button => {
        button.addEventListener('click', function () {
            const card = button.closest('.practice-card');
            if (!card) return;
            openDetails(card.dataset.id);
        });
    });

    practiceSearchInput?.addEventListener('input', applyPracticeFilters);
    dateFromFilter?.addEventListener('change', applyPracticeFilters);
    dateToFilter?.addEventListener('change', applyPracticeFilters);

    buildCustomSelect('practiceSortSelect', applyPracticeFilters);
    buildCustomSelect('specialtyFilterSelect', applyPracticeFilters);
    buildCustomSelect('practiceSpecialtySelect');

    Promise.resolve(loadSpecialties()).then(() => {
        applyPracticeFilters();
    });

    // вспомогательные функции, используемые выше
    function escapeHtml(s) {
        if (!s) return '';
        return String(s).replace(/[&<>"']/g, function (m) { return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[m]; });
    }

    function formatDate(v) {
        if (!v) return '';
        try {
            return new Date(v).toLocaleDateString('ru-RU');
        } catch { return v; }
    }
});         