document.addEventListener('DOMContentLoaded', function () {
    const panelButtons = Array.from(document.querySelectorAll('.department-nav-button'));
    const panels = Array.from(document.querySelectorAll('.department-panel'));

    const practiceSearchInput = document.getElementById('practiceSearchInput');
    const practicesList = document.getElementById('practicesList');
    const practicesCountLabel = document.getElementById('practicesCountLabel');
    const dateFromFilter = document.getElementById('dateFromFilter');
    const dateToFilter = document.getElementById('dateToFilter');
    const specialtyFilterSelect = document.getElementById('specialtyFilterSelect');
    const practiceSortSelect = document.getElementById('practiceSortSelect');

    const editModalBackdrop = document.getElementById('practiceEditModalBackdrop');
    const detailsModalBackdrop = document.getElementById('practiceDetailsModalBackdrop');
    const assignmentsModalBackdrop = document.getElementById('practiceAssignmentsModalBackdrop');
    const attestationPreviewModalBackdrop = document.getElementById('attestationPreviewModalBackdrop');

    const openCreatePracticeButton = document.getElementById('openCreatePracticeButton');
    const closePracticeEditModalButton = document.getElementById('closePracticeEditModalButton');
    const cancelPracticeEditModalButton = document.getElementById('cancelPracticeEditModalButton');
    const closePracticeDetailsModalButton = document.getElementById('closePracticeDetailsModalButton');
    const closePracticeAssignmentsModalButton = document.getElementById('closePracticeAssignmentsModalButton');
    const cancelPracticeAssignmentsModalButton = document.getElementById('cancelPracticeAssignmentsModalButton');
    const savePracticeAssignmentsButton = document.getElementById('savePracticeAssignmentsButton');

    const closeAttestationPreviewModalButton = document.getElementById('closeAttestationPreviewModalButton');
    const cancelAttestationPreviewButton = document.getElementById('cancelAttestationPreviewButton');
    const downloadAttestationButton = document.getElementById('downloadAttestationButton');
    const attestationPreviewContainer = document.getElementById('attestationPreviewContainer');
    const attestationPreviewFileName = document.getElementById('attestationPreviewFileName');

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
    const addCompetencyButton = document.getElementById('addCompetencyButton');
    const competencyTemplate = document.getElementById('competencyItemTemplate');

    const assignmentTabs = Array.from(document.querySelectorAll('.assignment-tab-button'));
    const practiceAssignmentsModalTitle = document.getElementById('practiceAssignmentsModalTitle');
    const practiceAssignmentsModalSubtitle = document.getElementById('practiceAssignmentsModalSubtitle');
    const assignmentPracticeMeta = document.getElementById('assignmentPracticeMeta');
    const assignedStudentsCounter = document.getElementById('assignedStudentsCounter');
    const allStudentsCounter = document.getElementById('allStudentsCounter');
    const studentAssignmentSearchInput = document.getElementById('studentAssignmentSearchInput');
    const studentSpecialtyFilterSelect = document.getElementById('studentSpecialtyFilterSelect');
    const studentCourseFilterSelect = document.getElementById('studentCourseFilterSelect');
    const studentGroupFilterSelect = document.getElementById('studentGroupFilterSelect');
    const studentSortSelect = document.getElementById('studentSortSelect');
    const assignAllFilteredCheckbox = document.getElementById('assignAllFilteredCheckbox');
    const studentAssignmentBulkBar = document.getElementById('studentAssignmentBulkBar');
    const studentAssignmentSummary = document.getElementById('studentAssignmentSummary');
    const studentAssignmentTableBody = document.getElementById('studentAssignmentTableBody');
    const studentAssignmentEmptyState = document.getElementById('studentAssignmentEmptyState');

    const practiceDetailsTitle = document.getElementById('practiceDetailsTitle');
    const practiceDetailsSubtitle = document.getElementById('practiceDetailsSubtitle');
    const practiceDetailsOverviewTitle = document.getElementById('practiceDetailsOverviewTitle');
    const practiceDetailsOverviewSubtitle = document.getElementById('practiceDetailsOverviewSubtitle');
    const practiceDetailsOverviewStats = document.getElementById('practiceDetailsOverviewStats');
    const practiceDetailsInfo = document.getElementById('practiceDetailsInfo');
    const practiceDetailsAssignments = document.getElementById('practiceDetailsAssignments');
    const practiceDetailsCompetencies = document.getElementById('practiceDetailsCompetencies');
    const openAssignmentsFromDetailsButton = document.getElementById('openAssignmentsFromDetailsButton');
    const editPracticeFromDetailsButton = document.getElementById('editPracticeFromDetailsButton');
    const generateAttestationSheetButton = document.getElementById('generateAttestationSheetButton');
    const deletePracticeButton = document.getElementById('deletePracticeButton');

    let specialties = [];
    let students = [];
    let supervisors = [];
    let studentLookup = new Map();
    let currentDetails = null;
    let currentAssignmentPractice = null;
    let currentAttestationPracticeId = null;
    let assignmentCatalogLoaded = false;
    let assignmentCatalogLoading = false;
    let assignmentTab = 'all';
    let assignmentFilters = createDefaultAssignmentFilters();
    let assignmentSelections = new Map();
    let assignmentRowErrors = {};
    let submittedAssignmentSnapshot = [];

    function createDefaultAssignmentFilters() {
        return {
            search: '',
            specialtyId: '',
            course: '',
            groupName: '',
            sort: 'name-asc'
        };
    }

    function escapeHtml(value) {
        const div = document.createElement('div');
        div.textContent = value ?? '';
        return div.innerHTML;
    }

    function formatDate(value) {
        if (!value) return '-';
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return value;
        return date.toLocaleDateString('ru-RU');
    }

    function switchPanel(targetId) {
        panelButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.panelTarget === targetId);
        });

        panels.forEach(panel => {
            panel.classList.toggle('active', panel.id === targetId);
        });
    }

    panelButtons.forEach(button => {
        button.addEventListener('click', function () {
            switchPanel(button.dataset.panelTarget);
        });
    });

    function resetAssignmentRowErrors() {
        assignmentRowErrors = {};
    }

    function clearFieldErrors() {
        document.querySelectorAll('.field-error').forEach(x => {
            x.textContent = '';
        });

        document.querySelectorAll('.input-error').forEach(x => {
            x.classList.remove('input-error');
        });

        resetAssignmentRowErrors();

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

    function getOrderedAssignments() {
        return Array.from(assignmentSelections.values()).sort((left, right) => {
            const leftStudent = getStudentById(left.studentId);
            const rightStudent = getStudentById(right.studentId);

            const byCourse = compareNullableNumbers(leftStudent?.course, rightStudent?.course);
            if (byCourse !== 0) return byCourse;

            const byGroup = compareStrings(leftStudent?.groupName, rightStudent?.groupName);
            if (byGroup !== 0) return byGroup;

            return compareStrings(leftStudent?.fullName, rightStudent?.fullName);
        });
    }

    function applyValidationErrors(errors, globalMessage) {
        clearFieldErrors();

        if (globalMessage) {
            showGlobalError(globalMessage);
        }

        let hasAssignmentRowErrors = false;

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
                const assignment = submittedAssignmentSnapshot[index];

                if (!assignment) {
                    setSimpleFieldError('StudentAssignments', messages[0]);
                    return;
                }

                if (!assignmentRowErrors[assignment.studentId]) {
                    assignmentRowErrors[assignment.studentId] = {};
                }

                assignmentRowErrors[assignment.studentId][field] = messages[0];
                hasAssignmentRowErrors = true;
                return;
            }

            setSimpleFieldError(key, messages[0]);
        });

        if (hasAssignmentRowErrors) {
            assignmentTab = 'assigned';
            renderAssignmentWorkspace();
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

            trigger.disabled = nativeSelect.disabled;
            custom.classList.toggle('is-disabled', nativeSelect.disabled);
        };

        nativeSelect._customSelectRebuild = rebuild;

        if (!custom.dataset.bound) {
            trigger.addEventListener('click', () => {
                if (nativeSelect.disabled) return;

                document.querySelectorAll('.custom-select.open').forEach(x => {
                    if (x !== custom) x.classList.remove('open');
                });
                custom.classList.toggle('open');
            });

            nativeSelect.addEventListener('change', () => {
                rebuild();
                if (onChange) onChange(nativeSelect.value);
            });

            custom.dataset.bound = 'true';
        }

        rebuild();
        return rebuild;
    }

    function refreshCustomSelect(select) {
        if (!select || typeof select._customSelectRebuild !== 'function') return;
        select._customSelectRebuild();
    }

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.custom-select')) {
            document.querySelectorAll('.custom-select.open').forEach(x => x.classList.remove('open'));
        }
    });

    function getPracticeCards() {
        return Array.from(document.querySelectorAll('.practice-card'));
    }

    function getPracticeSearchText(card) {
        return [
            card.dataset.practiceIndex,
            card.dataset.name,
            card.dataset.specialtyCode,
            card.dataset.specialtyName,
            card.dataset.professionalModuleCode,
            card.dataset.professionalModuleName
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
    closePracticeAssignmentsModalButton?.addEventListener('click', () => closeModal(assignmentsModalBackdrop));
    cancelPracticeAssignmentsModalButton?.addEventListener('click', () => closeModal(assignmentsModalBackdrop));
    closeAttestationPreviewModalButton?.addEventListener('click', () => closeModal(attestationPreviewModalBackdrop));
    cancelAttestationPreviewButton?.addEventListener('click', () => closeModal(attestationPreviewModalBackdrop));

    editModalBackdrop?.addEventListener('click', function (e) {
        if (e.target === editModalBackdrop) closeModal(editModalBackdrop);
    });

    detailsModalBackdrop?.addEventListener('click', function (e) {
        if (e.target === detailsModalBackdrop) closeModal(detailsModalBackdrop);
    });

    assignmentsModalBackdrop?.addEventListener('click', function (e) {
        if (e.target === assignmentsModalBackdrop) closeModal(assignmentsModalBackdrop);
    });

    attestationPreviewModalBackdrop?.addEventListener('click', function (e) {
        if (e.target === attestationPreviewModalBackdrop) closeModal(attestationPreviewModalBackdrop);
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

    function fillSelect(select, items, placeholder, mapValue, mapText) {
        if (!select) return;

        const currentValue = select.value;
        select.innerHTML = '';

        const placeholderOption = document.createElement('option');
        placeholderOption.value = '';
        placeholderOption.textContent = placeholder;
        select.appendChild(placeholderOption);

        items.forEach(item => {
            const option = document.createElement('option');
            option.value = String(mapValue(item));
            option.textContent = mapText(item);
            select.appendChild(option);
        });

        if (currentValue && Array.from(select.options).some(x => x.value === currentValue)) {
            select.value = currentValue;
        }

        if (typeof select._customSelectRebuild === 'function') {
            select._customSelectRebuild();
        }
    }

    function updateStudentLookup() {
        studentLookup = new Map(students.map(student => [Number(student.id), student]));
    }

    async function loadFormMetadata() {
        const data = await fetchJson('/DepartmentStaff/GetFormData');
        specialties = data && data.specialties ? data.specialties : [];
        supervisors = data && data.supervisors ? data.supervisors : [];
        updatePracticeSpecialtySelects();
        updateAssignmentSpecialtyFilterOptions();
    }

    async function ensureAssignmentCatalogLoaded() {
        if (assignmentCatalogLoaded || assignmentCatalogLoading) return;

        assignmentCatalogLoading = true;
        renderAssignmentWorkspace();

        try {
            const data = await fetchJson('/DepartmentStaff/GetFormData?includeAllStudents=true');
            specialties = data && data.specialties ? data.specialties : specialties;
            supervisors = data && data.supervisors ? data.supervisors : supervisors;
            students = data && data.students ? data.students : [];
            updateStudentLookup();
            updatePracticeSpecialtySelects();
            updateAssignmentSpecialtyFilterOptions();
            assignmentCatalogLoaded = true;
        } finally {
            assignmentCatalogLoading = false;
            renderAssignmentWorkspace();
        }
    }

    function updatePracticeSpecialtySelects() {
        fillSelect(
            specialtyFilterSelect,
            specialties,
            'Все специальности',
            item => item.id,
            item => item.label
        );

        fillSelect(
            practiceSpecialtySelect,
            specialties,
            '-- выберите специальность --',
            item => item.id,
            item => item.label
        );

        specialtyFilterSelect.dispatchEvent(new Event('change', { bubbles: true }));
        practiceSpecialtySelect.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function updateAssignmentSpecialtyFilterOptions() {
        fillSelect(
            studentSpecialtyFilterSelect,
            specialties,
            'Все специальности',
            item => item.id,
            item => item.label
        );
    }

    function getPracticeSpecialtyId() {
        const value = Number(currentAssignmentPractice?.specialtyId || 0);
        return Number.isFinite(value) ? value : 0;
    }

    function getPracticeSpecialtyLabel() {
        const specialtyId = getPracticeSpecialtyId();
        const specialty = specialties.find(item => Number(item.id) === specialtyId);
        return specialty ? specialty.label : '';
    }

    function updateAssignmentPracticeMeta() {
        if (!assignmentPracticeMeta) return;

        const specialtyLabel = getPracticeSpecialtyLabel();
        if (specialtyLabel) {
            assignmentPracticeMeta.textContent = `Специальность практики: ${specialtyLabel}`;
            return;
        }

        assignmentPracticeMeta.textContent = 'Откройте назначение для конкретной практики';
    }

    function syncAssignmentFilterWithPracticeSpecialty() {
        assignmentFilters.specialtyId = currentAssignmentPractice ? String(currentAssignmentPractice.specialtyId || '') : '';
        assignmentFilters.course = '';
        assignmentFilters.groupName = '';

        if (studentSpecialtyFilterSelect) {
            studentSpecialtyFilterSelect.value = assignmentFilters.specialtyId;
        }

        if (studentCourseFilterSelect) {
            studentCourseFilterSelect.value = '';
        }

        if (studentGroupFilterSelect) {
            studentGroupFilterSelect.value = '';
        }

        refreshCustomSelect(studentSpecialtyFilterSelect);
        refreshCustomSelect(studentCourseFilterSelect);
        refreshCustomSelect(studentGroupFilterSelect);

        updateAssignmentPracticeMeta();
        updateAssignmentFilterOptions();
        renderAssignmentWorkspace();
    }

    function getStudentById(studentId) {
        return studentLookup.get(Number(studentId)) || null;
    }

    function canAssignStudent(student) {
        const specialtyId = getPracticeSpecialtyId();
        if (!specialtyId) return false;
        return Number(student.specialtyId || 0) === specialtyId;
    }

    function getStudentSearchText(student) {
        return [
            student.fullName,
            student.specialtyCode,
            student.specialtyName,
            student.groupName,
            student.course
        ]
            .filter(Boolean)
            .join(' ')
            .toLowerCase();
    }

    function compareStrings(left, right) {
        return String(left || '').localeCompare(String(right || ''), 'ru', { sensitivity: 'base' });
    }

    function compareNullableNumbers(left, right) {
        const normalizedLeft = left ?? Number.MAX_SAFE_INTEGER;
        const normalizedRight = right ?? Number.MAX_SAFE_INTEGER;
        return normalizedLeft - normalizedRight;
    }

    function compareStudents(left, right, sort) {
        switch (sort) {
            case 'name-desc':
                return compareStrings(right.fullName, left.fullName);
            case 'course-asc':
                return compareNullableNumbers(left.course, right.course) || compareStrings(left.fullName, right.fullName);
            case 'course-desc':
                return compareNullableNumbers(right.course, left.course) || compareStrings(left.fullName, right.fullName);
            case 'group-asc':
                return compareStrings(left.groupName, right.groupName) || compareStrings(left.fullName, right.fullName);
            case 'specialty-asc':
                return compareStrings(left.specialtyName, right.specialtyName) || compareStrings(left.fullName, right.fullName);
            case 'name-asc':
            default:
                return compareStrings(left.fullName, right.fullName);
        }
    }

    function getFilteredStudents() {
        let filtered = students.slice();

        const search = assignmentFilters.search.trim().toLowerCase();
        if (search) {
            filtered = filtered.filter(student => getStudentSearchText(student).includes(search));
        }

        if (assignmentFilters.specialtyId) {
            filtered = filtered.filter(student => String(student.specialtyId || '') === assignmentFilters.specialtyId);
        }

        if (assignmentFilters.course) {
            filtered = filtered.filter(student => String(student.course || '') === assignmentFilters.course);
        }

        if (assignmentFilters.groupName) {
            filtered = filtered.filter(student => String(student.groupName || '') === assignmentFilters.groupName);
        }

        if (assignmentTab === 'assigned') {
            filtered = filtered.filter(student => assignmentSelections.has(Number(student.id)));
        }

        filtered.sort((left, right) => compareStudents(left, right, assignmentFilters.sort));
        return filtered;
    }

    function updateAssignmentFilterOptions() {
        const specialtyId = assignmentFilters.specialtyId;
        const baseStudents = specialtyId
            ? students.filter(student => String(student.specialtyId || '') === specialtyId)
            : students.slice();

        const courseValues = Array.from(new Set(
            baseStudents
                .filter(student => student.course != null)
                .map(student => Number(student.course))
        ))
            .sort((left, right) => left - right)
            .map(value => ({ value: String(value), label: `${value} курс` }));

        fillSelect(
            studentCourseFilterSelect,
            courseValues,
            'Все курсы',
            item => item.value,
            item => item.label
        );

        if (assignmentFilters.course && !courseValues.some(item => item.value === assignmentFilters.course)) {
            assignmentFilters.course = '';
            studentCourseFilterSelect.value = '';
            refreshCustomSelect(studentCourseFilterSelect);
        }

        const groupBaseStudents = assignmentFilters.course
            ? baseStudents.filter(student => String(student.course || '') === assignmentFilters.course)
            : baseStudents;

        const groupValues = Array.from(new Set(
            groupBaseStudents
                .map(student => student.groupName)
                .filter(Boolean)
        ))
            .sort((left, right) => compareStrings(left, right))
            .map(value => ({ value, label: value }));

        fillSelect(
            studentGroupFilterSelect,
            groupValues,
            'Все группы',
            item => item.value,
            item => item.label
        );

        if (assignmentFilters.groupName && !groupValues.some(item => item.value === assignmentFilters.groupName)) {
            assignmentFilters.groupName = '';
            studentGroupFilterSelect.value = '';
            refreshCustomSelect(studentGroupFilterSelect);
        }

        if (studentSortSelect && studentSortSelect.value !== assignmentFilters.sort) {
            studentSortSelect.value = assignmentFilters.sort;
            refreshCustomSelect(studentSortSelect);
        }
    }

    function renderSupervisorOptions(selectedSupervisorId) {
        const selectedValue = selectedSupervisorId ? String(selectedSupervisorId) : '';

        return [
            '<option value="">-- выберите руководителя --</option>',
            ...supervisors.map(supervisor => {
                const value = String(supervisor.id);
                const selected = value === selectedValue ? ' selected' : '';
                return `<option value="${value}"${selected}>${escapeHtml(supervisor.fullName)}</option>`;
            })
        ].join('');
    }

    function getAssignmentEmptyMessage() {
        if (assignmentCatalogLoading) {
            return 'Загружаем полный список студентов...';
        }

        if (!assignmentCatalogLoaded) {
            return 'Откройте создание или редактирование практики, чтобы загрузить список студентов.';
        }

        if (assignmentTab === 'assigned') {
            return 'Назначенных студентов пока нет. Переключитесь на полный список и отметьте нужных.';
        }

        return 'По выбранным фильтрам студенты не найдены. Измените параметры поиска или сбросьте фильтры.';
    }

    function initializeAssignmentSupervisorSelects() {
        if (!studentAssignmentTableBody) return;

        studentAssignmentTableBody
            .querySelectorAll('.custom-select[data-target^="assignmentSupervisorSelect-"]')
            .forEach(custom => {
                const selectId = custom.dataset.target;
                if (!selectId) return;
                buildCustomSelect(selectId);
            });
    }

    function renderAssignmentWorkspace() {
        if (!studentAssignmentTableBody || !studentAssignmentEmptyState) return;

        updateAssignmentPracticeMeta();
        updateAssignmentFilterOptions();

        const filteredStudents = assignmentCatalogLoaded ? getFilteredStudents() : [];
        const assignableFilteredStudents = filteredStudents.filter(student => canAssignStudent(student));
        const assignedCount = assignmentSelections.size;

        if (assignedStudentsCounter) {
            assignedStudentsCounter.textContent = String(assignedCount);
        }

        if (allStudentsCounter) {
            allStudentsCounter.textContent = String(students.length);
        }

        assignmentTabs.forEach(button => {
            button.classList.toggle('active', button.dataset.assignmentTab === assignmentTab);
        });

        if (studentAssignmentBulkBar) {
            studentAssignmentBulkBar.classList.toggle('hidden', assignmentTab === 'assigned');
        }

        if (assignAllFilteredCheckbox) {
            const allAssignableSelected = assignableFilteredStudents.length > 0 &&
                assignableFilteredStudents.every(student => assignmentSelections.has(Number(student.id)));

            assignAllFilteredCheckbox.disabled = assignableFilteredStudents.length === 0;
            assignAllFilteredCheckbox.checked = allAssignableSelected;
        }

        if (studentAssignmentSummary) {
            const visibleAssignedCount = filteredStudents.filter(student => assignmentSelections.has(Number(student.id))).length;
            studentAssignmentSummary.textContent =
                `Видно ${filteredStudents.length} студентов, из них ${visibleAssignedCount} уже в текущей выборке. Всего назначено: ${assignedCount}.`;
        }

        if (filteredStudents.length === 0) {
            studentAssignmentTableBody.innerHTML = '';
            studentAssignmentEmptyState.textContent = getAssignmentEmptyMessage();
            studentAssignmentEmptyState.classList.add('show');
            return;
        }

        studentAssignmentEmptyState.textContent = '';
        studentAssignmentEmptyState.classList.remove('show');

        studentAssignmentTableBody.innerHTML = filteredStudents.map(student => {
            const studentId = Number(student.id);
            const isAssigned = assignmentSelections.has(studentId);
            const assignment = assignmentSelections.get(studentId);
            const isAllowed = canAssignStudent(student);
            const isCheckboxDisabled = !isAllowed && !isAssigned;
            const rowErrors = assignmentRowErrors[studentId] || {};
            const studentError = rowErrors.StudentId || '';
            const supervisorError = rowErrors.SupervisorId || '';
            const supervisorSelectId = `assignmentSupervisorSelect-${studentId}`;
            const statusBadges = [
                isAssigned ? '<span class="student-assignment-badge">Назначен</span>' : '',
                !isAllowed && getPracticeSpecialtyId()
                    ? '<span class="student-assignment-badge warning">Другая специальность</span>'
                    : '',
                !getPracticeSpecialtyId()
                    ? '<span class="student-assignment-badge warning">Сначала выберите специальность практики</span>'
                    : ''
            ]
                .filter(Boolean)
                .join('');

            return `
                <tr class="student-assignment-row ${isAssigned ? 'is-assigned' : ''} ${!isAllowed ? 'is-unavailable' : ''}" data-student-id="${studentId}">
                    <td>
                        <label class="student-assignment-toggle">
                            <span class="student-assignment-toggle-main">
                                <input type="checkbox"
                                       class="assignment-row-checkbox"
                                       ${isAssigned ? 'checked' : ''}
                                       ${isCheckboxDisabled ? 'disabled' : ''} />
                                <span>${isAssigned ? 'На практике' : 'Назначить'}</span>
                            </span>
                            <span class="student-assignment-toggle-state">
                                ${isAssigned ? 'Студент включен в список практики' : 'Строка пока не включена'}
                            </span>
                        </label>
                    </td>
                    <td>
                        <div class="student-assignment-student">
                            <div class="student-assignment-student-name">${escapeHtml(student.fullName)}</div>
                            <div class="student-assignment-student-meta">${statusBadges}</div>
                            <div class="field-error">${escapeHtml(studentError)}</div>
                        </div>
                    </td>
                    <td>
                        <div class="student-assignment-cell-main">${escapeHtml(student.specialtyCode || '-')}</div>
                        <div class="student-assignment-cell-sub">${escapeHtml(student.specialtyName || 'Специальность не указана')}</div>
                    </td>
                    <td>
                        <div class="student-assignment-cell-main">${escapeHtml(student.groupName || '-')}</div>
                    </td>
                    <td>
                        <div class="student-assignment-cell-main">${escapeHtml(student.course != null ? `${student.course}` : '-')}</div>
                    </td>
                    <td>
                        <div class="student-assignment-supervisor-wrap">
                            <select id="${supervisorSelectId}"
                                    class="native-select-hidden student-assignment-supervisor-select ${supervisorError ? 'input-error' : ''}"
                                    ${!isAssigned || !isAllowed ? 'disabled' : ''}>
                                ${renderSupervisorOptions(assignment ? assignment.supervisorId : null)}
                            </select>
                            <div class="custom-select student-assignment-select" data-target="${supervisorSelectId}">
                                <button type="button" class="custom-select-trigger ${supervisorError ? 'input-error' : ''}">
                                    -- выберите руководителя --
                                </button>
                                <div class="custom-select-menu"></div>
                            </div>
                            <div class="field-error">${escapeHtml(supervisorError)}</div>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
        initializeAssignmentSupervisorSelects();
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
        });

        competenciesContainer.appendChild(fragment);
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
        currentAssignmentPractice = null;
        assignmentSelections = new Map();
        submittedAssignmentSnapshot = [];
        assignmentTab = 'all';
        assignmentFilters = createDefaultAssignmentFilters();

        if (studentAssignmentSearchInput) {
            studentAssignmentSearchInput.value = '';
        }

        if (studentSortSelect) {
            studentSortSelect.value = assignmentFilters.sort;
        }

        clearFieldErrors();
        practiceSpecialtySelect.dispatchEvent(new Event('change', { bubbles: true }));
    }

    async function openDetails(practiceId) {
        const details = await fetchJson(`/DepartmentStaff/GetPracticeDetails?id=${practiceId}`);
        if (!details) return;

        currentDetails = details;

        practiceDetailsTitle.textContent = `${details.practiceIndex} - ${details.name}`;
        practiceDetailsSubtitle.textContent = `${details.specialtyCode} ${details.specialtyName}`;
        if (practiceDetailsOverviewTitle) {
            practiceDetailsOverviewTitle.textContent = details.name || '\u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u0435\u043d\u043d\u0430\u044f \u043f\u0440\u0430\u043a\u0442\u0438\u043a\u0430';
        }
        if (practiceDetailsOverviewSubtitle) {
            practiceDetailsOverviewSubtitle.textContent =
                `${details.practiceIndex} \u2022 ${details.specialtyCode} ${details.specialtyName}`;
        }
        if (practiceDetailsOverviewStats) {
            practiceDetailsOverviewStats.innerHTML = `
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">\u0427\u0430\u0441\u044b</span>
                    <span class="department-details-overview-stat-value">${details.hours}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">\u0421\u0442\u0443\u0434\u0435\u043d\u0442\u044b</span>
                    <span class="department-details-overview-stat-value">${details.studentAssignments.length}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">\u041a\u043e\u043c\u043f\u0435\u0442\u0435\u043d\u0446\u0438\u0438</span>
                    <span class="department-details-overview-stat-value">${details.competencies.length}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">\u041f\u0435\u0440\u0438\u043e\u0434</span>
                    <span class="department-details-overview-stat-value">${formatDate(details.startDate)} - ${formatDate(details.endDate)}</span>
                </div>
            `;
        }

        practiceDetailsInfo.innerHTML = `
            <div class="department-details-item">
                <span class="department-details-label">\u041d\u0430\u0437\u0432\u0430\u043d\u0438\u0435 \u043f\u0440\u0430\u043a\u0442\u0438\u043a\u0438</span>
                <span class="department-details-value department-details-value-strong">${escapeHtml(details.name)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">\u0421\u043f\u0435\u0446\u0438\u0430\u043b\u044c\u043d\u043e\u0441\u0442\u044c</span>
                <span class="department-details-value">${escapeHtml(details.specialtyCode || '-')} ${escapeHtml(details.specialtyName || '')}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">\u0418\u043d\u0434\u0435\u043a\u0441 \u041f\u041f</span>
                <span class="department-details-value">${escapeHtml(details.practiceIndex)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">\u041a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e \u0447\u0430\u0441\u043e\u0432</span>
                <span class="department-details-value">${details.hours}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">\u041a\u043e\u0434 \u041f\u041c</span>
                <span class="department-details-value">${escapeHtml(details.professionalModuleCode)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">\u041d\u0430\u0437\u0432\u0430\u043d\u0438\u0435 \u041f\u041c</span>
                <span class="department-details-value">${escapeHtml(details.professionalModuleName)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">\u0414\u0430\u0442\u0430 \u043d\u0430\u0447\u0430\u043b\u0430</span>
                <span class="department-details-value">${formatDate(details.startDate)}</span>
            </div>
            <div class="department-details-item">
                <span class="department-details-label">\u0414\u0430\u0442\u0430 \u043e\u043a\u043e\u043d\u0447\u0430\u043d\u0438\u044f</span>
                <span class="department-details-value">${formatDate(details.endDate)}</span>
            </div>
        `;

        practiceDetailsAssignments.innerHTML = details.studentAssignments.length
            ? details.studentAssignments.map(x => `
                <div class="department-details-card">
                    <div class="department-details-card-header">
                        <div class="department-details-card-title">${escapeHtml(x.studentFullName)}</div>
                        <div class="department-details-chip">${escapeHtml(x.studentCourse != null ? `${x.studentCourse} \u043a\u0443\u0440\u0441` : '\u041a\u0443\u0440\u0441 \u043d\u0435 \u0443\u043a\u0430\u0437\u0430\u043d')}</div>
                    </div>
                    <div class="department-details-card-meta">
                        <span class="department-details-inline-chip">
                            ${escapeHtml(x.studentSpecialtyCode || '-')} ${escapeHtml(x.studentSpecialtyName || '')}
                        </span>
                        <span class="department-details-inline-chip">${escapeHtml(x.studentGroupName || '\u0413\u0440\u0443\u043f\u043f\u0430 \u043d\u0435 \u0443\u043a\u0430\u0437\u0430\u043d\u0430')}</span>
                    </div>
                    <div class="department-details-card-text">
                        \u0420\u0443\u043a\u043e\u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u043e\u0442 \u0442\u0435\u0445\u043d\u0438\u043a\u0443\u043c\u0430: ${escapeHtml(x.supervisorFullName || '\u041d\u0435 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d')}
                    </div>
                </div>
            `).join('')
            : '<div class="department-details-card"><div class="department-details-card-text">\u0421\u0442\u0443\u0434\u0435\u043d\u0442\u044b \u043f\u043e\u043a\u0430 \u043d\u0435 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u044b.</div></div>';

        practiceDetailsCompetencies.innerHTML = details.competencies.length
            ? details.competencies.map(x => `
                <div class="department-details-card">
                    <div class="department-details-card-header">
                        <div class="department-details-card-title">${escapeHtml(x.competencyCode)} - ${escapeHtml(x.competencyDescription)}</div>
                        <div class="department-details-chip">${x.hours} \u0447.</div>
                    </div>
                    <div class="department-details-card-text">${escapeHtml(x.workTypes)}</div>
                </div>
            `).join('')
            : '<div class="department-details-card"><div class="department-details-card-text">\u041a\u043e\u043c\u043f\u0435\u0442\u0435\u043d\u0446\u0438\u0438 \u043f\u043e\u043a\u0430 \u043d\u0435 \u0434\u043e\u0431\u0430\u0432\u043b\u0435\u043d\u044b.</div></div>';

        openModal(detailsModalBackdrop);        openModal(detailsModalBackdrop);
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

        practiceEditModalTitle.textContent = 'Редактирование производственной практики';
        practiceEditModalSubtitle.textContent = 'Измените параметры практики и компетенции. Назначение студентов открывается отдельным окном.';
    }

    function buildPayload() {
        return {
            id: practiceIdInput.value ? Number(practiceIdInput.value) : null,
            practiceIndex: practiceIndexInput.value.trim(),
            name: practiceNameInput.value.trim(),
            specialtyId: Number(practiceSpecialtySelect.value || 0),
            professionalModuleCode: professionalModuleCodeInput.value.trim(),
            professionalModuleName: professionalModuleNameInput.value.trim(),
            hours: Number(practiceHoursInput.value || 0),
            startDate: practiceStartDateInput.value ? practiceStartDateInput.value : null,
            endDate: practiceEndDateInput.value ? practiceEndDateInput.value : null,
            competencies: Array.from(competenciesContainer.querySelectorAll('.competency-item')).map(item => ({
                competencyCode: item.querySelector('.competency-code-input').value.trim(),
                competencyDescription: item.querySelector('.competency-description-input').value.trim(),
                workTypes: item.querySelector('.competency-worktypes-input').value.trim(),
                hours: Number(item.querySelector('.competency-hours-input').value || 0)
            }))
        };
    }

    function buildAssignmentsPayload() {
        submittedAssignmentSnapshot = getOrderedAssignments();

        return {
            practiceId: currentAssignmentPractice ? Number(currentAssignmentPractice.id) : 0,
            studentAssignments: submittedAssignmentSnapshot.map(item => ({
                studentId: item.studentId,
                supervisorId: item.supervisorId
            }))
        };
    }

    async function openAssignmentsModal(practiceId) {
        await ensureAssignmentCatalogLoaded();

        const details = await fetchJson(`/DepartmentStaff/GetPracticeDetails?id=${practiceId}`);
        if (!details) return;

        currentAssignmentPractice = details;
        assignmentSelections = new Map(
            (details.studentAssignments || []).map(item => [
                Number(item.studentId),
                {
                    studentId: Number(item.studentId),
                    supervisorId: item.supervisorId ? Number(item.supervisorId) : null
                }
            ])
        );

        assignmentFilters = createDefaultAssignmentFilters();
        assignmentTab = assignmentSelections.size > 0 ? 'assigned' : 'all';
        resetAssignmentRowErrors();

        if (studentAssignmentSearchInput) studentAssignmentSearchInput.value = '';
        if (studentSortSelect) studentSortSelect.value = assignmentFilters.sort;

        practiceAssignmentsModalTitle.textContent = `Назначение студентов: ${details.practiceIndex}`;
        practiceAssignmentsModalSubtitle.textContent = `${details.name}. ${details.specialtyCode} ${details.specialtyName}`;

        syncAssignmentFilterWithPracticeSpecialty();
        renderAssignmentWorkspace();
        openModal(assignmentsModalBackdrop);
    }

    addCompetencyButton?.addEventListener('click', () => {
        createCompetencyItem();
    });

    openCreatePracticeButton?.addEventListener('click', () => {
        resetPracticeForm();
        practiceEditModalTitle.textContent = 'Создание производственной практики';
        practiceEditModalSubtitle.textContent = 'Сначала создайте практику и заполните компетенции. Назначение студентов открывается отдельным окном.';
        createCompetencyItem();
        openModal(editModalBackdrop);
    });

    assignmentTabs.forEach(button => {
        button.addEventListener('click', () => {
            assignmentTab = button.dataset.assignmentTab || 'assigned';
            renderAssignmentWorkspace();
        });
    });

    studentAssignmentSearchInput?.addEventListener('input', () => {
        assignmentFilters.search = studentAssignmentSearchInput.value || '';
        renderAssignmentWorkspace();
    });

    studentSpecialtyFilterSelect?.addEventListener('change', () => {
        assignmentFilters.specialtyId = studentSpecialtyFilterSelect.value || '';
        assignmentFilters.course = '';
        assignmentFilters.groupName = '';
        if (studentCourseFilterSelect) studentCourseFilterSelect.value = '';
        if (studentGroupFilterSelect) studentGroupFilterSelect.value = '';
        renderAssignmentWorkspace();
    });

    studentCourseFilterSelect?.addEventListener('change', () => {
        assignmentFilters.course = studentCourseFilterSelect.value || '';
        assignmentFilters.groupName = '';
        if (studentGroupFilterSelect) studentGroupFilterSelect.value = '';
        renderAssignmentWorkspace();
    });

    studentGroupFilterSelect?.addEventListener('change', () => {
        assignmentFilters.groupName = studentGroupFilterSelect.value || '';
        renderAssignmentWorkspace();
    });

    studentSortSelect?.addEventListener('change', () => {
        assignmentFilters.sort = studentSortSelect.value || 'name-asc';
        renderAssignmentWorkspace();
    });

    assignAllFilteredCheckbox?.addEventListener('change', () => {
        const filteredStudents = getFilteredStudents().filter(student => canAssignStudent(student));

        if (assignAllFilteredCheckbox.checked) {
            filteredStudents.forEach(student => {
                const studentId = Number(student.id);
                if (!assignmentSelections.has(studentId)) {
                    assignmentSelections.set(studentId, {
                        studentId,
                        supervisorId: null
                    });
                }
            });
        } else {
            filteredStudents.forEach(student => {
                const studentId = Number(student.id);
                assignmentSelections.delete(studentId);
                delete assignmentRowErrors[studentId];
            });
        }

        renderAssignmentWorkspace();
    });

    studentAssignmentTableBody?.addEventListener('change', event => {
        const row = event.target.closest('tr[data-student-id]');
        if (!row) return;

        const studentId = Number(row.dataset.studentId);

        if (event.target.classList.contains('assignment-row-checkbox')) {
            if (event.target.checked) {
                const existing = assignmentSelections.get(studentId);
                assignmentSelections.set(studentId, {
                    studentId,
                    supervisorId: existing ? existing.supervisorId : null
                });
            } else {
                assignmentSelections.delete(studentId);
                delete assignmentRowErrors[studentId];
            }

            renderAssignmentWorkspace();
            return;
        }

        if (event.target.classList.contains('student-assignment-supervisor-select')) {
            const existing = assignmentSelections.get(studentId);
            if (!existing) return;

            existing.supervisorId = event.target.value ? Number(event.target.value) : null;

            if (assignmentRowErrors[studentId] && assignmentRowErrors[studentId].SupervisorId) {
                delete assignmentRowErrors[studentId].SupervisorId;
                if (!assignmentRowErrors[studentId].StudentId && !assignmentRowErrors[studentId].SupervisorId) {
                    delete assignmentRowErrors[studentId];
                }
            }

            const errorBlock = row.querySelector('.field-error');
            const select = row.querySelector('.student-assignment-supervisor-select');
            const selectTrigger = row.querySelector('.student-assignment-select .custom-select-trigger');
            if (errorBlock && !assignmentRowErrors[studentId]?.SupervisorId) {
                const errorBlocks = row.querySelectorAll('.field-error');
                if (errorBlocks[1]) errorBlocks[1].textContent = '';
            }
            select?.classList.remove('input-error');
            selectTrigger?.classList.remove('input-error');
        }
    });

    editPracticeFromDetailsButton?.addEventListener('click', async () => {
        if (!currentDetails) return;
        closeModal(detailsModalBackdrop);

        try {
            await fillEditFormFromDetails(currentDetails);
            openModal(editModalBackdrop);
        } catch (error) {
            alert(error.message || 'Не удалось подготовить форму редактирования.');
        }
    });

    openAssignmentsFromDetailsButton?.addEventListener('click', async () => {
        if (!currentDetails) return;

        try {
            closeModal(detailsModalBackdrop);
            await openAssignmentsModal(currentDetails.id);
        } catch (error) {
            alert(error.message || 'Не удалось открыть окно назначения студентов.');
        }
    });

    generateAttestationSheetButton?.addEventListener('click', async () => {
        if (!currentDetails) return;

        const result = await fetchJson(`/DepartmentStaff/PreviewAttestation?id=${currentDetails.id}`);
        currentAttestationPracticeId = currentDetails.id;

        attestationPreviewContainer.innerHTML = result.html || '';
        attestationPreviewFileName.textContent = result.fileName || 'Аттестационный лист';

        openModal(attestationPreviewModalBackdrop);
    });

    downloadAttestationButton?.addEventListener('click', () => {
        if (!currentAttestationPracticeId) return;
        window.location.href = `/DepartmentStaff/DownloadAttestation?id=${currentAttestationPracticeId}`;
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

            applyPracticeFilters();
        } catch (error) {
            alert(error.message || 'Не удалось удалить практику.');
        }
    });

    savePracticeAssignmentsButton?.addEventListener('click', async () => {
        clearFieldErrors();

        try {
            const payload = buildAssignmentsPayload();

            await fetchJson('/DepartmentStaff/SavePracticeAssignments', {
                method: 'POST',
                body: JSON.stringify(payload)
            });

            closeModal(assignmentsModalBackdrop);
            window.location.reload();
        } catch (error) {
            applyValidationErrors(error.validationErrors || {}, error.message || 'Не удалось сохранить назначения студентов.');
            if (!error.validationErrors || Object.keys(error.validationErrors).length === 0) {
                setSimpleFieldError('StudentAssignments', error.message || 'Не удалось сохранить назначения студентов.');
            }
        }
    });

    practiceForm?.addEventListener('submit', async function (e) {
        e.preventDefault();
        clearFieldErrors();

        try {
            const payload = buildPayload();

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

    document.querySelectorAll('.practice-assignments-button').forEach(button => {
        button.addEventListener('click', async function () {
            const card = button.closest('.practice-card');
            if (!card) return;

            try {
                await openAssignmentsModal(card.dataset.id);
            } catch (error) {
                alert(error.message || 'Не удалось открыть окно назначения студентов.');
            }
        });
    });

    practiceSearchInput?.addEventListener('input', applyPracticeFilters);
    dateFromFilter?.addEventListener('change', applyPracticeFilters);
    dateToFilter?.addEventListener('change', applyPracticeFilters);

    buildCustomSelect('practiceSortSelect', applyPracticeFilters);
    buildCustomSelect('specialtyFilterSelect', applyPracticeFilters);
    buildCustomSelect('practiceSpecialtySelect');
    buildCustomSelect('studentSpecialtyFilterSelect');
    buildCustomSelect('studentCourseFilterSelect');
    buildCustomSelect('studentGroupFilterSelect');
    buildCustomSelect('studentSortSelect');

    Promise.resolve(loadFormMetadata()).then(() => {
        applyPracticeFilters();
    });

    switchPanel('practicesPanel');
});
