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
    const deletePracticeId = document.getElementById('deletePracticeId');
    const editPracticeFromDetailsButton = document.getElementById('editPracticeFromDetailsButton');
    const generateAttestationSheetButton = document.getElementById('generateAttestationSheetButton');

    let specialties = [];
    let students = [];
    let supervisors = [];
    let currentDetails = null;

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

    async function fetchJson(url) {
        const response = await fetch(url);
        if (!response.ok) return null;
        return await response.json();
    }

    async function loadSpecialties() {
        const data = await fetchJson(`${window.apiBaseUrl.replace(/\/$/, '')}/api/Helping/specialties`);
        specialties = Array.isArray(data) ? data : [];

        specialtyFilterSelect.innerHTML = '<option value="">Все специальности</option>';
        practiceSpecialtySelect.innerHTML = '<option value="">-- выберите специальность --</option>';

        specialties.forEach(item => {
            const filterOption = document.createElement('option');
            filterOption.value = item.id;
            filterOption.textContent = `${item.code} — ${item.name}`;
            specialtyFilterSelect.appendChild(filterOption);

            const formOption = document.createElement('option');
            formOption.value = item.id;
            formOption.textContent = `${item.code} — ${item.name}`;
            practiceSpecialtySelect.appendChild(formOption);
        });

        specialtyFilterSelect.dispatchEvent(new Event('change', { bubbles: true }));
        practiceSpecialtySelect.dispatchEvent(new Event('change', { bubbles: true }));
    }

    async function loadStudentsAndSupervisors() {
        const studentsData = await fetchJson(`${window.apiBaseUrl.replace(/\/$/, '')}/api/admin/users`);
        const allUsers = Array.isArray(studentsData) ? studentsData : [];

        students = allUsers.filter(x => x.role === 'Student');
        supervisors = allUsers.filter(x => x.role === 'Supervisor');
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
            reindexPracticeForm();
        });

        competenciesContainer.appendChild(fragment);
        reindexPracticeForm();
    }

    function createAssignmentItem(value = {}) {
        const fragment = assignmentTemplate.content.cloneNode(true);
        const element = fragment.querySelector('.assignment-item');

        const studentSelect = element.querySelector('.assignment-student-select');
        const supervisorSelect = element.querySelector('.assignment-supervisor-select');
        const studentCustom = element.querySelector('.assignment-student-custom-select');
        const supervisorCustom = element.querySelector('.assignment-supervisor-custom-select');

        studentSelect.innerHTML = '<option value="">-- выберите студента --</option>';
        supervisorSelect.innerHTML = '<option value="">-- выберите руководителя --</option>';

        const selectedSpecialtyId = practiceSpecialtySelect.value;

        students
            .filter(x => !selectedSpecialtyId || String(x.specialtyId || '') === String(selectedSpecialtyId))
            .forEach(student => {
                const option = document.createElement('option');
                option.value = student.id;
                option.textContent = student.fullName;
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
            reindexPracticeForm();
        });

        assignmentsContainer.appendChild(fragment);
        reindexPracticeForm();
    }

    function reindexPracticeForm() {
        const competencyItems = Array.from(competenciesContainer.querySelectorAll('.competency-item'));
        competencyItems.forEach((item, index) => {
            item.querySelector('.competency-code-input').name = `Competencies[${index}].CompetencyCode`;
            item.querySelector('.competency-description-input').name = `Competencies[${index}].CompetencyDescription`;
            item.querySelector('.competency-worktypes-input').name = `Competencies[${index}].WorkTypes`;
            item.querySelector('.competency-hours-input').name = `Competencies[${index}].Hours`;
        });

        const assignmentItems = Array.from(assignmentsContainer.querySelectorAll('.assignment-item'));
        assignmentItems.forEach((item, index) => {
            item.querySelector('.assignment-student-select').name = `StudentAssignments[${index}].StudentId`;
            item.querySelector('.assignment-supervisor-select').name = `StudentAssignments[${index}].SupervisorId`;
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
        practiceSpecialtySelect.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function refillAssignmentsForSpecialty() {
        const existingValues = Array.from(assignmentsContainer.querySelectorAll('.assignment-item')).map(item => ({
            studentId: item.querySelector('.assignment-student-select').value,
            supervisorId: item.querySelector('.assignment-supervisor-select').value
        }));

        assignmentsContainer.innerHTML = '';
        existingValues.forEach(item => createAssignmentItem(item));
    }

    practiceSpecialtySelect?.addEventListener('change', refillAssignmentsForSpecialty);

    addCompetencyButton?.addEventListener('click', () => createCompetencyItem());
    addAssignmentButton?.addEventListener('click', () => createAssignmentItem());

    openCreatePracticeButton?.addEventListener('click', () => {
        resetPracticeForm();
        practiceEditModalTitle.textContent = 'Создание производственной практики';
        practiceEditModalSubtitle.textContent = 'Заполните основную информацию, компетенции и назначения.';
        createCompetencyItem();
        openModal(editModalBackdrop);
    });

    async function openDetails(practiceId) {
        const details = await fetchJson(`${window.apiBaseUrl.replace(/\/$/, '')}/api/department-staff/practices/${practiceId}`);
        if (!details) return;

        currentDetails = details;

        practiceDetailsTitle.textContent = `${details.practiceIndex} — ${details.name}`;
        practiceDetailsSubtitle.textContent = `${details.specialtyCode} ${details.specialtyName}`;

        practiceDetailsInfo.innerHTML = `
            <div class="department-details-item">
                <span class="department-details-label">Индекс ПП</span>
                <span class="department-details-value">${details.practiceIndex}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Количество часов</span>
                <span class="department-details-value">${details.hours}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Код ПМ</span>
                <span class="department-details-value">${details.professionalModuleCode}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">Название ПМ</span>
                <span class="department-details-value">${details.professionalModuleName}</span>
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

        deletePracticeId.value = details.id;
        openModal(detailsModalBackdrop);
    }

    function fillEditFormFromDetails(details) {
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
        (details.studentAssignments || []).forEach(item => createAssignmentItem(item));

        practiceEditModalTitle.textContent = 'Редактирование производственной практики';
        practiceEditModalSubtitle.textContent = 'Измените данные практики, компетенции и назначения.';
    }

    editPracticeFromDetailsButton?.addEventListener('click', () => {
        if (!currentDetails) return;
        closeModal(detailsModalBackdrop);
        fillEditFormFromDetails(currentDetails);
        openModal(editModalBackdrop);
    });

    generateAttestationSheetButton?.addEventListener('click', () => {
        alert('Следующим шагом сюда подключим формирование аттестационного листа по шаблону.');
    });

    document.getElementById('deletePracticeForm')?.addEventListener('submit', function (e) {
        if (!confirm('Вы уверены, что хотите удалить производственную практику?')) {
            e.preventDefault();
        }
    });

    function formatDate(dateText) {
        if (!dateText) return '—';
        const date = new Date(dateText);
        if (Number.isNaN(date.getTime())) return dateText;
        return date.toLocaleDateString('ru-RU');
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

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

    Promise.all([loadSpecialties(), loadStudentsAndSupervisors()])
        .then(() => {
            applyPracticeFilters();
        });
});