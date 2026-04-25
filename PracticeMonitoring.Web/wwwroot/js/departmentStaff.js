document.addEventListener('DOMContentLoaded', function () {
    const panelButtons = Array.from(document.querySelectorAll('.department-nav-button'));
    const panels = Array.from(document.querySelectorAll('.department-panel'));
    const practiceStatusButtons = Array.from(document.querySelectorAll('[data-practice-status]'));
    const logConsoleButtons = Array.from(document.querySelectorAll('[data-log-console]'));
    const logConsoles = Array.from(document.querySelectorAll('.department-console'));

    const practiceSearchInput = document.getElementById('practiceSearchInput');
    const practicesList = document.getElementById('practicesList');
    const practicesCountLabel = document.getElementById('practicesCountLabel');
    const dateFromFilter = document.getElementById('dateFromFilter');
    const dateToFilter = document.getElementById('dateToFilter');
    const specialtyFilterSelect = document.getElementById('specialtyFilterSelect');
    const practiceSortSelect = document.getElementById('practiceSortSelect');
    const supervisorSearchInput = document.getElementById('supervisorSearchInput');
    const supervisorSortSelect = document.getElementById('supervisorSortSelect');
    const supervisorsList = document.getElementById('supervisorsList');
    const supervisorsCountLabel = document.getElementById('supervisorsCountLabel');

    const editModalBackdrop = document.getElementById('practiceEditModalBackdrop');
    const detailsModalBackdrop = document.getElementById('practiceDetailsModalBackdrop');
    const assignmentsModalBackdrop = document.getElementById('practiceAssignmentsModalBackdrop');
    const attestationPreviewModalBackdrop = document.getElementById('attestationPreviewModalBackdrop');
    const supervisorDetailsModalBackdrop = document.getElementById('supervisorDetailsModalBackdrop');

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
    const closeSupervisorDetailsModalButton = document.getElementById('closeSupervisorDetailsModalButton');
    const closeSupervisorDetailsFooterButton = document.getElementById('closeSupervisorDetailsFooterButton');
    const supervisorDetailsTitle = document.getElementById('supervisorDetailsTitle');
    const supervisorDetailsSubtitle = document.getElementById('supervisorDetailsSubtitle');
    const supervisorDetailsOverviewTitle = document.getElementById('supervisorDetailsOverviewTitle');
    const supervisorDetailsOverviewSubtitle = document.getElementById('supervisorDetailsOverviewSubtitle');
    const supervisorDetailsOverviewStats = document.getElementById('supervisorDetailsOverviewStats');
    const supervisorDetailsStudents = document.getElementById('supervisorDetailsStudents');
    const supervisorDetailsPractices = document.getElementById('supervisorDetailsPractices');
    const supervisorStudentsTabCounter = document.getElementById('supervisorStudentsTabCounter');
    const supervisorPracticesTabCounter = document.getElementById('supervisorPracticesTabCounter');
    const supervisorDetailsTabButtons = Array.from(document.querySelectorAll('[data-supervisor-details-tab]'));
    const supervisorDetailsTabs = {
        students: document.getElementById('supervisorStudentsTab'),
        practices: document.getElementById('supervisorPracticesTab')
    };
    const warningModalBackdrop = document.getElementById('warningModalBackdrop');
    const warningModalTitle = document.getElementById('warningModalTitle');
    const warningModalSubtitle = document.getElementById('warningModalSubtitle');
    const warningModalMessage = document.getElementById('warningModalMessage');
    const warningModalConsequences = document.getElementById('warningModalConsequences');
    const closeWarningModalButton = document.getElementById('closeWarningModalButton');
    const cancelWarningModalButton = document.getElementById('cancelWarningModalButton');
    const confirmWarningModalButton = document.getElementById('confirmWarningModalButton');
    const errorModalBackdrop = document.getElementById('errorModalBackdrop');
    const errorModalTitle = document.getElementById('errorModalTitle');
    const errorModalSubtitle = document.getElementById('errorModalSubtitle');
    const errorModalMessage = document.getElementById('errorModalMessage');
    const errorModalDetails = document.getElementById('errorModalDetails');
    const closeErrorModalButton = document.getElementById('closeErrorModalButton');
    const confirmErrorModalButton = document.getElementById('confirmErrorModalButton');

    let specialties = [];
    let students = [];
    let supervisors = [];
    let studentLookup = new Map();
    let currentDetails = null;
    let currentSupervisorDetails = null;
    let currentAssignmentPractice = null;
    let currentAttestationPracticeId = null;
    let assignmentCatalogLoaded = false;
    let assignmentCatalogLoading = false;
    let assignmentTab = 'all';
    let practiceStatusTab = 'active';
    let assignmentFilters = createDefaultAssignmentFilters();
    let assignmentSelections = new Map();
    let assignmentRowErrors = {};
    let submittedAssignmentSnapshot = [];
    let originalEditSpecialtyId = null;
    let originalEditAssignedStudentsCount = 0;
    let specialtyChangeStudentResetConfirmed = false;
    let warningModalResolver = null;
    let errorModalResolver = null;
    let supervisorDetailsTab = 'students';

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

        const text = String(value);
        if (/^\d{4}-\d{2}-\d{2}/.test(text)) {
            const [year, month, day] = text.slice(0, 10).split('-');
            return `${day}.${month}.${year}`;
        }

        const date = new Date(text);
        if (Number.isNaN(date.getTime())) return text;
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

    function setPracticeStatusTab(status) {
        practiceStatusTab = status === 'completed' ? 'completed' : 'active';

        practiceStatusButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.practiceStatus === practiceStatusTab);
        });

        applyPracticeFilters();
    }

    function switchLogConsole(targetId) {
        logConsoleButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.logConsole === targetId);
        });

        logConsoles.forEach(consoleElement => {
            consoleElement.classList.toggle('active', consoleElement.id === targetId);
        });
    }

    function resetAssignmentRowErrors() {
        assignmentRowErrors = {};
    }

    function clearFieldErrors() {
        document.querySelectorAll('.field-error').forEach(block => {
            block.textContent = '';
        });

        document.querySelectorAll('.input-error').forEach(input => {
            input.classList.remove('input-error');
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

    function closeWarningModal(result) {
        if (warningModalResolver) {
            const resolver = warningModalResolver;
            warningModalResolver = null;
            resolver(result);
        }

        warningModalMessage && (warningModalMessage.textContent = '');
        warningModalConsequences && (warningModalConsequences.innerHTML = '');
        warningModalTitle && (warningModalTitle.textContent = 'Подтверждение действия');
        warningModalSubtitle && (warningModalSubtitle.textContent = 'Проверьте последствия перед продолжением.');

        closeModal(warningModalBackdrop);
    }

    function showWarningModal(options) {
        if (warningModalResolver) {
            closeWarningModal(false);
        }

        if (warningModalTitle) {
            warningModalTitle.textContent = options.title || 'Подтверждение действия';
        }

        if (warningModalSubtitle) {
            warningModalSubtitle.textContent = options.subtitle || 'Проверьте последствия перед продолжением.';
        }

        if (warningModalMessage) {
            warningModalMessage.textContent = options.message || '';
        }

        if (warningModalConsequences) {
            warningModalConsequences.innerHTML = (options.consequences || []).map(item => {
                if (typeof item === 'string') {
                    return `<div class="department-warning-item"><div class="department-warning-item-text">${escapeHtml(item)}</div></div>`;
                }

                const text = escapeHtml(item.text || '');
                const accent = item.accent ? `<div class="department-warning-item-accent">${escapeHtml(item.accent)}</div>` : '';
                return `
                    <div class="department-warning-item">
                        <div>
                            <div class="department-warning-item-text">${text}</div>
                            ${accent}
                        </div>
                    </div>
                `;
            }).join('');
        }

        if (confirmWarningModalButton) {
            confirmWarningModalButton.textContent = options.confirmText || 'Продолжить';
        }

        openModal(warningModalBackdrop);

        return new Promise(resolve => {
            warningModalResolver = resolve;
        });
    }

    function closeErrorModal() {
        if (errorModalResolver) {
            const resolver = errorModalResolver;
            errorModalResolver = null;
            resolver();
        }

        if (errorModalTitle) {
            errorModalTitle.textContent = 'Не удалось выполнить действие';
        }

        if (errorModalSubtitle) {
            errorModalSubtitle.textContent = 'Система остановила операцию из-за ошибки.';
        }

        if (errorModalMessage) {
            errorModalMessage.textContent = '';
        }

        if (errorModalDetails) {
            errorModalDetails.innerHTML = '';
        }

        closeModal(errorModalBackdrop);
    }

    function showErrorModal(options) {
        if (errorModalResolver) {
            closeErrorModal();
        }

        if (errorModalTitle) {
            errorModalTitle.textContent = options.title || 'Не удалось выполнить действие';
        }

        if (errorModalSubtitle) {
            errorModalSubtitle.textContent = options.subtitle || 'Система остановила операцию из-за ошибки.';
        }

        if (errorModalMessage) {
            errorModalMessage.textContent = options.message || 'Операцию не удалось завершить.';
        }

        if (errorModalDetails) {
            errorModalDetails.innerHTML = (options.details || []).map(item => {
                if (typeof item === 'string') {
                    return `<div class="department-warning-item department-warning-item-error"><div class="department-warning-item-text">${escapeHtml(item)}</div></div>`;
                }

                const text = escapeHtml(item.text || '');
                const accent = item.accent ? `<div class="department-warning-item-accent">${escapeHtml(item.accent)}</div>` : '';
                return `
                    <div class="department-warning-item department-warning-item-error">
                        <div>
                            <div class="department-warning-item-text">${text}</div>
                            ${accent}
                        </div>
                    </div>
                `;
            }).join('');
        }

        openModal(errorModalBackdrop);

        return new Promise(resolve => {
            errorModalResolver = resolve;
        });
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
            const trigger = document.querySelector(`.custom-select[data-target="${input.id}"] .custom-select-trigger`);
            trigger?.classList.add('input-error');
        }
    }

    function applyValidationErrors(errors, globalMessage) {
        clearFieldErrors();

        if (globalMessage) {
            showGlobalError(globalMessage);
        }

        let hasAssignmentErrors = false;

        Object.entries(errors || {}).forEach(([key, messages]) => {
            if (!Array.isArray(messages) || messages.length === 0) return;

            if (key.startsWith('Competencies[')) {
                const match = key.match(/^Competencies\[(\d+)\]\.(.+)$/);
                if (!match) return;

                const index = Number(match[1]);
                const field = match[2];
                const card = competenciesContainer?.querySelectorAll('.competency-item')[index];
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
                hasAssignmentErrors = true;
                return;
            }

            setSimpleFieldError(key, messages[0]);
        });

        if (hasAssignmentErrors) {
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
            const options = Array.from(nativeSelect.options);
            menu.innerHTML = '';

            options.forEach(option => {
                const item = document.createElement('div');
                item.className = 'custom-select-option';
                item.dataset.value = option.value;
                item.textContent = option.textContent || '';

                if (option.selected) {
                    item.classList.add('selected');
                    trigger.textContent = option.textContent || '';
                }

                item.addEventListener('click', () => {
                    nativeSelect.value = option.value;
                    nativeSelect.dispatchEvent(new Event('change', { bubbles: true }));
                    custom.classList.remove('open');
                });

                menu.appendChild(item);
            });

            const selected = options.find(option => option.selected) || options[0];
            if (selected) {
                trigger.textContent = selected.textContent || '';
                menu.querySelectorAll('.custom-select-option').forEach(item => {
                    item.classList.toggle('selected', item.dataset.value === selected.value);
                });
            }

            trigger.disabled = nativeSelect.disabled;
            custom.classList.toggle('is-disabled', nativeSelect.disabled);
        };

        nativeSelect._customSelectRebuild = rebuild;

        if (!custom.dataset.bound) {
            trigger.addEventListener('click', () => {
                if (nativeSelect.disabled) return;

                document.querySelectorAll('.custom-select.open').forEach(item => {
                    if (item !== custom) item.classList.remove('open');
                });

                custom.classList.toggle('open');
            });

            nativeSelect.addEventListener('change', () => {
                rebuild();
                if (typeof onChange === 'function') {
                    onChange(nativeSelect.value);
                }
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

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.custom-select')) {
            document.querySelectorAll('.custom-select.open').forEach(item => item.classList.remove('open'));
        }
    });

    function openModal(backdrop) {
        backdrop?.classList.add('open');
    }

    function closeModal(backdrop) {
        backdrop?.classList.remove('open');
    }

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

        if (currentValue && Array.from(select.options).some(option => option.value === currentValue)) {
            select.value = currentValue;
        }

        refreshCustomSelect(select);
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

        specialtyFilterSelect?.dispatchEvent(new Event('change', { bubbles: true }));
        practiceSpecialtySelect?.dispatchEvent(new Event('change', { bubbles: true }));
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
            const isCompleted = (card.dataset.isCompleted || 'false') === 'true';

            const matchesSearch = !search || getPracticeSearchText(card).includes(search);
            const matchesSpecialty = !specialtyId || specialtyId === cardSpecialtyId;
            const matchesDateFrom = !dateFrom || endDate >= dateFrom;
            const matchesDateTo = !dateTo || startDate <= dateTo;
            const matchesStatus = practiceStatusTab === 'completed' ? isCompleted : !isCompleted;

            card.style.display = matchesSearch && matchesSpecialty && matchesDateFrom && matchesDateTo && matchesStatus ? '' : 'none';
        });

        cards = cards.filter(card => card.style.display !== 'none');

        cards.sort((left, right) => {
            const leftStart = left.dataset.startDate || '';
            const rightStart = right.dataset.startDate || '';
            const leftEnd = left.dataset.endDate || '';
            const rightEnd = right.dataset.endDate || '';
            const leftIndex = (left.dataset.practiceIndex || '').toLowerCase();
            const rightIndex = (right.dataset.practiceIndex || '').toLowerCase();
            const leftHours = Number(left.dataset.hours || 0);
            const rightHours = Number(right.dataset.hours || 0);

            switch (sort) {
                case 'date-desc':
                    return rightStart.localeCompare(leftStart) || rightEnd.localeCompare(leftEnd);
                case 'index-asc':
                    return leftIndex.localeCompare(rightIndex, 'ru');
                case 'hours-desc':
                    return rightHours - leftHours || leftStart.localeCompare(rightStart);
                case 'date-asc':
                default:
                    return leftStart.localeCompare(rightStart) || leftEnd.localeCompare(rightEnd);
            }
        });

        cards.forEach(card => practicesList?.appendChild(card));

        if (practicesCountLabel) {
            practicesCountLabel.textContent = `Показано практик: ${cards.length}`;
        }
    }

    function getSupervisorCards() {
        return Array.from(document.querySelectorAll('.supervisor-card'));
    }

    function applySupervisorFilters() {
        const search = (supervisorSearchInput?.value || '').trim().toLowerCase();
        const sort = supervisorSortSelect?.value || 'name-asc';

        let cards = getSupervisorCards();

        cards.forEach(card => {
            const haystack = `${card.dataset.fullName || ''} ${card.dataset.email || ''}`.toLowerCase();
            const matches = !search || haystack.includes(search);
            card.style.display = matches ? '' : 'none';
        });

        cards = cards.filter(card => card.style.display !== 'none');

        cards.sort((left, right) => {
            const leftName = left.dataset.fullName || '';
            const rightName = right.dataset.fullName || '';
            const leftStudents = Number(left.dataset.studentsCount || 0);
            const rightStudents = Number(right.dataset.studentsCount || 0);
            const leftPractices = Number(left.dataset.practicesCount || 0);
            const rightPractices = Number(right.dataset.practicesCount || 0);

            switch (sort) {
                case 'students-desc':
                    return rightStudents - leftStudents || leftName.localeCompare(rightName, 'ru');
                case 'practices-desc':
                    return rightPractices - leftPractices || leftName.localeCompare(rightName, 'ru');
                case 'name-asc':
                default:
                    return leftName.localeCompare(rightName, 'ru');
            }
        });

        cards.forEach(card => supervisorsList?.appendChild(card));

        if (supervisorsCountLabel) {
            supervisorsCountLabel.textContent = `Показано руководителей: ${cards.length}`;
        }
    }

    function setSupervisorDetailsTab(tab) {
        supervisorDetailsTab = tab === 'practices' ? 'practices' : 'students';

        supervisorDetailsTabButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.supervisorDetailsTab === supervisorDetailsTab);
        });

        Object.entries(supervisorDetailsTabs).forEach(([key, element]) => {
            element?.classList.toggle('active', key === supervisorDetailsTab);
        });
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
            if (studentCourseFilterSelect) studentCourseFilterSelect.value = '';
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
            if (studentGroupFilterSelect) studentGroupFilterSelect.value = '';
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
            return 'Откройте окно назначения, чтобы загрузить каталог студентов.';
        }

        if (assignmentTab === 'assigned') {
            return 'Назначенных студентов пока нет. Переключитесь на полный список и отметьте нужных.';
        }

        return 'По текущим фильтрам студенты не найдены.';
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

            assignAllFilteredCheckbox.disabled = assignmentTab === 'assigned' || assignableFilteredStudents.length === 0;
            assignAllFilteredCheckbox.checked = assignmentTab !== 'assigned' && allAssignableSelected;
        }

        if (studentAssignmentSummary) {
            const visibleAssignedCount = filteredStudents.filter(student => assignmentSelections.has(Number(student.id))).length;
            studentAssignmentSummary.textContent = `Показано студентов: ${filteredStudents.length}. В текущей выборке назначено: ${visibleAssignedCount}. Всего назначено: ${assignedCount}.`;
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
            const checkboxDisabled = !isAllowed && !isAssigned;
            const rowErrors = assignmentRowErrors[studentId] || {};
            const studentError = rowErrors.StudentId || '';
            const supervisorError = rowErrors.SupervisorId || '';
            const supervisorSelectId = `assignmentSupervisorSelect-${studentId}`;
            const badges = [
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
                                       ${checkboxDisabled ? 'disabled' : ''} />
                                <span>${isAssigned ? 'Включён' : 'Назначить'}</span>
                            </span>
                            <span class="student-assignment-toggle-state">
                                ${isAssigned ? 'Студент входит в состав практики' : 'Строка пока не включена'}
                            </span>
                        </label>
                    </td>
                    <td>
                        <div class="student-assignment-student">
                            <div class="student-assignment-student-name">${escapeHtml(student.fullName)}</div>
                            <div class="student-assignment-student-meta">${badges}</div>
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
                        <div class="student-assignment-cell-main">${escapeHtml(student.course != null ? String(student.course) : '-')}</div>
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
        if (!competencyTemplate || !competenciesContainer) return;

        const fragment = competencyTemplate.content.cloneNode(true);
        const element = fragment.querySelector('.competency-item');
        if (!element) return;

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
        if (practiceIdInput) practiceIdInput.value = '';
        if (practiceIndexInput) practiceIndexInput.value = '';
        if (practiceNameInput) practiceNameInput.value = '';
        if (practiceSpecialtySelect) practiceSpecialtySelect.value = '';
        if (practiceHoursInput) practiceHoursInput.value = '';
        if (professionalModuleCodeInput) professionalModuleCodeInput.value = '';
        if (professionalModuleNameInput) professionalModuleNameInput.value = '';
        if (practiceStartDateInput) practiceStartDateInput.value = '';
        if (practiceEndDateInput) practiceEndDateInput.value = '';
        if (competenciesContainer) competenciesContainer.innerHTML = '';

        currentAssignmentPractice = null;
        assignmentSelections = new Map();
        submittedAssignmentSnapshot = [];
        assignmentTab = 'all';
        assignmentFilters = createDefaultAssignmentFilters();
        originalEditSpecialtyId = null;
        originalEditAssignedStudentsCount = 0;
        specialtyChangeStudentResetConfirmed = false;

        if (studentAssignmentSearchInput) studentAssignmentSearchInput.value = '';
        if (studentSortSelect) studentSortSelect.value = assignmentFilters.sort;

        clearFieldErrors();
        practiceSpecialtySelect?.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function renderPracticeDetails(details) {
        if (practiceDetailsTitle) {
            practiceDetailsTitle.textContent = `${details.practiceIndex} - ${details.name}`;
        }

        if (practiceDetailsSubtitle) {
            practiceDetailsSubtitle.textContent = `${details.specialtyCode} ${details.specialtyName}`;
        }

        if (practiceDetailsOverviewTitle) {
            practiceDetailsOverviewTitle.textContent = details.name || 'Производственная практика';
        }

        if (practiceDetailsOverviewSubtitle) {
            practiceDetailsOverviewSubtitle.textContent = `${details.practiceIndex} • ${details.specialtyCode} ${details.specialtyName}`;
        }

        if (practiceDetailsOverviewStats) {
            practiceDetailsOverviewStats.innerHTML = `
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">Часы</span>
                    <span class="department-details-overview-stat-value">${details.hours}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">Студенты</span>
                    <span class="department-details-overview-stat-value">${details.studentAssignments.length}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">Компетенции</span>
                    <span class="department-details-overview-stat-value">${details.competencies.length}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">Период</span>
                    <span class="department-details-overview-stat-value">${formatDate(details.startDate)} - ${formatDate(details.endDate)}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">Статус</span>
                    <span class="department-details-overview-stat-value">${details.isCompleted ? 'Завершилась' : 'Активна'}</span>
                </div>
            `;
        }

        if (practiceDetailsInfo) {
            practiceDetailsInfo.innerHTML = `
                <div class="department-details-item">
                    <span class="department-details-label">Название практики</span>
                    <span class="department-details-value department-details-value-strong">${escapeHtml(details.name)}</span>
                </div>
                <div class="department-details-item">
                    <span class="department-details-label">Специальность</span>
                    <span class="department-details-value">${escapeHtml(details.specialtyCode || '-')} ${escapeHtml(details.specialtyName || '')}</span>
                </div>
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
                <div class="department-details-item">
                    <span class="department-details-label">Статус</span>
                    <span class="department-details-value">${details.isCompleted ? 'Завершившаяся практика' : 'Активная практика'}</span>
                </div>
            `;
        }

        if (practiceDetailsAssignments) {
            practiceDetailsAssignments.innerHTML = details.studentAssignments.length
                ? details.studentAssignments.map(item => `
                    <div class="department-details-card">
                        <div class="department-details-card-header">
                            <div class="department-details-card-title">${escapeHtml(item.studentFullName)}</div>
                            <div class="department-details-chip">${escapeHtml(item.studentCourse != null ? `${item.studentCourse} курс` : 'Курс не указан')}</div>
                        </div>
                        <div class="department-details-card-meta">
                            <span class="department-details-inline-chip">${escapeHtml(item.studentSpecialtyCode || '-')} ${escapeHtml(item.studentSpecialtyName || '')}</span>
                            <span class="department-details-inline-chip">${escapeHtml(item.studentGroupName || 'Группа не указана')}</span>
                        </div>
                        <div class="department-details-card-text">Руководитель от техникума: ${escapeHtml(item.supervisorFullName || 'Не назначен')}</div>
                    </div>
                `).join('')
                : '<div class="department-details-card"><div class="department-details-card-text">Студенты пока не назначены.</div></div>';
        }

        if (practiceDetailsCompetencies) {
            practiceDetailsCompetencies.innerHTML = details.competencies.length
                ? details.competencies.map(item => `
                    <div class="department-details-card">
                        <div class="department-details-card-header">
                            <div class="department-details-card-title">${escapeHtml(item.competencyCode)} - ${escapeHtml(item.competencyDescription)}</div>
                            <div class="department-details-chip">${item.hours} ч.</div>
                        </div>
                        <div class="department-details-card-text">${escapeHtml(item.workTypes)}</div>
                    </div>
                `).join('')
                : '<div class="department-details-card"><div class="department-details-card-text">Компетенции пока не добавлены.</div></div>';
        }
    }

    function renderSupervisorDetails(details) {
        currentSupervisorDetails = details;

        if (supervisorDetailsTitle) {
            supervisorDetailsTitle.textContent = `Руководитель: ${details.fullName}`;
        }

        if (supervisorDetailsSubtitle) {
            supervisorDetailsSubtitle.textContent = details.email || 'Руководитель от техникума';
        }

        if (supervisorDetailsOverviewTitle) {
            supervisorDetailsOverviewTitle.textContent = details.fullName || 'Руководитель';
        }

        if (supervisorDetailsOverviewSubtitle) {
            supervisorDetailsOverviewSubtitle.textContent = details.email || 'Нагрузка по студентам и производственным практикам';
        }

        if (supervisorDetailsOverviewStats) {
            supervisorDetailsOverviewStats.innerHTML = `
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">Студенты</span>
                    <span class="department-details-overview-stat-value">${details.assignedStudentsCount}</span>
                </div>
                <div class="department-details-overview-stat">
                    <span class="department-details-overview-stat-label">Практики</span>
                    <span class="department-details-overview-stat-value">${details.practicesCount}</span>
                </div>

            `;
        }

        if (supervisorStudentsTabCounter) {
            supervisorStudentsTabCounter.textContent = String((details.students || []).length);
        }

        if (supervisorPracticesTabCounter) {
            supervisorPracticesTabCounter.textContent = String((details.practices || []).length);
        }

        if (supervisorDetailsStudents) {
            supervisorDetailsStudents.innerHTML = (details.students || []).length
                ? details.students.map(item => `
                    <div class="department-details-card">
                        <div class="department-details-card-header">
                            <div class="department-details-card-title">${escapeHtml(item.studentFullName)}</div>
                            <div class="department-details-chip">${escapeHtml(item.course != null ? `${item.course} курс` : 'Курс не указан')}</div>
                        </div>
                        <div class="department-details-card-meta">
                            <span class="department-details-inline-chip">${escapeHtml(item.groupName || 'Группа не указана')}</span>
                            <span class="department-details-inline-chip">${escapeHtml(item.practiceIndex)} ${escapeHtml(item.practiceName)}</span>
                        </div>
                    </div>
                `).join('')
                : '<div class="department-details-card"><div class="department-details-card-text">У этого руководителя пока нет назначенных студентов.</div></div>';
        }

        if (supervisorDetailsPractices) {
            supervisorDetailsPractices.innerHTML = (details.practices || []).length
                ? details.practices.map(item => `
                    <div class="department-details-card">
                        <div class="department-details-card-header">
                            <div class="department-details-card-title">${escapeHtml(item.practiceIndex)} - ${escapeHtml(item.practiceName)}</div>
                            <div class="department-details-chip">${item.studentsCount} студ.</div>
                        </div>
                        <div class="department-details-card-meta">
                            <span class="department-details-inline-chip">${escapeHtml(item.specialtyCode)} ${escapeHtml(item.specialtyName)}</span>
                        </div>
                    </div>
                `).join('')
                : '<div class="department-details-card"><div class="department-details-card-text">У этого руководителя пока нет производственных практик с назначенными студентами.</div></div>';
        }
    }

    async function openDetails(practiceId) {
        const details = await fetchJson(`/DepartmentStaff/GetPracticeDetails?id=${practiceId}`);
        if (!details) return;

        currentDetails = details;
        renderPracticeDetails(details);
        openModal(detailsModalBackdrop);
    }

    async function openSupervisorDetails(supervisorId) {
        const details = await fetchJson(`/DepartmentStaff/GetSupervisorDetails?id=${supervisorId}`);
        if (!details) return;

        renderSupervisorDetails(details);
        setSupervisorDetailsTab('students');
        openModal(supervisorDetailsModalBackdrop);
    }

    async function fillEditFormFromDetails(details) {
        resetPracticeForm();

        if (practiceIdInput) practiceIdInput.value = details.id;
        if (practiceIndexInput) practiceIndexInput.value = details.practiceIndex || '';
        if (practiceNameInput) practiceNameInput.value = details.name || '';
        if (practiceSpecialtySelect) practiceSpecialtySelect.value = String(details.specialtyId || '');
        if (practiceHoursInput) practiceHoursInput.value = details.hours || '';
        if (professionalModuleCodeInput) professionalModuleCodeInput.value = details.professionalModuleCode || '';
        if (professionalModuleNameInput) professionalModuleNameInput.value = details.professionalModuleName || '';
        if (practiceStartDateInput) practiceStartDateInput.value = String(details.startDate || '').slice(0, 10);
        if (practiceEndDateInput) practiceEndDateInput.value = String(details.endDate || '').slice(0, 10);

        originalEditSpecialtyId = Number(details.specialtyId || 0);
        originalEditAssignedStudentsCount = Array.isArray(details.studentAssignments) ? details.studentAssignments.length : 0;
        specialtyChangeStudentResetConfirmed = false;

        practiceSpecialtySelect?.dispatchEvent(new Event('change', { bubbles: true }));

        (details.competencies || []).forEach(item => createCompetencyItem(item));

        if (practiceEditModalTitle) {
            practiceEditModalTitle.textContent = 'Редактирование производственной практики';
        }

        if (practiceEditModalSubtitle) {
            practiceEditModalSubtitle.textContent = 'Измените параметры практики и перечень компетенций. Назначение студентов открывается отдельным окном.';
        }
    }

    function buildPayload() {
        return {
            id: practiceIdInput?.value ? Number(practiceIdInput.value) : null,
            practiceIndex: practiceIndexInput?.value.trim() || '',
            name: practiceNameInput?.value.trim() || '',
            specialtyId: Number(practiceSpecialtySelect?.value || 0),
            professionalModuleCode: professionalModuleCodeInput?.value.trim() || '',
            professionalModuleName: professionalModuleNameInput?.value.trim() || '',
            hours: Number(practiceHoursInput?.value || 0),
            startDate: practiceStartDateInput?.value || null,
            endDate: practiceEndDateInput?.value || null,
            confirmSpecialtyChangeStudentReset: specialtyChangeStudentResetConfirmed,
            competencies: Array.from(competenciesContainer?.querySelectorAll('.competency-item') || []).map(item => ({
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
        submittedAssignmentSnapshot = [];
        resetAssignmentRowErrors();

        if (studentAssignmentSearchInput) studentAssignmentSearchInput.value = '';
        if (studentSortSelect) studentSortSelect.value = assignmentFilters.sort;

        if (practiceAssignmentsModalTitle) {
            practiceAssignmentsModalTitle.textContent = `Назначение студентов: ${details.practiceIndex}`;
        }

        if (practiceAssignmentsModalSubtitle) {
            practiceAssignmentsModalSubtitle.textContent = `${details.name}. ${details.specialtyCode} ${details.specialtyName}`;
        }

        syncAssignmentFilterWithPracticeSpecialty();
        renderAssignmentWorkspace();
        openModal(assignmentsModalBackdrop);
    }

    closePracticeEditModalButton?.addEventListener('click', () => closeModal(editModalBackdrop));
    cancelPracticeEditModalButton?.addEventListener('click', () => closeModal(editModalBackdrop));
    closePracticeDetailsModalButton?.addEventListener('click', () => closeModal(detailsModalBackdrop));
    closePracticeAssignmentsModalButton?.addEventListener('click', () => closeModal(assignmentsModalBackdrop));
    cancelPracticeAssignmentsModalButton?.addEventListener('click', () => closeModal(assignmentsModalBackdrop));
    closeAttestationPreviewModalButton?.addEventListener('click', () => closeModal(attestationPreviewModalBackdrop));
    cancelAttestationPreviewButton?.addEventListener('click', () => closeModal(attestationPreviewModalBackdrop));
    closeSupervisorDetailsModalButton?.addEventListener('click', () => closeModal(supervisorDetailsModalBackdrop));
    closeSupervisorDetailsFooterButton?.addEventListener('click', () => closeModal(supervisorDetailsModalBackdrop));

    editModalBackdrop?.addEventListener('click', event => {
        if (event.target === editModalBackdrop) closeModal(editModalBackdrop);
    });

    detailsModalBackdrop?.addEventListener('click', event => {
        if (event.target === detailsModalBackdrop) closeModal(detailsModalBackdrop);
    });

    assignmentsModalBackdrop?.addEventListener('click', event => {
        if (event.target === assignmentsModalBackdrop) closeModal(assignmentsModalBackdrop);
    });

    attestationPreviewModalBackdrop?.addEventListener('click', event => {
        if (event.target === attestationPreviewModalBackdrop) closeModal(attestationPreviewModalBackdrop);
    });

    supervisorDetailsModalBackdrop?.addEventListener('click', event => {
        if (event.target === supervisorDetailsModalBackdrop) closeModal(supervisorDetailsModalBackdrop);
    });

    warningModalBackdrop?.addEventListener('click', event => {
        if (event.target === warningModalBackdrop) closeWarningModal(false);
    });

    closeWarningModalButton?.addEventListener('click', () => closeWarningModal(false));
    cancelWarningModalButton?.addEventListener('click', () => closeWarningModal(false));
    confirmWarningModalButton?.addEventListener('click', () => closeWarningModal(true));

    errorModalBackdrop?.addEventListener('click', event => {
        if (event.target === errorModalBackdrop) closeErrorModal();
    });

    closeErrorModalButton?.addEventListener('click', () => closeErrorModal());
    confirmErrorModalButton?.addEventListener('click', () => closeErrorModal());

    panelButtons.forEach(button => {
        button.addEventListener('click', () => {
            switchPanel(button.dataset.panelTarget);
        });
    });

    practiceStatusButtons.forEach(button => {
        button.addEventListener('click', () => {
            setPracticeStatusTab(button.dataset.practiceStatus || 'active');
        });
    });

    logConsoleButtons.forEach(button => {
        button.addEventListener('click', () => {
            switchLogConsole(button.dataset.logConsole || 'practiceChangesConsole');
        });
    });

    supervisorDetailsTabButtons.forEach(button => {
        button.addEventListener('click', () => {
            setSupervisorDetailsTab(button.dataset.supervisorDetailsTab || 'students');
        });
    });

    addCompetencyButton?.addEventListener('click', () => {
        createCompetencyItem();
    });

    openCreatePracticeButton?.addEventListener('click', () => {
        resetPracticeForm();

        if (practiceEditModalTitle) {
            practiceEditModalTitle.textContent = 'Создание производственной практики';
        }

        if (practiceEditModalSubtitle) {
            practiceEditModalSubtitle.textContent = 'Сначала создайте практику и заполните компетенции. Назначение студентов выполняется в отдельном окне.';
        }

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
        const target = event.target;
        const row = target.closest('tr[data-student-id]');
        if (!row) return;

        const studentId = Number(row.dataset.studentId);

        if (target.classList.contains('assignment-row-checkbox')) {
            if (target.checked) {
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

        if (target.classList.contains('student-assignment-supervisor-select')) {
            const existing = assignmentSelections.get(studentId);
            if (!existing) return;

            existing.supervisorId = target.value ? Number(target.value) : null;

            if (assignmentRowErrors[studentId] && assignmentRowErrors[studentId].SupervisorId) {
                delete assignmentRowErrors[studentId].SupervisorId;
                if (!assignmentRowErrors[studentId].StudentId && !assignmentRowErrors[studentId].SupervisorId) {
                    delete assignmentRowErrors[studentId];
                }
            }

            const errorBlock = row.querySelector('.student-assignment-supervisor-wrap .field-error');
            const trigger = row.querySelector('.student-assignment-select .custom-select-trigger');
            if (errorBlock) errorBlock.textContent = '';
            target.classList.remove('input-error');
            trigger?.classList.remove('input-error');
        }
    });

    practiceSpecialtySelect?.addEventListener('change', () => {
        const currentSpecialtyId = Number(practiceSpecialtySelect.value || 0);
        specialtyChangeStudentResetConfirmed = currentSpecialtyId === originalEditSpecialtyId;
    });

    editPracticeFromDetailsButton?.addEventListener('click', async () => {
        if (!currentDetails) return;
        closeModal(detailsModalBackdrop);

        try {
            await fillEditFormFromDetails(currentDetails);
            openModal(editModalBackdrop);
        } catch (error) {
            await showErrorModal({
                title: 'Не удалось открыть режим редактирования',
                subtitle: 'Карточка практики не была подготовлена',
                message: error.message || 'Система не смогла загрузить данные практики для редактирования.',
                details: [
                    {
                        text: 'Форма редактирования осталась закрытой, данные на странице не изменились.',
                        accent: 'Можно повторить попытку ещё раз.'
                    }
                ]
            });
        }
    });

    openAssignmentsFromDetailsButton?.addEventListener('click', async () => {
        if (!currentDetails) return;

        try {
            closeModal(detailsModalBackdrop);
            await openAssignmentsModal(currentDetails.id);
        } catch (error) {
            await showErrorModal({
                title: 'Не удалось открыть назначение студентов',
                subtitle: 'Окно назначения не было загружено',
                message: error.message || 'Система не смогла открыть рабочее окно назначения студентов для выбранной практики.',
                details: [
                    {
                        text: 'Состав студентов не изменился и текущие назначения остались без правок.',
                        accent: 'Попробуй повторить открытие окна ещё раз.'
                    }
                ]
            });
        }
    });

    generateAttestationSheetButton?.addEventListener('click', async () => {
        if (!currentDetails) return;

        const result = await fetchJson(`/DepartmentStaff/PreviewAttestation?id=${currentDetails.id}`);
        currentAttestationPracticeId = currentDetails.id;

        if (attestationPreviewContainer) {
            attestationPreviewContainer.innerHTML = result.html || '';
        }

        if (attestationPreviewFileName) {
            attestationPreviewFileName.textContent = result.fileName || 'Аттестационный лист';
        }

        openModal(attestationPreviewModalBackdrop);
    });

    downloadAttestationButton?.addEventListener('click', () => {
        if (!currentAttestationPracticeId) return;
        window.location.href = `/DepartmentStaff/DownloadAttestation?id=${currentAttestationPracticeId}`;
    });

    deletePracticeButton?.addEventListener('click', async () => {
        if (!currentDetails) return;

        const confirmed = await showWarningModal({
            title: 'Удаление производственной практики',
            subtitle: `${currentDetails.practiceIndex} • ${currentDetails.name}`,
            message: 'Это действие удалит карточку практики из рабочего раздела. Отменить его после сохранения не получится.',
            confirmText: 'Удалить практику',
            consequences: [
                {
                    text: 'Будут удалены основные сведения о практике, профессиональные компетенции и текущий состав назначенных студентов.',
                    accent: 'Практика исчезнет из активного и завершённого списков.'
                },
                {
                    text: 'Сформировать аттестационный лист по этой карточке после удаления уже не получится.',
                    accent: 'Для повторной работы практику придётся создавать заново.'
                },
                {
                    text: 'Записи о факте удаления сохранятся в журнале изменений.',
                    accent: 'Аудит не удаляется вместе с карточкой.'
                }
            ]
        });
        if (!confirmed) return;

        try {
            await fetchJson('/DepartmentStaff/DeletePractice', {
                method: 'POST',
                body: JSON.stringify(currentDetails.id)
            });

            closeModal(detailsModalBackdrop);

            const card = practicesList?.querySelector(`.practice-card[data-id="${currentDetails.id}"]`);
            card?.remove();
            currentDetails = null;
            applyPracticeFilters();
        } catch (error) {
            await showErrorModal({
                title: 'Не удалось удалить практику',
                subtitle: 'Карточка практики осталась в системе',
                message: error.message || 'Система не смогла удалить производственную практику.',
                details: [
                    {
                        text: 'Практика, назначения студентов и компетенции остались без изменений.',
                        accent: 'Журнал аудита тоже не был дополнен записью об удалении.'
                    }
                ]
            });
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

    practiceForm?.addEventListener('submit', async event => {
        event.preventDefault();
        clearFieldErrors();

        try {
            const isEditMode = Boolean(practiceIdInput?.value);
            const currentSpecialtyId = Number(practiceSpecialtySelect?.value || 0);
            const specialtyChanged = isEditMode &&
                originalEditAssignedStudentsCount > 0 &&
                originalEditSpecialtyId > 0 &&
                currentSpecialtyId > 0 &&
                currentSpecialtyId !== originalEditSpecialtyId;

            if (specialtyChanged && !specialtyChangeStudentResetConfirmed) {
                const confirmed = await showWarningModal({
                    title: 'Смена специальности у практики',
                    subtitle: 'Изменение влияет на текущие назначения студентов',
                    message: 'У этой производственной практики уже есть назначенные студенты. После смены специальности старые назначения перестанут соответствовать правилам предметной области.',
                    confirmText: 'Изменить специальность',
                    consequences: [
                        {
                            text: 'Все уже назначенные студенты и выбранные для них руководители будут автоматически удалены из состава практики.',
                            accent: 'После сохранения придётся заново сформировать список студентов для новой специальности.'
                        },
                        {
                            text: 'Основная карточка практики и перечень компетенций останутся, изменится только специальность и связанный с ней состав студентов.',
                            accent: 'Если нужна старая группа назначений, лучше не менять специальность, а создать отдельную практику.'
                        },
                        {
                            text: 'Факт автоматической очистки назначений будет зафиксирован в журнале изменений.',
                            accent: 'Это нужно для прозрачности и последующей проверки действий.'
                        }
                    ]
                });
                if (!confirmed) return;
                specialtyChangeStudentResetConfirmed = true;
            }

            const payload = buildPayload();

            await fetchJson('/DepartmentStaff/SavePractice', {
                method: 'POST',
                body: JSON.stringify(payload)
            });

            closeModal(editModalBackdrop);
            window.location.reload();
        } catch (error) {
            if ((error.validationErrors || {}).SpecialtyId) {
                specialtyChangeStudentResetConfirmed = false;
            }

            applyValidationErrors(error.validationErrors || {}, error.message || 'Не удалось сохранить производственную практику.');
        }
    });

    document.querySelectorAll('.practice-details-button').forEach(button => {
        button.addEventListener('click', () => {
            const card = button.closest('.practice-card');
            if (!card) return;
            openDetails(card.dataset.id);
        });
    });

    document.querySelectorAll('.practice-assignments-button').forEach(button => {
        button.addEventListener('click', async () => {
            const card = button.closest('.practice-card');
            if (!card) return;

            try {
                await openAssignmentsModal(card.dataset.id);
            } catch (error) {
                await showErrorModal({
                    title: 'Не удалось открыть назначение студентов',
                    subtitle: 'Окно назначения не было загружено',
                    message: error.message || 'Система не смогла открыть рабочее окно назначения студентов для выбранной практики.',
                    details: [
                        {
                            text: 'Текущая карточка практики осталась без изменений.',
                            accent: 'Можно повторить попытку после обновления страницы.'
                        }
                    ]
                });
            }
        });
    });

    document.querySelectorAll('.supervisor-details-button').forEach(button => {
        button.addEventListener('click', async () => {
            const card = button.closest('.supervisor-card');
            if (!card) return;

            try {
                await openSupervisorDetails(card.dataset.id);
            } catch (error) {
                await showErrorModal({
                    title: 'Не удалось открыть сведения о руководителе',
                    subtitle: 'Данные по нагрузке не были загружены',
                    message: error.message || 'Система не смогла открыть карточку руководителя.',
                    details: [
                        {
                            text: 'Список руководителей остался доступен, но расширенная информация не была показана.',
                            accent: 'Попробуй повторить открытие карточки ещё раз.'
                        }
                    ]
                });
            }
        });
    });

    practiceSearchInput?.addEventListener('input', applyPracticeFilters);
    dateFromFilter?.addEventListener('change', applyPracticeFilters);
    dateToFilter?.addEventListener('change', applyPracticeFilters);
    supervisorSearchInput?.addEventListener('input', applySupervisorFilters);

    buildCustomSelect('practiceSortSelect', applyPracticeFilters);
    buildCustomSelect('specialtyFilterSelect', applyPracticeFilters);
    buildCustomSelect('practiceSpecialtySelect');
    buildCustomSelect('studentSpecialtyFilterSelect');
    buildCustomSelect('studentCourseFilterSelect');
    buildCustomSelect('studentGroupFilterSelect');
    buildCustomSelect('studentSortSelect');
    buildCustomSelect('supervisorSortSelect', applySupervisorFilters);

    Promise.resolve(loadFormMetadata()).then(() => {
        applyPracticeFilters();
        applySupervisorFilters();
    });

    setPracticeStatusTab('active');
    switchLogConsole('practiceChangesConsole');
    switchPanel('practicesPanel');
});

