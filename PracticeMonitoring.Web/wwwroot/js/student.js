document.addEventListener('DOMContentLoaded', () => {
    const workspace = document.querySelector('[data-student-workspace]');
    if (workspace) {
        initStudentWorkspace(workspace);
    }
});

function initStudentWorkspace(workspace) {
    const urls = {
        getPractice: workspace.dataset.getPracticeUrl || '',
        saveOrganization: workspace.dataset.saveOrganizationUrl || '',
        saveDiary: workspace.dataset.saveDiaryUrl || '',
        saveReportItems: workspace.dataset.saveReportItemsUrl || '',
        saveSources: workspace.dataset.saveSourcesUrl || '',
        uploadAppendix: workspace.dataset.uploadAppendixUrl || '',
        deleteAppendix: workspace.dataset.deleteAppendixUrl || '',
        downloadAppendix: workspace.dataset.downloadAppendixUrl || '',
        downloadDiaryAttachment: workspace.dataset.downloadDiaryAttachmentUrl || ''
    };

    const $ = selector => workspace.querySelector(selector);
    const $$ = selector => Array.from(workspace.querySelectorAll(selector));
    const state = {
        practices: Array.isArray(window.initialStudentPractices) ? window.initialStudentPractices : [],
        detailsByAssignment: new Map(),
        activeStatus: 'active',
        currentDetails: null,
        selectedDate: '',
        organizationConfirmArmed: false,
        reportDocument: createEmptyReportDocument(),
        reportSelectedBlockId: null,
        reportTableSelection: null,
        reportDirty: false,
        reportSaving: false,
        reportAutosaveTimer: null,
        reportEditorOpen: false
    };

    const reportCategories = [
        { key: 'TechnicalTool', title: 'Технические средства', namePlaceholder: 'Ноутбук, сервер, маршрутизатор', descriptionPlaceholder: 'Характеристики или назначение' },
        { key: 'SoftwareTool', title: 'Программные средства', namePlaceholder: 'Visual Studio, PostgreSQL, Figma', descriptionPlaceholder: 'Версия, назначение, где применялось' },
        { key: 'PeripheralDevice', title: 'Периферийные устройства', namePlaceholder: 'Принтер, сканер, веб-камера', descriptionPlaceholder: 'Модель или задача использования' }
    ];

    bindWorkspaceEvents();
    renderPractices();
    openPracticeFromQuery();

    function bindWorkspaceEvents() {
        $$('[data-student-panel-target]').forEach(button => {
            button.addEventListener('click', () => activatePanel(button.dataset.studentPanelTarget));
        });

        $$('[data-practice-status]').forEach(button => {
            button.addEventListener('click', () => {
                state.activeStatus = button.dataset.practiceStatus || 'active';
                $$('[data-practice-status]').forEach(item => item.classList.toggle('active', item === button));
                renderPractices();
            });
        });

        $('#studentPracticeSearch')?.addEventListener('input', renderPractices);
        $('#studentPracticeSort')?.addEventListener('change', renderPractices);
        $('#studentPracticesList')?.addEventListener('click', event => {
            const button = getClosest(event, '[data-open-practice]');
            if (button) {
                openPractice(Number(button.dataset.openPractice || '0'));
            }
        });

        $('#closeStudentPracticeModal')?.addEventListener('click', closePracticeModal);
        $('#studentPracticeModal')?.addEventListener('click', event => {
            if (event.target.id === 'studentPracticeModal') {
                closePracticeModal();
            }
        });

        $$('[data-student-modal-tab]').forEach(button => {
            button.addEventListener('click', () => activateModalTab(button.dataset.studentModalTab));
        });

        $('#editOrganizationButton')?.addEventListener('click', () => setOrganizationEditMode(true));
        $('#cancelOrganizationEditButton')?.addEventListener('click', () => {
            state.organizationConfirmArmed = false;
            fillOrganizationForm(state.currentDetails);
            setOrganizationEditMode(false);
            hideStatus();
        });

        $('#organizationSupervisorPhone')?.addEventListener('focus', event => {
            if (!event.target.value.trim()) {
                event.target.value = '+7 ';
            }
        });
        $('#organizationSupervisorPhone')?.addEventListener('input', event => {
            event.target.value = formatRuPhone(event.target.value);
            clearFieldError('OrganizationSupervisorPhone');
        });
        $('#organizationSupervisorEmail')?.addEventListener('input', () => clearFieldError('OrganizationSupervisorEmail'));

        $('#studentOrganizationForm')?.addEventListener('submit', saveOrganization);
        $('#studentDiaryForm')?.addEventListener('submit', saveDiaryEntry);
        $('#saveReportItemsButton')?.addEventListener('click', saveReportItems);
        $('#addSourceButton')?.addEventListener('click', () => addSourceRow());
        $('#saveSourcesButton')?.addEventListener('click', saveSources);
        $('#studentAppendixForm')?.addEventListener('submit', uploadAppendix);
        $('#appendixFile')?.addEventListener('change', updateAppendixFileName);

        $('#studentDiaryCalendar')?.addEventListener('click', event => {
            const button = getClosest(event, '[data-calendar-date]');
            if (button && !button.disabled) {
                selectDiaryDate(button.dataset.calendarDate);
            }
        });

        $('#openDayReportEditorButton')?.addEventListener('click', openReportEditor);
        $('#closeDayReportEditorButton')?.addEventListener('click', requestCloseReportEditor);
        $('#saveDayReportEditorButton')?.addEventListener('click', () => saveCurrentDiaryFromReportEditor(false));
        $('#cancelReportCloseButton')?.addEventListener('click', hideReportCloseGuard);
        $('#discardReportChangesButton')?.addEventListener('click', () => closeReportEditor(true));
        $('#studentReportImageInput')?.addEventListener('change', handleReportImageSelected);

        $('#studentReportEditorModal')?.addEventListener('click', event => {
            if (event.target.id === 'studentReportEditorModal') {
                requestCloseReportEditor();
            }
        });
        $('#studentReportToolbar')?.addEventListener('click', handleReportToolbarClick);
        $('#studentReportDocumentCanvas')?.addEventListener('click', handleReportCanvasClick);
        $('#studentReportDocumentCanvas')?.addEventListener('contextmenu', handleReportCanvasContextMenu);
        $('#studentReportDocumentCanvas')?.addEventListener('input', handleReportCanvasInput);
        $('#studentReportDocumentCanvas')?.addEventListener('change', handleReportCanvasChange);
        $('#studentReportOutline')?.addEventListener('click', handleReportOutlineClick);
        $('#studentReportProperties')?.addEventListener('click', handleReportPropertiesClick);
        $('#studentReportValidationSummary')?.addEventListener('click', handleReportProblemClick);
        $('#studentTableContextMenu')?.addEventListener('click', handleReportTableMenuClick);

        workspace.addEventListener('click', handleWorkspaceClick);
        workspace.addEventListener('input', handleWorkspaceInput);
        workspace.addEventListener('change', handleWorkspaceChange);
        workspace.addEventListener('focusin', event => {
            const block = getClosest(event, '[data-report-block-id]');
            if (block) {
                selectReportBlock(block.dataset.reportBlockId);
            }
        });
    }

    function handleReportToolbarClick(event) {
        const target = event.target instanceof Element ? event.target : null;
        if (!target) {
            return;
        }

        const insertButton = target.closest('[data-report-insert]');
        if (insertButton) {
            event.preventDefault();
            event.stopPropagation();
            insertReportBlock(insertButton.dataset.reportInsert || 'text');
        }
    }

    function handleReportCanvasClick(event) {
        const target = event.target instanceof Element ? event.target : null;
        if (!target) {
            return;
        }

        const insertButton = target.closest('[data-report-insert-empty]');
        if (insertButton) {
            event.preventDefault();
            event.stopPropagation();
            insertReportBlock(insertButton.dataset.reportInsertEmpty || 'text');
            return;
        }

        const tableCell = target.closest('[data-table-cell]');
        if (tableCell) {
            event.stopPropagation();
            selectReportTableCell(tableCell.dataset.blockId, tableCell.dataset.rowId, tableCell.dataset.cellId, event.shiftKey);
        } else if (!target.closest('#studentTableContextMenu')) {
            hideReportTableContextMenu();
        }

        const selectBlock = target.closest('[data-report-block-id]');
        if (selectBlock) {
            selectReportBlock(selectBlock.dataset.reportBlockId);
        }

        const moveButton = target.closest('[data-report-move]');
        if (moveButton) {
            event.preventDefault();
            event.stopPropagation();
            moveReportBlock(moveButton.dataset.reportBlockId, moveButton.dataset.reportMove);
            return;
        }

        const deleteButton = target.closest('[data-report-delete]');
        if (deleteButton) {
            event.preventDefault();
            event.stopPropagation();
            deleteReportBlock(deleteButton.dataset.reportDelete);
            return;
        }

        const addAfterButton = target.closest('[data-report-add-after]');
        if (addAfterButton) {
            event.preventDefault();
            event.stopPropagation();
            state.reportSelectedBlockId = addAfterButton.dataset.reportAddAfter;
            insertReportBlock(addAfterButton.dataset.reportType || 'text');
            return;
        }

        const imageButton = target.closest('[data-report-pick-image]');
        if (imageButton) {
            event.preventDefault();
            event.stopPropagation();
            selectReportBlock(imageButton.dataset.reportPickImage);
            $('#studentReportImageInput').click();
            return;
        }

        const tableAction = target.closest('[data-table-action]');
        if (tableAction) {
            event.preventDefault();
            event.stopPropagation();
            mutateReportTable(tableAction.dataset.blockId, tableAction.dataset.tableAction, tableAction.dataset.rowId, tableAction.dataset.cellId);
        }
    }

    function handleReportCanvasContextMenu(event) {
        const cell = event.target instanceof Element ? event.target.closest('[data-table-cell]') : null;
        if (!cell) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        selectReportTableCell(cell.dataset.blockId, cell.dataset.rowId, cell.dataset.cellId, event.shiftKey);
        showReportTableContextMenu(cell.dataset.blockId, cell.dataset.rowId, cell.dataset.cellId, event.clientX, event.clientY);
    }

    function handleReportCanvasInput(event) {
        event.stopPropagation();
        updateReportBlockFromInput(event.target);
    }

    function handleReportCanvasChange(event) {
        event.stopPropagation();
        updateReportBlockFromChange(event.target);
    }

    function handleReportOutlineClick(event) {
        const outlineItem = event.target instanceof Element ? event.target.closest('[data-outline-block]') : null;
        if (!outlineItem) {
            return;
        }
        selectReportBlock(outlineItem.dataset.outlineBlock);
        document.querySelector(`[data-report-block-id="${outlineItem.dataset.outlineBlock}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function handleReportPropertiesClick(event) {
        const addAfterButton = event.target instanceof Element ? event.target.closest('[data-report-add-after]') : null;
        if (!addAfterButton) {
            return;
        }
        state.reportSelectedBlockId = addAfterButton.dataset.reportAddAfter;
        insertReportBlock(addAfterButton.dataset.reportType || 'text');
    }

    function handleWorkspaceClick(event) {
        const target = event.target instanceof Element ? event.target : null;
        if (!target) {
            return;
        }

        if (target.closest('#studentReportEditorModal')) {
            return;
        }

        const addReportRowButton = target.closest('[data-add-report-row]');
        if (addReportRowButton) {
            addReportRow(addReportRowButton.dataset.addReportRow || 'TechnicalTool');
            return;
        }

        const removeReportRowButton = target.closest('[data-remove-report-row]');
        if (removeReportRowButton) {
            removeReportRowButton.closest('[data-report-row]')?.remove();
            return;
        }

        const removeSourceRowButton = target.closest('[data-remove-source-row]');
        if (removeSourceRowButton) {
            removeSourceRowButton.closest('[data-source-row]')?.remove();
            return;
        }

        const outlineItem = target.closest('[data-outline-block]');
        if (outlineItem) {
            selectReportBlock(outlineItem.dataset.outlineBlock);
            document.querySelector(`[data-report-block-id="${outlineItem.dataset.outlineBlock}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return;
        }

        const deleteAppendixButton = target.closest('[data-delete-appendix]');
        if (deleteAppendixButton) {
            deleteAppendix(Number(deleteAppendixButton.dataset.deleteAppendix || '0'));
        }
    }

    function handleWorkspaceInput(event) {
        if (event.target instanceof Element && event.target.closest('#studentReportEditorModal')) {
            return;
        }
        updateReportBlockFromInput(event.target);
    }

    function updateReportBlockFromInput(rawTarget) {
        const target = rawTarget instanceof Element ? rawTarget : null;
        if (!target) {
            return;
        }

        const textBlock = target.closest('[data-report-text]');
        if (textBlock) {
            const block = getReportBlock(textBlock.dataset.blockId);
            if (block?.type === 'text') {
                block.content = textBlock.value;
                markReportDirty();
                renderReportSummary();
                renderReportValidationSummary();
            }
            return;
        }

        const editable = target.closest('[data-report-editable]');
        if (editable) {
            const block = getReportBlock(editable.dataset.blockId);
            if (block) {
                block.html = editable.innerHTML.trim();
                markReportDirty();
                renderReportSummary();
            }
            return;
        }

        const tableCell = target.closest('[data-table-cell]');
        if (tableCell) {
            const block = getReportBlock(tableCell.dataset.blockId);
            const cell = getReportTableCell(block, tableCell.dataset.rowId, tableCell.dataset.cellId);
            if (cell) {
                cell.text = tableCell.value;
                markReportDirty();
                renderReportSummary();
                renderReportValidationSummary();
            }
            return;
        }

        const caption = target.closest('[data-report-caption]');
        if (caption) {
            const block = getReportBlock(caption.dataset.blockId);
            if (block) {
                block.title = caption.value;
                block.caption = caption.value;
                markReportDirty();
                renderReportSummary();
                renderReportValidationSummary();
            }
            return;
        }

        const alt = target.closest('[data-report-alt]');
        if (alt) {
            const block = getReportBlock(alt.dataset.blockId);
            if (block) {
                block.alt = alt.value;
                markReportDirty();
            }
        }
    }

    function handleWorkspaceChange(event) {
        if (event.target instanceof Element && event.target.closest('#studentReportEditorModal')) {
            return;
        }
        updateReportBlockFromChange(event.target);
    }

    function updateReportBlockFromChange(rawTarget) {
        const target = rawTarget instanceof Element ? rawTarget : null;
        if (!target) {
            return;
        }

        const textMode = target.closest('[data-report-text-mode]');
        if (textMode) {
            const block = getReportBlock(textMode.dataset.blockId);
            if (block?.type === 'text') {
                block.mode = textMode.value || 'paragraph';
                markReportDirty();
                renderReportSummary();
                renderReportEditor();
            }
            return;
        }

        const hasHeader = target.closest('[data-table-header-toggle]');
        if (hasHeader) {
            const block = getReportBlock(hasHeader.dataset.blockId);
            if (block?.type === 'table') {
                block.hasHeaderRow = hasHeader.checked;
                block.hasHeader = hasHeader.checked;
                markReportDirty();
                renderReportEditor();
            }
        }
    }

    function activatePanel(targetId) {
        $$('[data-student-panel-target]').forEach(button => button.classList.toggle('active', button.dataset.studentPanelTarget === targetId));
        $$('[data-student-panel]').forEach(panel => panel.classList.toggle('active', panel.id === targetId));
    }

    function renderPractices() {
        const list = $('#studentPracticesList');
        if (!list) {
            return;
        }

        const query = ($('#studentPracticeSearch')?.value || '').trim().toLowerCase();
        const sort = $('#studentPracticeSort')?.value || 'date-asc';
        let items = state.practices.filter(practice => {
            const status = practice.isCompleted ? 'completed' : 'active';
            return status === state.activeStatus && (!query || buildPracticeSearchText(practice).includes(query));
        });

        items = sortPractices(items, sort);
        $('#studentPracticesSummary').textContent = `Показано практик: ${items.length}`;

        if (!items.length) {
            list.innerHTML = '<div class="student-empty-state">По выбранным условиям практики не найдены.</div>';
            return;
        }

        list.innerHTML = items.map(practice => `
            <article class="student-practice-row" data-assignment-id="${practice.assignmentId}">
                <div class="student-practice-title-cell">
                    <div class="student-practice-chip-row">
                        <span class="student-practice-index">${escapeHtml(practice.practiceIndex)}</span>
                        <span class="student-practice-state ${practice.isCompleted ? 'completed' : 'active'}">${practice.isCompleted ? 'Завершена' : 'Активна'}</span>
                        ${practice.isDetailsOverdue ? '<span class="student-practice-state warning">Просрочены сведения</span>' : ''}
                    </div>
                    <strong>${escapeHtml(practice.name)}</strong>
                    <small>${escapeHtml(`${practice.professionalModuleCode || ''} ${practice.professionalModuleName || ''}`.trim())}</small>
                </div>
                <div class="student-practice-date-cell">
                    <strong>${formatDate(practice.startDate)} - ${formatDate(practice.endDate)}</strong>
                    <small>${practice.hours || 0} ч. · до ${formatDate(practice.detailsDueDate)} заполнить организацию</small>
                </div>
                <div class="student-practice-supervisor-cell">
                    <strong>${escapeHtml(practice.supervisorFullName || 'Не назначен')}</strong>
                    <small>${escapeHtml(practice.organizationName || 'Организация не указана')}</small>
                </div>
                <div class="student-practice-progress-cell">
                    <strong>${practice.hasRequiredDetails ? 'Сведения заполнены' : 'Нужно заполнить'}</strong>
                    <small>Дневник: ${practice.diaryEntriesCount || 0}/${practice.workDaysCount || 0} дней</small>
                </div>
                <div class="student-practice-action-cell">
                    <button type="button" class="student-primary-button student-open-practice-button" data-open-practice="${practice.assignmentId}">Открыть</button>
                </div>
            </article>`).join('');
    }

    async function openPractice(assignmentId) {
        const details = await loadPracticeDetails(assignmentId, false, false);
        if (!details) {
            return;
        }

        state.currentDetails = details;
        state.selectedDate = '';
        renderPracticeModal(details);
        activateModalTab('overview');
        $('#studentPracticeModal').hidden = false;
        document.body.style.overflow = 'hidden';
    }

    async function loadPracticeDetails(assignmentId, rerenderModal, forceReload) {
        if (!assignmentId) {
            return null;
        }

        if (forceReload || !state.detailsByAssignment.has(assignmentId)) {
            const response = await fetch(`${urls.getPractice}?assignmentId=${encodeURIComponent(assignmentId)}`, { cache: 'no-store' });
            if (!response.ok) {
                showStatus('Не удалось загрузить практику.', true);
                return null;
            }
            state.detailsByAssignment.set(assignmentId, await response.json());
        }

        const details = state.detailsByAssignment.get(assignmentId);
        state.currentDetails = details;
        if (rerenderModal) {
            renderPracticeModal(details);
        }
        return details;
    }

    function renderPracticeModal(details) {
        $('#studentPracticeModalTitle').textContent = `${details.practiceIndex} ${details.name}`;
        $('#studentPracticeModalSubtitle').textContent = `${formatDate(details.startDate)} - ${formatDate(details.endDate)} · ${details.hours} ч. · ${details.supervisorFullName || 'руководитель не назначен'}`;
        $('#studentPracticeModalEyebrow').textContent = details.isCompleted ? 'Завершённая практика' : 'Активная практика';
        renderOverview(details);
        renderOrganization(details);
        renderDiary(details);
        renderReportTables(details.reportItems || []);
        renderSources(details.sources || []);
        renderAppendices(details.appendices || []);
    }

    function renderOverview(details) {
        const rows = [
            ['Специальность', `${details.specialtyCode || ''} ${details.specialtyName || ''}`.trim()],
            ['Профессиональный модуль', `${details.professionalModuleCode || ''} ${details.professionalModuleName || ''}`.trim()],
            ['Сроки', `${formatDate(details.startDate)} - ${formatDate(details.endDate)}`],
            ['Руководитель', details.supervisorFullName || 'Не назначен'],
            ['Организация', details.organizationName || 'Не указана'],
            ['Заполнить сведения до', formatDate(details.detailsDueDate)],
            ['Дневник', `${details.diaryEntriesCount || 0}/${details.workDaysCount || 0} рабочих дней`],
            ['Статус', details.hasRequiredDetails ? 'Основные сведения заполнены' : 'Ожидает заполнения']
        ];
        $('#studentPracticeOverview').innerHTML = rows.map(([label, value]) => `
            <div class="student-overview-item">
                <span>${escapeHtml(label)}</span>
                <strong>${escapeHtml(value || 'Не указано')}</strong>
            </div>`).join('');

        $('#studentPracticeCompetencies').innerHTML = (details.competencies || []).length
            ? details.competencies.map(item => `
                <article class="student-compact-card">
                    <strong>${escapeHtml(item.competencyCode)} · ${escapeHtml(item.competencyDescription)}</strong>
                    <p>${escapeHtml(item.workTypes)}</p>
                    <small>${item.hours || 0} ч.</small>
                </article>`).join('')
            : '<div class="student-empty-state">Компетенции не указаны.</div>';
    }

    function renderOrganization(details) {
        fillOrganizationForm(details);
        $('#studentOrganizationReadonly').innerHTML = [
            ['Организация', details.organizationName],
            ['Руководитель от организации', details.organizationSupervisorFullName],
            ['Должность', details.organizationSupervisorPosition],
            ['Телефон', details.organizationSupervisorPhone],
            ['Почта', details.organizationSupervisorEmail],
            ['Содержание задания', details.practiceTaskContent]
        ].map(([label, value]) => `
            <div class="student-readonly-item ${label === 'Содержание задания' ? 'wide' : ''}">
                <span>${escapeHtml(label)}</span>
                <strong>${escapeHtml(value || 'Не указано')}</strong>
            </div>`).join('');
        setOrganizationEditMode(!details.hasRequiredDetails);
    }

    function setOrganizationEditMode(isEdit) {
        $('#studentOrganizationForm').hidden = !isEdit;
        $('#studentOrganizationView').hidden = isEdit;
        if (isEdit) {
            clearStudentFieldErrors();
        }
    }

    function fillOrganizationForm(details) {
        const form = $('#studentOrganizationForm');
        if (!form || !details) {
            return;
        }

        form.organizationName.value = details.organizationName || '';
        form.organizationSupervisorFullName.value = details.organizationSupervisorFullName || '';
        form.organizationSupervisorPosition.value = details.organizationSupervisorPosition || '';
        form.organizationSupervisorPhone.value = details.organizationSupervisorPhone || '+7 ';
        form.organizationSupervisorEmail.value = details.organizationSupervisorEmail || '';
        form.practiceTaskContent.value = details.practiceTaskContent || '';
    }

    async function saveOrganization(event) {
        event.preventDefault();
        clearStudentFieldErrors();
        const payload = readFormValues(event.currentTarget);
        const clientErrors = validateOrganizationClient(payload);
        if (Object.keys(clientErrors).length) {
            renderFieldErrors(clientErrors);
            showStatus('Проверьте поля организации.', true);
            return;
        }

        if (state.currentDetails.hasRequiredDetails && !state.organizationConfirmArmed) {
            state.organizationConfirmArmed = true;
            showStatus('После сохранения руководитель практики получит уведомление об изменении сведений. Эти данные используются в отчётных документах и при проверке практики. Нажмите “Сохранить сведения” ещё раз, чтобы подтвердить изменение.', true);
            return;
        }

        const result = await postJson(withAssignment(urls.saveOrganization, state.currentDetails.assignmentId), payload);
        if (!result.ok) {
            renderFieldErrors(result.errors || {});
            showStatus(result.message || 'Не удалось сохранить сведения.', true);
            return;
        }

        state.organizationConfirmArmed = false;
        applyUpdatedDetails(result.data);
        setOrganizationEditMode(false);
        showStatus('Сведения сохранены. Руководитель практики получит уведомление.', false);
    }

    function renderDiary(details) {
        renderDiaryCalendar(details);
        selectDiaryDate(state.selectedDate || getDefaultDiaryDate(details), false);
    }

    function renderDiaryCalendar(details) {
        const entries = new Map((details.diaryEntries || []).map(entry => [toDateInputValue(entry.workDate), entry]));
        const days = getPracticeDays(details.startDate, details.endDate);
        $('#diaryProgressLabel').textContent = `${details.diaryEntriesCount || 0}/${details.workDaysCount || 0} заполнено`;
        $('#studentDiaryCalendar').innerHTML = days.map(day => {
            const date = toDateInputValue(day);
            const weekend = day.getDay() === 0 || day.getDay() === 6;
            const filled = entries.has(date);
            return `
                <button type="button"
                        class="student-calendar-day ${weekend ? 'weekend' : ''} ${filled ? 'filled' : 'empty'} ${state.selectedDate === date ? 'active' : ''}"
                        data-calendar-date="${date}"
                        ${weekend ? 'disabled' : ''}>
                    <span>${day.getDate()}</span>
                    <small>${formatWeekday(day)}</small>
                    <b>${weekend ? 'выходной' : filled ? 'готово' : 'пусто'}</b>
                </button>`;
        }).join('');
    }

    function selectDiaryDate(date, scrollIntoView = true) {
        if (!date || !state.currentDetails) {
            return;
        }

        state.selectedDate = date;
        $('#diaryWorkDate').value = date;
        $('#diaryEditorTitle').textContent = `Рабочий день — ${formatDate(date)}`;
        const entry = getSelectedDiaryEntry();
        $('#diaryShortDescription').value = entry?.shortDescription || '';
        state.reportDocument = parseReportDocument(entry?.detailedReport, entry?.attachments || []);
        state.reportSelectedBlockId = state.reportDocument.content[0]?.id || null;
        state.reportDirty = false;
        hideReportCloseGuard();
        setReportSaveState('saved', entry?.updatedAtUtc ? `Сохранено ${formatDateTime(entry.updatedAtUtc)}` : 'Сохранено');
        renderDiaryCalendar(state.currentDetails);
        renderReportSummary();
        if (state.reportEditorOpen) {
            renderReportEditor();
        }
        if (scrollIntoView) {
            $('#studentDiaryForm')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }

    function renderReportSummary() {
        const stats = getReportStats(state.reportDocument);
        $('#studentReportPreviewStats').textContent = `${stats.text} текстовых блоков · ${stats.tables} таблиц · ${stats.figures} рисунков`;
        const entry = getSelectedDiaryEntry();
        $('#studentReportPreviewUpdated').textContent = entry?.updatedAtUtc ? `Обновлено ${formatDateTime(entry.updatedAtUtc)}` : 'Не сохранялся';
        $('#studentReportPreviewExcerpt').textContent = buildReportExcerpt(state.reportDocument);
        $('#diaryDetailedReport').value = JSON.stringify(prepareReportDocumentForSave(state.reportDocument).document);
    }

    function openReportEditor() {
        if (!state.currentDetails || !state.selectedDate) {
            return;
        }
        state.reportEditorOpen = true;
        $('#studentReportEditorTitle').textContent = `Рабочий день — ${formatDate(state.selectedDate)}`;
        $('#studentReportEditorModal').hidden = false;
        document.body.style.overflow = 'hidden';
        hideReportCloseGuard();
        renderReportEditor();
    }

    function requestCloseReportEditor() {
        if (!state.reportDirty) {
            closeReportEditor(false);
            return;
        }
        $('#studentReportCloseGuard').hidden = false;
    }

    function closeReportEditor(discardChanges) {
        if (discardChanges) {
            const entry = getSelectedDiaryEntry();
            state.reportDocument = parseReportDocument(entry?.detailedReport, entry?.attachments || []);
            state.reportDirty = false;
            renderReportSummary();
        }
        state.reportEditorOpen = false;
        $('#studentReportEditorModal').hidden = true;
        $('#studentReportImageInput').value = '';
        hideReportCloseGuard();
        document.body.style.overflow = $('#studentPracticeModal')?.hidden ? '' : 'hidden';
    }

    function hideReportCloseGuard() {
        const guard = $('#studentReportCloseGuard');
        if (guard) {
            guard.hidden = true;
        }
    }

    function renderReportEditor() {
        const canvas = $('#studentReportDocumentCanvas');
        if (!canvas) {
            return;
        }

        ensureReportDocumentShape(state.reportDocument);
        hideReportTableContextMenu();

        if (!state.reportDocument.content.length) {
            canvas.innerHTML = `
                <div class="student-report-empty-state">
                    <strong>Отчёт пока пустой</strong>
                    <span>Добавьте первый смысловой блок. Оформление будет применено автоматически при генерации DOCX.</span>
                    <div class="student-report-empty-actions">
                        <button type="button" data-report-insert-empty="text">Добавить текст</button>
                        <button type="button" data-report-insert-empty="table">Добавить таблицу</button>
                        <button type="button" data-report-insert-empty="image">Добавить рисунок</button>
                    </div>
                </div>`;
        } else {
            canvas.innerHTML = state.reportDocument.content.map((block, index) => renderReportBlock(block, index)).join('');
        }

        renderReportOutline();
        renderReportProperties();
        renderReportValidationSummary();
    }

    function renderReportBlock(block, index) {
        const selected = block.id === state.reportSelectedBlockId ? 'selected' : '';
        const controls = `
            <div class="student-report-block-controls" aria-label="Управление блоком">
                <button type="button" title="Переместить выше" data-report-move="up" data-report-block-id="${block.id}" ${index === 0 ? 'disabled' : ''}>↑</button>
                <button type="button" title="Переместить ниже" data-report-move="down" data-report-block-id="${block.id}" ${index === state.reportDocument.content.length - 1 ? 'disabled' : ''}>↓</button>
                <button type="button" title="Удалить блок" data-report-delete="${block.id}">×</button>
            </div>`;

        const blockHtml = block.type === 'table'
            ? renderReportTableBlock(block, controls, selected)
            : block.type === 'image'
                ? renderReportImageBlock(block, controls, selected)
                : renderReportTextBlock(block, controls, selected);

        return `${blockHtml}${renderReportBlockInsertionRow(block.id)}`;
    }

    function renderReportBlockInsertionRow(blockId) {
        return `
            <div class="student-report-insert-row" aria-label="Добавить блок после текущего">
                <span></span>
                <button type="button" data-report-add-after="${blockId}" data-report-type="text">+ Текст</button>
                <button type="button" data-report-add-after="${blockId}" data-report-type="table">+ Таблица</button>
                <button type="button" data-report-add-after="${blockId}" data-report-type="image">+ Рисунок</button>
                <span></span>
            </div>`;
    }

    function renderReportTextBlock(block, controls, selected) {
        const mode = block.mode || 'paragraph';
        return `
            <section class="student-report-block student-report-text-block ${selected}" data-report-block-id="${block.id}">
                ${controls}
                <div class="student-report-block-label">Текстовый блок</div>
                <select class="student-report-mode-select" data-report-text-mode data-block-id="${block.id}" aria-label="Тип текстового блока">
                    <option value="paragraph" ${mode === 'paragraph' ? 'selected' : ''}>Обычный текст</option>
                    <option value="bullet_list" ${mode === 'bullet_list' ? 'selected' : ''}>Маркированный список</option>
                    <option value="numbered_list" ${mode === 'numbered_list' ? 'selected' : ''}>Нумерованный список</option>
                </select>
                <textarea class="student-report-textarea"
                          data-report-text
                          data-block-id="${block.id}"
                          rows="7"
                          placeholder="Опишите выполненную работу, решения, проблемы и результат дня...">${escapeHtml(block.content || '')}</textarea>
                <small>Шрифт, интервалы и отступы будут применены автоматически по шаблону DOCX.</small>
            </section>`;
    }

    function renderReportTableBlock(block, controls, selected) {
        block.rows = normalizeReportTableRows(block.rows);
        const selectedCellIds = getSelectedTableCellIds(block.id);
        const tableNumber = getBlockNumber('table', block.id);
        return `
            <section class="student-report-block student-report-table-block-editor ${selected}" data-report-block-id="${block.id}">
                ${controls}
                <div class="student-report-block-label">Таблица ${tableNumber}</div>
                <input class="student-report-caption-input" data-report-caption data-block-id="${block.id}" value="${escapeHtmlAttribute(block.title || block.caption || '')}" placeholder="Название таблицы без префикса “Таблица ${tableNumber} —”" />
                ${renderReportTableContextToolbar(block)}
                <table class="student-report-structured-table">
                    <tbody>
                        ${block.rows.map((row, rowIndex) => `
                            <tr data-row-id="${row.id}">
                                ${(row.cells || []).filter(cell => !cell.hidden).map(cell => {
                                    const tag = (block.hasHeaderRow || block.hasHeader) && rowIndex === 0 ? 'th' : 'td';
                                    const isSelected = selectedCellIds.includes(cell.id) ? 'selected' : '';
                                    return `<${tag} class="${isSelected}" colspan="${cell.colspan || 1}" rowspan="${cell.rowspan || 1}">
                                        <textarea data-table-cell data-block-id="${block.id}" data-row-id="${row.id}" data-cell-id="${cell.id}" rows="2" placeholder="Текст ячейки">${escapeHtml(cell.text || '')}</textarea>
                                    </${tag}>`;
                                }).join('')}
                            </tr>`).join('')}
                    </tbody>
                </table>
                <label class="student-report-table-header-toggle"><input type="checkbox" data-table-header-toggle data-block-id="${block.id}" ${(block.hasHeaderRow || block.hasHeader) ? 'checked' : ''}> первая строка является заголовком</label>
            </section>`;
    }

    function renderReportTableContextToolbar(block) {
        const canMerge = canMergeSelectedTableCells(block);
        const canSplit = canSplitSelectedTableCell(block);
        return `
            <div class="student-table-context-panel">
                <button type="button" data-table-action="add-row-above" data-block-id="${block.id}">Строка выше</button>
                <button type="button" data-table-action="add-row-below" data-block-id="${block.id}">Строка ниже</button>
                <button type="button" data-table-action="add-column-left" data-block-id="${block.id}">Столбец слева</button>
                <button type="button" data-table-action="add-column-right" data-block-id="${block.id}">Столбец справа</button>
                <button type="button" data-table-action="delete-row" data-block-id="${block.id}">Удалить строку</button>
                <button type="button" data-table-action="delete-column" data-block-id="${block.id}">Удалить столбец</button>
                <button type="button" data-table-action="merge-cells" data-block-id="${block.id}" ${canMerge ? '' : 'disabled'}>Объединить</button>
                <button type="button" data-table-action="split-cell" data-block-id="${block.id}" ${canSplit ? '' : 'disabled'}>Разделить</button>
            </div>`;
    }

    function renderReportImageBlock(block, controls, selected) {
        const imageSrc = block.previewUrl || block.imageUrl || (block.attachmentId ? `${urls.downloadDiaryAttachment}?attachmentId=${encodeURIComponent(block.attachmentId)}` : '');
        const imageNumber = getBlockNumber('image', block.id);
        return `
            <section class="student-report-block student-report-figure-block ${selected}" data-report-block-id="${block.id}">
                ${controls}
                <div class="student-report-block-label">Рисунок ${imageNumber}</div>
                <div class="student-report-figure-preview ${imageSrc ? '' : 'empty'}">
                    ${imageSrc
                        ? `<img src="${escapeHtmlAttribute(imageSrc)}" alt="${escapeHtmlAttribute(block.alt || block.title || 'Рисунок')}" />`
                        : '<span>Изображение не выбрано</span>'}
                </div>
                <input class="student-report-caption-input" data-report-caption data-block-id="${block.id}" value="${escapeHtmlAttribute(block.title || block.caption || '')}" placeholder="Название рисунка без префикса “Рисунок ${imageNumber} —”" />
                <input class="student-report-alt-input" data-report-alt data-block-id="${block.id}" value="${escapeHtmlAttribute(block.alt || '')}" placeholder="Описание изображения для будущего DOCX" />
                <button type="button" class="student-mini-button" data-report-pick-image="${block.id}">${imageSrc ? 'Заменить изображение' : 'Выбрать изображение'}</button>
            </section>`;
    }

    function renderReportOutline() {
        const outline = $('#studentReportOutline');
        if (!outline) {
            return;
        }
        const problemsByBlock = groupReportProblemsByBlock(collectReportProblems(state.reportDocument));
        if (!state.reportDocument.content.length) {
            outline.innerHTML = '<div class="student-report-outline-empty">Структура появится после добавления блоков.</div>';
            return;
        }
        outline.innerHTML = state.reportDocument.content.map((block, index) => {
            const problemCount = problemsByBlock.get(block.id)?.length || 0;
            return `
                <button type="button" class="${block.id === state.reportSelectedBlockId ? 'active' : ''} ${problemCount ? 'has-problems' : ''}" data-outline-block="${block.id}">
                    <span>${index + 1}. ${getReportBlockTitle(block)}</span>
                    <small>${getReportBlockKind(block)} ${problemCount ? `<b>${problemCount}</b>` : ''}</small>
                </button>`;
        }).join('');
    }

    function renderReportProperties() {
        const panel = $('#studentReportProperties');
        const block = getReportBlock(state.reportSelectedBlockId);
        if (!panel) {
            return;
        }
        if (!block) {
            panel.innerHTML = '<p>Выберите блок на листе.</p>';
            return;
        }

        const position = state.reportDocument.content.findIndex(item => item.id === block.id) + 1;
        const tableHint = block.type === 'table'
            ? '<p>Выделите ячейку. Shift+клик выбирает несколько соседних ячеек для объединения.</p>'
            : '';
        const imageHint = block.type === 'image'
            ? '<p>Размер и выравнивание рисунка задаст DOCX-шаблон. Здесь нужна только подпись и изображение.</p>'
            : '';
        const textHint = block.type === 'text'
            ? '<p>Допустим только текст и режим списка. Ручное оформление не используется.</p>'
            : '';

        panel.innerHTML = `
            <div class="student-report-property-card">
                <span>Тип блока</span>
                <strong>${getReportBlockKind(block)}</strong>
            </div>
            <div class="student-report-property-card">
                <span>Позиция</span>
                <strong>${position} из ${state.reportDocument.content.length}</strong>
            </div>
            <div class="student-report-property-card muted">
                ${tableHint || imageHint || textHint}
            </div>
            <div class="student-report-property-actions">
                <button type="button" class="student-mini-button" data-report-add-after="${block.id}" data-report-type="text">Добавить текст после</button>
                <button type="button" class="student-mini-button" data-report-add-after="${block.id}" data-report-type="table">Добавить таблицу после</button>
                <button type="button" class="student-mini-button" data-report-add-after="${block.id}" data-report-type="image">Добавить рисунок после</button>
            </div>`;
    }

    function insertReportBlock(type) {
        const normalizedType = type === 'figure' ? 'image' : type;
        const block = createReportBlock(normalizedType);
        const selectedIndex = state.reportDocument.content.findIndex(item => item.id === state.reportSelectedBlockId);
        const insertAt = selectedIndex >= 0 ? selectedIndex + 1 : state.reportDocument.content.length;
        state.reportDocument.content.splice(insertAt, 0, block);
        state.reportSelectedBlockId = block.id;
        state.reportTableSelection = null;
        markReportDirty();
        renderReportEditor();
        renderReportSummary();
        setTimeout(() => {
            const element = document.querySelector(`[data-report-block-id="${block.id}"] textarea, [data-report-block-id="${block.id}"] input`);
            element?.focus();
        }, 0);
    }

    function createReportBlock(type) {
        if (type === 'table') {
            return {
                id: makeId('table'),
                type: 'table',
                title: '',
                hasHeaderRow: true,
                rows: [
                    createReportTableRow(3, ['Показатель', 'Значение', 'Комментарий']),
                    createReportTableRow(3)
                ]
            };
        }
        if (type === 'image') {
            return { id: makeId('image'), type: 'image', title: '', alt: '', attachmentId: null, imageUrl: '', fileName: '', mimeType: '', size: 0 };
        }
        return { id: makeId('text'), type: 'text', content: '', mode: 'paragraph' };
    }

    function selectReportBlock(blockId) {
        if (!blockId) {
            return;
        }
        if (state.reportSelectedBlockId === blockId) {
            return;
        }
        state.reportSelectedBlockId = blockId;
        if (getReportBlock(blockId)?.type !== 'table') {
            state.reportTableSelection = null;
        }
        updateReportSelectionUi();
        renderReportOutline();
        renderReportProperties();
        renderReportValidationSummary();
    }

    function updateReportSelectionUi() {
        $('#studentReportDocumentCanvas')?.querySelectorAll('[data-report-block-id]').forEach(element => {
            element.classList.toggle('selected', element.dataset.reportBlockId === state.reportSelectedBlockId);
        });
    }

    function moveReportBlock(blockId, direction) {
        const index = state.reportDocument.content.findIndex(block => block.id === blockId);
        if (index < 0) {
            return;
        }
        const target = direction === 'up' ? index - 1 : index + 1;
        if (target < 0 || target >= state.reportDocument.content.length) {
            return;
        }
        const [block] = state.reportDocument.content.splice(index, 1);
        state.reportDocument.content.splice(target, 0, block);
        state.reportSelectedBlockId = block.id;
        markReportDirty();
        renderReportEditor();
        renderReportSummary();
    }

    function deleteReportBlock(blockId) {
        const index = state.reportDocument.content.findIndex(block => block.id === blockId);
        if (index < 0) {
            return;
        }
        state.reportDocument.content.splice(index, 1);
        state.reportSelectedBlockId = state.reportDocument.content[Math.min(index, state.reportDocument.content.length - 1)]?.id || null;
        state.reportTableSelection = null;
        markReportDirty();
        renderReportEditor();
        renderReportSummary();
    }

    function mutateReportTable(blockId, action, rowId, cellId) {
        const block = getReportBlock(blockId);
        if (!block || block.type !== 'table') {
            return;
        }

        block.rows = normalizeReportTableRows(block.rows);
        const selection = getPrimaryTableSelection(block, rowId, cellId);
        const columnCount = getReportTableColumnCount(block);
        const rowIndex = Math.max(0, selection.rowIndex);
        const colIndex = Math.max(0, selection.colIndex);

        if (action === 'add-row' || action === 'add-row-below') {
            block.rows.splice(rowIndex + 1, 0, createReportTableRow(columnCount));
        }
        if (action === 'add-row-above') {
            block.rows.splice(rowIndex, 0, createReportTableRow(columnCount));
        }
        if (action === 'add-column' || action === 'add-column-right') {
            block.rows.forEach(row => row.cells.splice(colIndex + 1, 0, createReportTableCell('')));
        }
        if (action === 'add-column-left') {
            block.rows.forEach(row => row.cells.splice(colIndex, 0, createReportTableCell('')));
        }
        if (action === 'remove-row' || action === 'delete-row') {
            if (block.rows.length > 1) {
                block.rows.splice(rowIndex, 1);
            }
        }
        if (action === 'remove-column' || action === 'delete-column') {
            if (columnCount > 1) {
                block.rows.forEach(row => removeReportTableColumn(row, colIndex));
            }
        }
        if (action === 'merge-cells') {
            mergeSelectedReportTableCells(block);
        }
        if (action === 'split-cell') {
            splitSelectedReportTableCell(block);
        }

        hideReportTableContextMenu();
        markReportDirty();
        renderReportEditor();
        renderReportSummary();
    }

    function handleReportProblemClick(event) {
        const problemButton = event.target instanceof Element ? event.target.closest('[data-report-problem-block]') : null;
        if (!problemButton) {
            return;
        }

        const blockId = problemButton.dataset.reportProblemBlock;
        selectReportBlock(blockId);
        document.querySelector(`[data-report-block-id="${blockId}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function handleReportTableMenuClick(event) {
        const actionButton = event.target instanceof Element ? event.target.closest('[data-table-action]') : null;
        if (!actionButton) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        mutateReportTable(actionButton.dataset.blockId, actionButton.dataset.tableAction, actionButton.dataset.rowId, actionButton.dataset.cellId);
    }

    function selectReportTableCell(blockId, rowId, cellId, extendSelection) {
        const block = getReportBlock(blockId);
        if (!block || block.type !== 'table' || !getReportTableCell(block, rowId, cellId)) {
            return;
        }

        state.reportSelectedBlockId = blockId;
        const current = state.reportTableSelection?.blockId === blockId ? state.reportTableSelection.cellIds : [];
        let next = [cellId];
        if (extendSelection && current.length) {
            next = current.includes(cellId) ? current.filter(id => id !== cellId) : [...current, cellId];
            if (!next.length) {
                next = [cellId];
            }
        }
        state.reportTableSelection = { blockId, cellIds: next };
        renderReportEditor();
        setTimeout(() => {
            document.querySelector(`[data-table-cell][data-block-id="${blockId}"][data-cell-id="${cellId}"]`)?.focus();
        }, 0);
    }

    function getSelectedTableCellIds(blockId) {
        return state.reportTableSelection?.blockId === blockId ? state.reportTableSelection.cellIds : [];
    }

    function showReportTableContextMenu(blockId, rowId, cellId, clientX, clientY) {
        const menu = $('#studentTableContextMenu');
        const block = getReportBlock(blockId);
        if (!menu || !block) {
            return;
        }

        const canMerge = canMergeSelectedTableCells(block);
        const canSplit = canSplitSelectedTableCell(block);
        menu.innerHTML = `
            <button type="button" data-table-action="add-row-above" data-block-id="${blockId}" data-row-id="${rowId}" data-cell-id="${cellId}">Добавить строку выше</button>
            <button type="button" data-table-action="add-row-below" data-block-id="${blockId}" data-row-id="${rowId}" data-cell-id="${cellId}">Добавить строку ниже</button>
            <button type="button" data-table-action="add-column-left" data-block-id="${blockId}" data-row-id="${rowId}" data-cell-id="${cellId}">Добавить столбец слева</button>
            <button type="button" data-table-action="add-column-right" data-block-id="${blockId}" data-row-id="${rowId}" data-cell-id="${cellId}">Добавить столбец справа</button>
            <hr>
            <button type="button" data-table-action="merge-cells" data-block-id="${blockId}" ${canMerge ? '' : 'disabled'}>Объединить выделенные</button>
            <button type="button" data-table-action="split-cell" data-block-id="${blockId}" ${canSplit ? '' : 'disabled'}>Разделить ячейку</button>
            <hr>
            <button type="button" data-table-action="delete-row" data-block-id="${blockId}" data-row-id="${rowId}" data-cell-id="${cellId}">Удалить строку</button>
            <button type="button" data-table-action="delete-column" data-block-id="${blockId}" data-row-id="${rowId}" data-cell-id="${cellId}">Удалить столбец</button>`;
        menu.hidden = false;
        menu.style.left = `${Math.min(clientX, window.innerWidth - 240)}px`;
        menu.style.top = `${Math.min(clientY, window.innerHeight - 280)}px`;
    }

    function hideReportTableContextMenu() {
        const menu = $('#studentTableContextMenu');
        if (menu) {
            menu.hidden = true;
            menu.innerHTML = '';
        }
    }

    function createReportTableCell(text) {
        return { id: makeId('cell'), text: String(text || ''), colspan: 1, rowspan: 1 };
    }

    function createReportTableRow(columnCount, values) {
        const rowValues = Array.isArray(values) ? values : [];
        return {
            id: makeId('row'),
            cells: Array.from({ length: Math.max(1, columnCount || rowValues.length || 1) }, (_, index) => createReportTableCell(rowValues[index] || ''))
        };
    }

    function normalizeReportTableRows(rows) {
        if (!Array.isArray(rows) || !rows.length) {
            return [createReportTableRow(3, ['Показатель', 'Значение', 'Комментарий']), createReportTableRow(3)];
        }

        return rows.map(row => {
            if (Array.isArray(row)) {
                return createReportTableRow(Math.max(1, row.length), row);
            }

            return {
                id: row.id || makeId('row'),
                cells: Array.isArray(row.cells) && row.cells.length
                    ? row.cells.map(cell => ({
                        id: cell.id || makeId('cell'),
                        text: String(cell.text || ''),
                        colspan: Math.max(1, Number(cell.colspan || 1)),
                        rowspan: Math.max(1, Number(cell.rowspan || 1)),
                        hidden: Boolean(cell.hidden)
                    }))
                    : [createReportTableCell('')]
            };
        });
    }

    function getReportTableCell(block, rowId, cellId) {
        if (!block || block.type !== 'table') {
            return null;
        }
        const row = block.rows?.find(item => item.id === rowId);
        return row?.cells?.find(cell => cell.id === cellId) || null;
    }

    function getReportTableColumnCount(block) {
        return Math.max(1, ...(block.rows || []).map(row => (row.cells || []).filter(cell => !cell.hidden).length));
    }

    function getPrimaryTableSelection(block, rowId, cellId) {
        const selectedCellId = state.reportTableSelection?.blockId === block.id ? state.reportTableSelection.cellIds[0] : cellId;
        for (let rowIndex = 0; rowIndex < block.rows.length; rowIndex += 1) {
            const visibleCells = block.rows[rowIndex].cells.filter(cell => !cell.hidden);
            const colIndex = visibleCells.findIndex(cell => cell.id === selectedCellId);
            if (colIndex >= 0) {
                return { rowIndex, colIndex, rowId: block.rows[rowIndex].id, cellId: selectedCellId };
            }
        }
        return { rowIndex: 0, colIndex: 0, rowId: block.rows[0]?.id, cellId: block.rows[0]?.cells?.[0]?.id };
    }

    function removeReportTableColumn(row, visibleColIndex) {
        const visibleCells = row.cells.filter(cell => !cell.hidden);
        const target = visibleCells[visibleColIndex];
        const physicalIndex = row.cells.findIndex(cell => cell.id === target?.id);
        if (physicalIndex >= 0) {
            row.cells.splice(physicalIndex, 1);
        }
        if (!row.cells.some(cell => !cell.hidden)) {
            row.cells.push(createReportTableCell(''));
        }
    }

    function getSelectedTablePositions(block) {
        const selectedIds = getSelectedTableCellIds(block.id);
        const positions = [];
        block.rows.forEach((row, rowIndex) => {
            let colIndex = 0;
            row.cells.forEach((cell, physicalIndex) => {
                if (cell.hidden) {
                    return;
                }
                if (selectedIds.includes(cell.id)) {
                    positions.push({ row, rowIndex, cell, colIndex, physicalIndex });
                }
                colIndex += 1;
            });
        });
        return positions;
    }

    function canMergeSelectedTableCells(block) {
        const positions = getSelectedTablePositions(block);
        if (positions.length < 2) {
            return false;
        }
        if (!positions.every(item => (item.cell.colspan || 1) === 1 && (item.cell.rowspan || 1) === 1)) {
            return false;
        }
        const rows = [...new Set(positions.map(item => item.rowIndex))].sort((a, b) => a - b);
        const cols = [...new Set(positions.map(item => item.colIndex))].sort((a, b) => a - b);
        const rowsAreAdjacent = rows.every((row, index) => index === 0 || row === rows[index - 1] + 1);
        const colsAreAdjacent = cols.every((col, index) => index === 0 || col === cols[index - 1] + 1);
        return rowsAreAdjacent && colsAreAdjacent && positions.length === rows.length * cols.length;
    }

    function canSplitSelectedTableCell(block) {
        const positions = getSelectedTablePositions(block);
        return positions.length === 1 && ((positions[0].cell.colspan || 1) > 1 || (positions[0].cell.rowspan || 1) > 1);
    }

    function mergeSelectedReportTableCells(block) {
        if (!canMergeSelectedTableCells(block)) {
            return;
        }
        const positions = getSelectedTablePositions(block);
        const rows = [...new Set(positions.map(item => item.rowIndex))].sort((a, b) => a - b);
        const cols = [...new Set(positions.map(item => item.colIndex))].sort((a, b) => a - b);
        const primaryPosition = positions.find(item => item.rowIndex === rows[0] && item.colIndex === cols[0]) || positions[0];
        const primary = primaryPosition.cell;
        primary.colspan = cols.length;
        primary.rowspan = rows.length;
        primary.text = primary.text || positions.map(item => item.cell.text).filter(Boolean).join(' ');
        positions.forEach(item => {
            if (item.cell.id !== primary.id) {
                item.cell.hidden = true;
            }
        });
        state.reportTableSelection = { blockId: block.id, cellIds: [primary.id] };
    }

    function splitSelectedReportTableCell(block) {
        if (!canSplitSelectedTableCell(block)) {
            return;
        }
        const [{ rowIndex, colIndex, physicalIndex, cell }] = getSelectedTablePositions(block);
        const span = Math.max(1, Number(cell.colspan || 1));
        const rowSpan = Math.max(1, Number(cell.rowspan || 1));
        cell.colspan = 1;
        cell.rowspan = 1;
        for (let currentRowIndex = rowIndex; currentRowIndex < rowIndex + rowSpan; currentRowIndex += 1) {
            const currentRow = block.rows[currentRowIndex];
            if (!currentRow) {
                continue;
            }
            for (let currentColIndex = 0; currentColIndex < span; currentColIndex += 1) {
                if (currentRowIndex === rowIndex && currentColIndex === 0) {
                    continue;
                }
                const targetIndex = currentRowIndex === rowIndex ? physicalIndex + currentColIndex : colIndex + currentColIndex;
                const target = currentRow.cells[targetIndex];
                if (target) {
                    target.hidden = false;
                    target.colspan = 1;
                    target.rowspan = 1;
                } else {
                    currentRow.cells.splice(targetIndex, 0, createReportTableCell(''));
                }
            }
        }
        state.reportTableSelection = { blockId: block.id, cellIds: [cell.id] };
    }

    async function handleReportImageSelected(event) {
        const file = event.target.files?.[0];
        const block = getReportBlock(state.reportSelectedBlockId);
        if (!file || !block || block.type !== 'image') {
            return;
        }
        const errors = validateImageFile(file);
        if (errors.length) {
            setReportSaveState('error', errors.join(' '));
            event.target.value = '';
            return;
        }
        if (block.previewUrl?.startsWith('blob:')) {
            URL.revokeObjectURL(block.previewUrl);
        }
        block.pendingFile = file;
        block.uploadClientId = makeId('upload');
        block.previewUrl = URL.createObjectURL(file);
        block.fileName = file.name;
        block.mimeType = file.type || 'application/octet-stream';
        block.size = file.size;
        block.attachmentId = null;
        if (!block.title && !block.caption) {
            block.title = file.name.replace(/\.[^.]+$/, '');
            block.caption = block.title;
        }
        markReportDirty();
        renderReportEditor();
        renderReportSummary();
        event.target.value = '';
    }

    function markReportDirty() {
        state.reportDirty = true;
        setReportSaveState('dirty', 'Есть несохранённые изменения');
        clearTimeout(state.reportAutosaveTimer);
        state.reportAutosaveTimer = setTimeout(() => {
            if (state.reportEditorOpen && state.reportDirty && !state.reportSaving) {
                saveCurrentDiaryFromReportEditor(true);
            }
        }, 2800);
    }

    async function saveDiaryEntry(event) {
        event.preventDefault();
        await saveCurrentDiary(false);
    }

    async function saveCurrentDiaryFromReportEditor(isAutosave) {
        await saveCurrentDiary(isAutosave);
    }

    async function saveCurrentDiary(isAutosave) {
        if (!state.currentDetails || !state.selectedDate) {
            return;
        }

        clearStudentFieldErrors();
        const validation = validateReportDocument(state.reportDocument);
        if (validation.length) {
            renderFieldErrors({ DetailedReport: validation });
            setReportSaveState('error', validation.join(' '));
            showStatus('Проверьте подробный отчёт за день.', true);
            return;
        }

        const prepared = prepareReportDocumentForSave(state.reportDocument);
        const figures = await readFigurePayloadsFromReport(state.reportDocument);
        const payload = {
            workDate: $('#diaryWorkDate').value,
            shortDescription: $('#diaryShortDescription').value,
            detailedReport: JSON.stringify(prepared.document),
            keptAttachmentIds: prepared.keptAttachmentIds,
            figures
        };

        state.reportSaving = true;
        setReportSaveState('saving', isAutosave ? 'Автосохранение...' : 'Сохранение...');
        const result = await postJson(withAssignment(urls.saveDiary, state.currentDetails.assignmentId), payload);
        state.reportSaving = false;

        if (!result.ok) {
            renderFieldErrors(result.errors || {});
            setReportSaveState('error', result.message || 'Ошибка сохранения');
            showStatus(result.message || 'Не удалось сохранить запись дневника.', true);
            return;
        }

        applyUpdatedDetails(result.data);
        state.reportDirty = false;
        hideReportCloseGuard();
        setReportSaveState('saved', isAutosave ? 'Автосохранено' : 'Сохранено');
        showStatus(isAutosave ? 'Черновик отчёта автосохранён.' : 'Запись дневника сохранена.', false);
        if (state.reportEditorOpen) {
            renderReportEditor();
        }
    }

    function validateReportDocument(reportDocument) {
        const problems = collectReportProblems(reportDocument);
        return problems.length ? [buildReportProblemsSummary(problems)] : [];
    }

    function collectReportProblems(reportDocument) {
        const problems = [];
        const blocks = reportDocument?.content || [];
        if (!blocks.length) {
            problems.push({ blockId: null, kind: 'empty-report', message: 'отчёт пока пустой' });
            return problems;
        }

        blocks.forEach(block => {
            if (block.type === 'text' && !String(block.content || '').trim()) {
                problems.push({ blockId: block.id, kind: 'empty-text', message: 'текстовый блок пустой' });
            }
            if (block.type === 'table') {
                const rows = normalizeReportTableRows(block.rows);
                const hasCells = rows.some(row => row.cells.some(cell => !cell.hidden && String(cell.text || '').trim()));
                if (!String(block.title || block.caption || '').trim()) {
                    problems.push({ blockId: block.id, kind: 'table-title', message: 'таблица без подписи' });
                }
                if (!hasCells) {
                    problems.push({ blockId: block.id, kind: 'empty-table', message: 'таблица без данных' });
                }
            }
            if (block.type === 'image') {
                if (!block.attachmentId && !block.pendingFile) {
                    problems.push({ blockId: block.id, kind: 'image-file', message: 'рисунок без изображения' });
                }
                if (!String(block.title || block.caption || '').trim()) {
                    problems.push({ blockId: block.id, kind: 'image-title', message: 'рисунок без подписи' });
                }
            }
        });
        return problems;
    }

    function buildReportProblemsSummary(problems) {
        const grouped = problems.reduce((acc, problem) => {
            acc.set(problem.message, (acc.get(problem.message) || 0) + 1);
            return acc;
        }, new Map());
        const details = Array.from(grouped.entries()).map(([message, count]) => `${count} ${message}`).join(', ');
        return `Есть ${problems.length} ${getProblemWord(problems.length)}: ${details}.`;
    }

    function getProblemWord(count) {
        const last = count % 10;
        const lastTwo = count % 100;
        if (last === 1 && lastTwo !== 11) return 'проблема';
        if ([2, 3, 4].includes(last) && ![12, 13, 14].includes(lastTwo)) return 'проблемы';
        return 'проблем';
    }

    function groupReportProblemsByBlock(problems) {
        const map = new Map();
        problems.forEach(problem => {
            if (!problem.blockId) {
                return;
            }
            const list = map.get(problem.blockId) || [];
            list.push(problem);
            map.set(problem.blockId, list);
        });
        return map;
    }

    function renderReportValidationSummary() {
        const summary = $('#studentReportValidationSummary');
        if (!summary) {
            return;
        }
        const problems = collectReportProblems(state.reportDocument);
        if (!problems.length) {
            summary.hidden = true;
            summary.innerHTML = '';
            return;
        }

        summary.hidden = false;
        summary.innerHTML = `
            <strong>${buildReportProblemsSummary(problems)}</strong>
            <div>
                ${problems.filter(problem => problem.blockId).map(problem => `
                    <button type="button" data-report-problem-block="${problem.blockId}">${escapeHtml(problem.message)}</button>
                `).join('')}
            </div>`;
    }

    function prepareReportDocumentForSave(reportDocument) {
        const keptAttachmentIds = [];
        const cleanContent = reportDocument.content.map(block => {
            if (block.type === 'image') {
                const clean = {
                    id: block.id,
                    type: 'image',
                    title: block.title || block.caption || '',
                    alt: block.alt || '',
                    attachmentId: block.attachmentId || null,
                    imageUrl: block.imageUrl || '',
                    uploadClientId: block.pendingFile ? block.uploadClientId : undefined,
                    fileName: block.fileName || '',
                    mimeType: block.mimeType || '',
                    size: block.size || 0
                };
                if (clean.attachmentId) {
                    keptAttachmentIds.push(clean.attachmentId);
                }
                return clean;
            }
            if (block.type === 'table') {
                return {
                    id: block.id,
                    type: 'table',
                    title: block.title || block.caption || '',
                    hasHeaderRow: Boolean(block.hasHeaderRow || block.hasHeader),
                    rows: normalizeReportTableRows(block.rows).map(row => ({
                        id: row.id,
                        cells: row.cells.map(cell => ({
                            id: cell.id,
                            text: String(cell.text || '').trim(),
                            colspan: Math.max(1, Number(cell.colspan || 1)),
                            rowspan: Math.max(1, Number(cell.rowspan || 1)),
                            hidden: Boolean(cell.hidden)
                        }))
                    }))
                };
            }
            return {
                id: block.id,
                type: 'text',
                content: String(block.content || stripHtml(block.html || '') || ''),
                mode: block.mode || 'paragraph'
            };
        });

        const cleanDocument = {
            version: 3,
            type: 'practice-day-report',
            blocks: cleanContent,
            attachments: cleanContent
                .filter(block => block.type === 'image' && block.attachmentId)
                .map(block => ({
                    id: block.attachmentId,
                    type: 'image',
                    filename: block.fileName || '',
                    mimeType: block.mimeType || '',
                    size: block.size || 0
                }))
        };

        return { document: cleanDocument, keptAttachmentIds };
    }

    async function readFigurePayloadsFromReport(reportDocument) {
        const figures = [];
        const figureBlocks = reportDocument.content.filter(block => block.type === 'image' && block.pendingFile);
        for (let i = 0; i < figureBlocks.length; i += 1) {
            const block = figureBlocks[i];
            figures.push({
                clientId: block.uploadClientId,
                caption: block.title || block.caption || block.pendingFile.name,
                fileName: block.pendingFile.name,
                contentType: block.pendingFile.type || 'application/octet-stream',
                base64Content: await fileToBase64(block.pendingFile),
                sortOrder: getFigureSortOrder(block.id)
            });
        }
        return figures;
    }

    function setReportSaveState(kind, text) {
        const element = $('#studentReportSaveState');
        if (!element) {
            return;
        }
        element.className = `student-report-save-state ${kind}`;
        element.textContent = text;
    }

    async function saveReportItems() {
        const items = $$('[data-report-row]').map(row => ({
            category: row.dataset.category || '',
            name: row.querySelector('[data-report-name]')?.value || '',
            description: row.querySelector('[data-report-description]')?.value || ''
        })).filter(item => item.name.trim());

        const result = await postJson(withAssignment(urls.saveReportItems, state.currentDetails.assignmentId), { items });
        if (!result.ok) {
            showStatus(result.message || 'Не удалось сохранить таблицы отчёта.', true);
            return;
        }
        applyUpdatedDetails(result.data);
        showStatus('Таблицы отчёта сохранены.', false);
    }

    function renderReportTables(items) {
        $('#studentReportTables').innerHTML = reportCategories.map(category => {
            const rows = items.filter(item => item.category === category.key);
            return `
                <div class="student-report-table-block" data-report-category="${category.key}">
                    <div class="student-report-table-header">
                        <h4>${category.title}</h4>
                        <button type="button" class="student-mini-button" data-add-report-row="${category.key}">Добавить</button>
                    </div>
                    <div data-report-rows="${category.key}">
                        ${rows.length ? rows.map(item => buildReportRow(category.key, item)).join('') : buildReportRow(category.key)}
                    </div>
                </div>`;
        }).join('');
    }

    function addReportRow(category) {
        $(`[data-report-rows="${category}"]`)?.insertAdjacentHTML('beforeend', buildReportRow(category));
    }

    function buildReportRow(category, item) {
        const meta = reportCategories.find(x => x.key === category) || reportCategories[0];
        return `
            <div class="student-report-row" data-report-row data-category="${category}">
                <input class="form-input" data-report-name value="${escapeHtmlAttribute(item?.name || '')}" placeholder="${escapeHtmlAttribute(meta.namePlaceholder)}" />
                <textarea class="form-input" data-report-description rows="3" placeholder="${escapeHtmlAttribute(meta.descriptionPlaceholder)}">${escapeHtml(item?.description || '')}</textarea>
                <div class="student-report-row-actions">
                    <button type="button" class="student-mini-button" data-remove-report-row>Удалить</button>
                </div>
            </div>`;
    }

    async function saveSources() {
        const sources = $$('[data-source-row]').map(row => ({
            title: row.querySelector('[data-source-title]')?.value || '',
            url: row.querySelector('[data-source-url]')?.value || '',
            description: row.querySelector('[data-source-description]')?.value || ''
        })).filter(source => source.title.trim() || source.url.trim() || source.description.trim());

        const result = await postJson(withAssignment(urls.saveSources, state.currentDetails.assignmentId), { sources });
        if (!result.ok) {
            showStatus(result.message || 'Не удалось сохранить источники.', true);
            return;
        }
        applyUpdatedDetails(result.data);
        showStatus('Источники сохранены.', false);
    }

    function renderSources(sources) {
        $('#studentSourcesList').innerHTML = sources.length ? sources.map(source => buildSourceRow(source)).join('') : buildSourceRow();
    }

    function addSourceRow(source) {
        $('#studentSourcesList').insertAdjacentHTML('beforeend', buildSourceRow(source));
    }

    function buildSourceRow(source) {
        return `
            <div class="student-source-row" data-source-row>
                <input class="form-input" data-source-title value="${escapeHtmlAttribute(source?.title || '')}" placeholder="Название источника" />
                <input class="form-input" data-source-url value="${escapeHtmlAttribute(source?.url || '')}" placeholder="Ссылка" />
                <input class="form-input" data-source-description value="${escapeHtmlAttribute(source?.description || '')}" placeholder="Комментарий" />
                <div class="student-source-row-actions">
                    <button type="button" class="student-mini-button" data-remove-source-row>Удалить</button>
                </div>
            </div>`;
    }

    async function uploadAppendix(event) {
        event.preventDefault();
        clearStudentFieldErrors();
        const file = $('#appendixFile')?.files?.[0];
        const allowed = ['doc', 'docx', 'pdf', 'zip', 'rar', '7z', 'txt', 'cs', 'sql', 'png', 'jpg', 'jpeg'];
        const ext = file?.name.split('.').pop()?.toLowerCase() || '';

        if (!file || !allowed.includes(ext)) {
            renderFieldErrors({ AppendixFile: ['Выберите допустимый файл: DOCX, PDF, архив, код или изображение.'] });
            return;
        }

        if (file.size > 15 * 1024 * 1024) {
            renderFieldErrors({ AppendixFile: ['Файл приложения не должен превышать 15 МБ.'] });
            return;
        }

        const response = await fetch(withAssignment(urls.uploadAppendix, state.currentDetails.assignmentId), {
            method: 'POST',
            body: new FormData(event.currentTarget)
        });
        if (!response.ok) {
            const error = await safeReadJson(response);
            showStatus(error?.message || 'Не удалось загрузить приложение.', true);
            return;
        }

        event.currentTarget.reset();
        updateAppendixFileName();
        applyUpdatedDetails(await response.json());
        showStatus('Приложение загружено.', false);
    }

    async function deleteAppendix(appendixId) {
        if (!appendixId || !state.currentDetails) {
            return;
        }
        const response = await fetch(`${urls.deleteAppendix}?appendixId=${encodeURIComponent(appendixId)}`, { method: 'POST' });
        if (!response.ok) {
            const error = await safeReadJson(response);
            showStatus(error?.message || 'Не удалось удалить приложение.', true);
            return;
        }
        await loadPracticeDetails(state.currentDetails.assignmentId, true, true);
        showStatus('Приложение удалено.', false);
    }

    function renderAppendices(appendices) {
        $('#studentAppendicesList').innerHTML = appendices.length
            ? appendices.map(item => `
                <article class="student-compact-card">
                    <strong>${escapeHtml(item.title)}</strong>
                    <p>${escapeHtml(item.description || item.fileName)}</p>
                    <small>${escapeHtml(item.fileName)} · ${formatBytes(item.sizeBytes)} · ${formatDateTime(item.createdAtUtc)}</small>
                    <div class="student-appendix-actions">
                        <a class="student-mini-button" href="${urls.downloadAppendix}?appendixId=${encodeURIComponent(item.id)}">Скачать</a>
                        <button type="button" class="student-mini-button" data-delete-appendix="${item.id}">Удалить</button>
                    </div>
                </article>`).join('')
            : '<div class="student-empty-state">Приложения пока не загружены.</div>';
    }

    function updateAppendixFileName() {
        const file = $('#appendixFile')?.files?.[0];
        $('#appendixFileName').textContent = file ? `${file.name} · ${formatBytes(file.size)}` : 'Файл не выбран';
        clearFieldError('AppendixFile');
    }

    function applyUpdatedDetails(details) {
        state.currentDetails = details;
        state.detailsByAssignment.set(details.assignmentId, details);
        state.practices = state.practices.map(item => item.assignmentId === details.assignmentId ? toListItem(details) : item);
        renderPractices();
        renderPracticeModal(details);
    }

    function activateModalTab(tab) {
        $$('[data-student-modal-tab]').forEach(button => button.classList.toggle('active', button.dataset.studentModalTab === tab));
        $$('[data-student-modal-panel]').forEach(panel => panel.classList.toggle('active', panel.dataset.studentModalPanel === tab));
    }

    function closePracticeModal() {
        if (state.reportEditorOpen) {
            requestCloseReportEditor();
            return;
        }
        $('#studentPracticeModal').hidden = true;
        document.body.style.overflow = '';
        state.currentDetails = null;
        hideStatus();
    }

    function showStatus(message, isError) {
        const status = $('#studentPracticeModalStatus');
        if (!status || !message) {
            return;
        }
        status.hidden = false;
        status.textContent = message;
        status.classList.toggle('error', Boolean(isError));
    }

    function hideStatus() {
        const status = $('#studentPracticeModalStatus');
        if (!status) {
            return;
        }
        status.hidden = true;
        status.textContent = '';
        status.classList.remove('error');
    }

    function openPracticeFromQuery() {
        const practiceId = Number(new URLSearchParams(window.location.search).get('practiceId') || '0');
        if (practiceId > 0) {
            activatePanel('studentPracticesPanel');
            openPractice(practiceId);
        }
    }

    function getSelectedDiaryEntry() {
        return (state.currentDetails?.diaryEntries || []).find(item => toDateInputValue(item.workDate) === state.selectedDate) || null;
    }

    function getReportBlock(blockId) {
        return state.reportDocument.content.find(block => block.id === blockId) || null;
    }

    function getBlockNumber(type, blockId) {
        return state.reportDocument.content.filter(block => block.type === type).findIndex(block => block.id === blockId) + 1 || 1;
    }

    function getFigureSortOrder(blockId) {
        return state.reportDocument.content.filter(block => block.type === 'image').findIndex(block => block.id === blockId) + 1 || 1;
    }

    function toListItem(details) {
        return {
            assignmentId: details.assignmentId,
            practiceId: details.practiceId,
            practiceIndex: details.practiceIndex,
            name: details.name,
            specialtyCode: details.specialtyCode,
            specialtyName: details.specialtyName,
            professionalModuleCode: details.professionalModuleCode,
            professionalModuleName: details.professionalModuleName,
            hours: details.hours,
            startDate: details.startDate,
            endDate: details.endDate,
            isCompleted: details.isCompleted,
            supervisorFullName: details.supervisorFullName,
            organizationName: details.organizationName,
            hasRequiredDetails: details.hasRequiredDetails,
            detailsDueDate: details.detailsDueDate,
            isDetailsOverdue: details.isDetailsOverdue,
            diaryEntriesCount: details.diaryEntriesCount,
            workDaysCount: details.workDaysCount
        };
    }
}

function createEmptyReportDocument() {
    return { version: 3, type: 'practice-day-report', content: [], attachments: [] };
}

function ensureReportDocumentShape(reportDocument) {
    if (!reportDocument) {
        return createEmptyReportDocument();
    }
    reportDocument.version = 3;
    reportDocument.type = reportDocument.type || 'practice-day-report';
    reportDocument.content = Array.isArray(reportDocument.content) ? reportDocument.content : [];
    reportDocument.attachments = Array.isArray(reportDocument.attachments) ? reportDocument.attachments : [];
    return reportDocument;
}

function createReportTableCell(text) {
    return { id: makeId('cell'), text: String(text || ''), colspan: 1, rowspan: 1 };
}

function createReportTableRow(columnCount, values) {
    const rowValues = Array.isArray(values) ? values : [];
    return {
        id: makeId('row'),
        cells: Array.from({ length: Math.max(1, columnCount || rowValues.length || 1) }, (_, index) => createReportTableCell(rowValues[index] || ''))
    };
}

function normalizeReportTableRows(rows) {
    if (!Array.isArray(rows) || !rows.length) {
        return [createReportTableRow(3, ['Показатель', 'Значение', 'Комментарий']), createReportTableRow(3)];
    }

    return rows.map(row => {
        if (Array.isArray(row)) {
            return createReportTableRow(Math.max(1, row.length), row);
        }

        return {
            id: row.id || makeId('row'),
            cells: Array.isArray(row.cells) && row.cells.length
                ? row.cells.map(cell => ({
                    id: cell.id || makeId('cell'),
                    text: String(cell.text || ''),
                    colspan: Math.max(1, Number(cell.colspan || 1)),
                    rowspan: Math.max(1, Number(cell.rowspan || 1)),
                    hidden: Boolean(cell.hidden)
                }))
                : [createReportTableCell('')]
        };
    });
}

function parseReportDocument(value, attachments) {
    const attachmentList = Array.isArray(attachments) ? attachments : [];
    if (!value) {
        return createEmptyReportDocument();
    }

    try {
        const parsed = JSON.parse(value);
        if (parsed?.version === 3 && Array.isArray(parsed.blocks)) {
            const documentModel = {
                version: 3,
                type: parsed.type || 'practice-day-report',
                content: parsed.blocks.map(block => normalizeReportBlock(block, attachmentList)).filter(Boolean),
                attachments: parsed.attachments || []
            };
            appendUnreferencedAttachments(documentModel, attachmentList);
            return documentModel;
        }
        if (parsed?.version === 2 && Array.isArray(parsed.content)) {
            const documentModel = {
                version: 3,
                type: parsed.type || 'practice-day-report',
                content: parsed.content.map(block => normalizeReportBlock(block, attachmentList)).filter(Boolean),
                attachments: parsed.attachments || []
            };
            appendUnreferencedAttachments(documentModel, attachmentList);
            return documentModel;
        }
        if (Array.isArray(parsed)) {
            return convertLegacyBlocksToReportDocument(parsed, attachmentList);
        }
    } catch {
        const fallbackDocument = {
            version: 3,
            type: 'practice-day-report',
            content: [{ id: makeId('text'), type: 'text', content: String(value || ''), mode: 'paragraph' }],
            attachments: []
        };
        appendUnreferencedAttachments(fallbackDocument, attachmentList);
        return fallbackDocument;
    }

    return createEmptyReportDocument();
}

function normalizeReportBlock(block, attachments) {
    if (!block || typeof block !== 'object') {
        return null;
    }
    if (block.type === 'text' || block.type === 'paragraph' || block.type === 'heading') {
        return {
            id: block.id || makeId('text'),
            type: 'text',
            content: block.content || stripHtml(block.html || block.text || ''),
            mode: ['paragraph', 'bullet_list', 'numbered_list'].includes(block.mode) ? block.mode : 'paragraph'
        };
    }
    if (block.type === 'table') {
        return {
            id: block.id || makeId('table'),
            type: 'table',
            title: block.title || block.caption || '',
            caption: block.title || block.caption || '',
            hasHeaderRow: Boolean(block.hasHeaderRow ?? block.hasHeader),
            rows: normalizeReportTableRows(block.rows)
        };
    }
    if (block.type === 'image' || block.type === 'figure') {
        const attachment = attachments.find(item => item.id === block.attachmentId);
        return {
            id: block.id || makeId('image'),
            type: 'image',
            title: block.title || block.caption || attachment?.caption || '',
            caption: block.title || block.caption || attachment?.caption || '',
            alt: block.alt || '',
            attachmentId: block.attachmentId || null,
            imageUrl: block.imageUrl || '',
            fileName: block.fileName || attachment?.fileName || '',
            mimeType: block.mimeType || attachment?.contentType || '',
            size: block.size || attachment?.sizeBytes || 0
        };
    }
    return { id: block.id || makeId('text'), type: 'text', content: stripHtml(block.html || block.text || ''), mode: 'paragraph' };
}

function convertLegacyBlocksToReportDocument(blocks, attachments) {
    let figureIndex = 0;
    return {
        version: 3,
        type: 'practice-day-report',
        content: blocks.map(block => {
            if (block.type === 'table') {
                return {
                    id: makeId('table'),
                    type: 'table',
                    title: block.title || block.caption || '',
                    caption: block.title || block.caption || '',
                    hasHeaderRow: true,
                    rows: normalizeReportTableRows(block.rows)
                };
            }
            if (block.type === 'figure' || block.type === 'image') {
                const attachment = attachments[figureIndex++];
                return {
                    id: makeId('image'),
                    type: 'image',
                    title: block.title || block.caption || attachment?.caption || '',
                    caption: block.title || block.caption || attachment?.caption || '',
                    alt: '',
                    attachmentId: attachment?.id || null,
                    imageUrl: block.imageUrl || '',
                    fileName: attachment?.fileName || block.fileName || '',
                    mimeType: attachment?.contentType || '',
                    size: attachment?.sizeBytes || 0
                };
            }
            return { id: makeId('text'), type: 'text', content: block.content || block.text || stripHtml(block.html || ''), mode: 'paragraph' };
        }).filter(Boolean),
        attachments: attachments.map(item => ({ id: item.id, type: 'image', filename: item.fileName, mimeType: item.contentType, size: item.sizeBytes }))
    };
}

function appendUnreferencedAttachments(documentModel, attachments) {
    const usedIds = new Set(documentModel.content.filter(block => block.type === 'image' && block.attachmentId).map(block => block.attachmentId));
    attachments.filter(item => !usedIds.has(item.id)).forEach(item => {
        documentModel.content.push({
            id: makeId('image'),
            type: 'image',
            title: item.caption || '',
            caption: item.caption || '',
            alt: '',
            attachmentId: item.id,
            imageUrl: '',
            fileName: item.fileName,
            mimeType: item.contentType,
            size: item.sizeBytes
        });
    });
}

function getReportStats(reportDocument) {
    return {
        text: reportDocument.content.filter(block => block.type === 'text').length,
        tables: reportDocument.content.filter(block => block.type === 'table').length,
        figures: reportDocument.content.filter(block => block.type === 'image').length
    };
}

function buildReportExcerpt(reportDocument) {
    if (!reportDocument.content.length) {
        return 'Отчёт пока пустой. Откройте редактор и добавьте текст, таблицу или рисунок.';
    }
    const text = reportDocument.content
        .map(block => {
            if (block.type === 'table') return block.title || block.caption || 'Таблица без подписи';
            if (block.type === 'image') return block.title || block.caption || 'Рисунок без подписи';
            return block.content || stripHtml(block.html || '');
        })
        .filter(Boolean)
        .join(' · ');
    return text.length > 180 ? `${text.slice(0, 180)}...` : text;
}

function getReportBlockTitle(block) {
    if (block.type === 'table') return block.title || block.caption || 'Таблица без подписи';
    if (block.type === 'image') return block.title || block.caption || 'Рисунок без подписи';
    return String(block.content || stripHtml(block.html || '')).slice(0, 48) || 'Пустой текстовый блок';
}

function getReportBlockKind(block) {
    if (block.type === 'table') return 'Таблица';
    if (block.type === 'image') return 'Рисунок';
    return 'Текст';
}

function validateOrganizationClient(payload) {
    const errors = {};
    const phone = (payload.organizationSupervisorPhone || '').trim();
    const email = (payload.organizationSupervisorEmail || '').trim();
    if (!payload.organizationName?.trim()) errors.OrganizationName = ['Укажите организацию.'];
    if (!payload.organizationSupervisorFullName?.trim()) errors.OrganizationSupervisorFullName = ['Укажите ФИО руководителя.'];
    if (!payload.organizationSupervisorPosition?.trim()) errors.OrganizationSupervisorPosition = ['Укажите должность руководителя.'];
    if (!phone && !email) errors.OrganizationSupervisorPhone = ['Укажите телефон или почту руководителя.'];
    if (phone && !phone.startsWith('+7')) errors.OrganizationSupervisorPhone = ['Телефон должен начинаться с +7.'];
    if (phone && phone.replace(/\D/g, '').length !== 11) errors.OrganizationSupervisorPhone = ['Телефон должен быть в формате +7 (999) 999-99-99.'];
    if (email && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) errors.OrganizationSupervisorEmail = ['Почта указана некорректно.'];
    if (!payload.practiceTaskContent?.trim()) errors.PracticeTaskContent = ['Опишите содержание задания.'];
    return errors;
}

function validateImageFile(file) {
    const errors = [];
    if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type)) {
        errors.push('Можно загрузить только PNG, JPG или WEBP.');
    }
    if (file.size > 8 * 1024 * 1024) {
        errors.push('Размер рисунка не должен превышать 8 МБ.');
    }
    return errors;
}

function normalizeEditableHtml(html) {
    return String(html || '').trim();
}

function stripHtml(html) {
    const element = document.createElement('div');
    element.innerHTML = html || '';
    return element.textContent.trim();
}

function withAssignment(url, assignmentId) {
    return `${url}?assignmentId=${encodeURIComponent(assignmentId)}`;
}

function readFormValues(form) {
    return Object.fromEntries(new FormData(form).entries());
}

async function postJson(url, payload) {
    const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    if (!response.ok) {
        const error = await safeReadJson(response);
        return { ok: false, message: error?.message || null, errors: error?.errors || {} };
    }
    return { ok: true, data: await response.json() };
}

function sortPractices(items, sort) {
    return [...items].sort((a, b) => {
        if (sort === 'date-desc') return new Date(b.startDate) - new Date(a.startDate);
        if (sort === 'name-asc') return String(a.name || '').localeCompare(String(b.name || ''), 'ru');
        if (sort === 'progress-asc') return Number(a.hasRequiredDetails) - Number(b.hasRequiredDetails);
        return new Date(a.startDate) - new Date(b.startDate);
    });
}

function buildPracticeSearchText(practice) {
    return [practice.practiceIndex, practice.name, practice.specialtyCode, practice.specialtyName, practice.professionalModuleCode, practice.professionalModuleName, practice.supervisorFullName, practice.organizationName].filter(Boolean).join(' ').toLowerCase();
}

function getPracticeDays(startDate, endDate) {
    const days = [];
    const start = new Date(startDate);
    const end = new Date(endDate);
    for (let day = new Date(start); day <= end; day.setDate(day.getDate() + 1)) {
        days.push(new Date(day));
    }
    return days;
}

function getDefaultDiaryDate(details) {
    const entries = new Set((details.diaryEntries || []).map(entry => toDateInputValue(entry.workDate)));
    const today = toDateInputValue(new Date());
    const workDays = getPracticeDays(details.startDate, details.endDate).filter(day => day.getDay() !== 0 && day.getDay() !== 6);
    const todayIsOpen = workDays.some(day => toDateInputValue(day) === today) && !entries.has(today);
    if (todayIsOpen) {
        return today;
    }
    return toDateInputValue(workDays.find(day => !entries.has(toDateInputValue(day))) || workDays[0] || new Date(details.startDate));
}

function fileToBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result || '').split(',')[1] || '');
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

function formatRuPhone(value) {
    let digits = String(value || '').replace(/\D/g, '');
    if (digits.startsWith('8')) digits = `7${digits.slice(1)}`;
    if (!digits.startsWith('7')) digits = `7${digits}`;
    digits = digits.slice(0, 11);
    const rest = digits.slice(1);
    let result = '+7';
    if (rest.length) result += ` (${rest.slice(0, 3)}`;
    if (rest.length >= 3) result += ')';
    if (rest.length > 3) result += ` ${rest.slice(3, 6)}`;
    if (rest.length > 6) result += `-${rest.slice(6, 8)}`;
    if (rest.length > 8) result += `-${rest.slice(8, 10)}`;
    return result;
}

function renderFieldErrors(errors) {
    clearStudentFieldErrors();
    Object.entries(errors || {}).forEach(([key, messages]) => {
        const normalized = key.replace(/^.*\./, '').replace(/\[\d+\]/g, '');
        const target = document.querySelector(`[data-field-error="${normalized}"]`);
        if (target) {
            target.textContent = Array.isArray(messages) ? messages.join(' ') : String(messages || '');
        }
    });
}

function clearStudentFieldErrors() {
    document.querySelectorAll('.student-field-error').forEach(item => {
        item.textContent = '';
    });
}

function clearFieldError(key) {
    const target = document.querySelector(`[data-field-error="${key}"]`);
    if (target) {
        target.textContent = '';
    }
}

function toDateInputValue(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '';
    }
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function formatDate(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return 'Не указано';
    }
    return new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
}

function formatDateTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return 'Не указано';
    }
    return new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(date);
}

function formatWeekday(value) {
    return new Intl.DateTimeFormat('ru-RU', { weekday: 'short' }).format(value).replace('.', '');
}

function formatBytes(bytes) {
    const value = Number(bytes || 0);
    if (value < 1024) return `${value} Б`;
    if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} КБ`;
    return `${(value / (1024 * 1024)).toFixed(1)} МБ`;
}

function makeId(prefix) {
    return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}

function getClosest(event, selector) {
    return event.target instanceof Element ? event.target.closest(selector) : null;
}

function escapeHtml(value) {
    return String(value || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

function escapeHtmlAttribute(value) {
    return escapeHtml(value).replace(/`/g, '&#96;');
}

async function safeReadJson(response) {
    try {
        return await response.json();
    } catch {
        return null;
    }
}

// Future DOCX integration point: convert the canonical report JSON into blocks
// that can be mapped to OpenXml paragraphs, tables and drawing elements.
function normalizeReportDocumentForDocx(reportDocument) {
    return (reportDocument?.content || []).map(block => {
        if (block.type === 'table') {
            return { kind: 'table', title: block.title || block.caption || '', hasHeaderRow: Boolean(block.hasHeaderRow || block.hasHeader), rows: normalizeReportTableRows(block.rows) };
        }
        if (block.type === 'image') {
            return { kind: 'image', attachmentId: block.attachmentId, title: block.title || block.caption || '', alt: block.alt || '' };
        }
        return { kind: 'text', content: block.content || stripHtml(block.html || ''), mode: block.mode || 'paragraph' };
    });
}
