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
        organizationConfirmArmed: false
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
            const button = event.target.closest('[data-open-practice]');
            if (button) {
                openPractice(Number(button.dataset.openPractice || '0'));
            }
        });

        $('#closeStudentPracticeModal')?.addEventListener('click', closeModal);
        $('#studentPracticeModal')?.addEventListener('click', event => {
            if (event.target.id === 'studentPracticeModal') {
                closeModal();
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
            const button = event.target.closest('[data-calendar-date]');
            if (button && !button.disabled) {
                selectDiaryDate(button.dataset.calendarDate);
            }
        });

        workspace.addEventListener('click', event => {
            const addBlock = event.target.closest('[data-add-report-block]');
            if (addBlock) {
                addRichBlock(addBlock.dataset.addReportBlock || 'text');
            }

            const addTableRow = event.target.closest('[data-add-table-row]');
            if (addTableRow) {
                addRichTableRow(addTableRow.closest('[data-rich-block]'));
            }

            const remove = event.target.closest('[data-remove-rich-block], [data-remove-table-row], [data-remove-report-row], [data-remove-source-row]');
            if (remove) {
                remove.closest('[data-rich-block], [data-rich-table-row], [data-report-row], [data-source-row]')?.remove();
            }

            const addReportRowButton = event.target.closest('[data-add-report-row]');
            if (addReportRowButton) {
                addReportRow(addReportRowButton.dataset.addReportRow || 'TechnicalTool');
            }

            const deleteAppendixButton = event.target.closest('[data-delete-appendix]');
            if (deleteAppendixButton) {
                deleteAppendix(Number(deleteAppendixButton.dataset.deleteAppendix || '0'));
            }

            const figurePicker = event.target.closest('[data-pick-figure]');
            if (figurePicker) {
                figurePicker.closest('[data-rich-block]')?.querySelector('[data-figure-file]')?.click();
            }
        });

        workspace.addEventListener('change', event => {
            if (event.target.matches('[data-figure-file]')) {
                const file = event.target.files?.[0];
                const label = event.target.closest('[data-rich-block]')?.querySelector('[data-figure-file-name]');
                if (label) {
                    label.textContent = file ? `${file.name} · ${formatBytes(file.size)}` : 'Файл не выбран';
                }
                clearFieldError('DetailedReport');
            }
        });
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
            showStatus('После сохранения руководитель практики получит уведомление об изменении сведений. Это важно, потому что эти данные используются в отчётных документах и при проверке практики. Нажмите “Сохранить сведения” ещё раз, чтобы подтвердить изменение.', true);
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
        $('#diaryEditorTitle').textContent = `Рабочий день · ${formatDate(date)}`;
        const entry = (state.currentDetails.diaryEntries || []).find(item => toDateInputValue(item.workDate) === date);
        $('#diaryShortDescription').value = entry?.shortDescription || '';
        renderRichBlocks(parseReportBlocks(entry?.detailedReport), entry?.attachments || []);
        renderDiaryCalendar(state.currentDetails);
        if (scrollIntoView) {
            $('#studentDiaryForm')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }

    function renderRichBlocks(blocks, attachments) {
        const builder = $('#studentRichReportBuilder');
        const normalized = Array.isArray(blocks) && blocks.length ? blocks : [{ type: 'text', text: '' }];
        builder.innerHTML = normalized.map(buildRichBlock).join('');

        if (attachments?.length) {
            builder.insertAdjacentHTML('beforeend', `
                <div class="student-existing-figures">
                    <strong>Загруженные рисунки</strong>
                    ${attachments.map(file => `
                        <a class="student-existing-figure" href="${urls.downloadDiaryAttachment}?attachmentId=${encodeURIComponent(file.id)}">
                            <span>${escapeHtml(file.caption || file.fileName)}</span>
                            <small>${escapeHtml(file.fileName)} · ${formatBytes(file.sizeBytes)}</small>
                        </a>`).join('')}
                </div>`);
        }
    }

    function addRichBlock(type) {
        $('#studentRichReportBuilder').insertAdjacentHTML('beforeend', buildRichBlock({ type }));
    }

    function buildRichBlock(block) {
        const type = block?.type || 'text';
        if (type === 'table') {
            const rows = Array.isArray(block.rows) && block.rows.length ? block.rows : [['', '', '']];
            return `
                <section class="student-rich-block" data-rich-block="table">
                    ${richBlockHeader('Таблица в отчёте')}
                    <div class="student-rich-table">
                        ${rows.map(row => buildRichTableRow(row)).join('')}
                    </div>
                    <button type="button" class="student-mini-button" data-add-table-row>Добавить строку</button>
                </section>`;
        }

        if (type === 'figure') {
            return `
                <section class="student-rich-block" data-rich-block="figure">
                    ${richBlockHeader('Рисунок')}
                    <input class="form-input" data-figure-caption maxlength="220" value="${escapeHtmlAttribute(block.caption || '')}" placeholder="Название рисунка, например: Рисунок 1 - Структура базы данных" />
                    <input type="file" accept="image/png,image/jpeg,image/webp" data-figure-file hidden />
                    <button type="button" class="student-file-dropzone student-figure-dropzone" data-pick-figure>
                        <strong>Выберите изображение</strong>
                        <span>PNG, JPG или WEBP до 8 МБ</span>
                        <small data-figure-file-name>${escapeHtml(block.fileName || 'Файл не выбран')}</small>
                    </button>
                </section>`;
        }

        return `
            <section class="student-rich-block" data-rich-block="text">
                ${richBlockHeader('Текстовый блок')}
                <textarea class="form-input student-rich-textarea" data-rich-text rows="6" placeholder="Опишите выполненную работу, решения, проблемы и результат дня...">${escapeHtml(block.text || '')}</textarea>
            </section>`;
    }

    function richBlockHeader(title) {
        return `
            <div class="student-rich-block-header">
                <span>${title}</span>
                <button type="button" class="student-mini-button" data-remove-rich-block>Удалить</button>
            </div>`;
    }

    function buildRichTableRow(cells) {
        const safeCells = Array.isArray(cells) ? cells : ['', '', ''];
        return `
            <div class="student-rich-table-row" data-rich-table-row>
                <input class="form-input" value="${escapeHtmlAttribute(safeCells[0] || '')}" placeholder="Параметр" />
                <input class="form-input" value="${escapeHtmlAttribute(safeCells[1] || '')}" placeholder="Значение" />
                <input class="form-input" value="${escapeHtmlAttribute(safeCells[2] || '')}" placeholder="Комментарий" />
                <button type="button" class="student-mini-button" data-remove-table-row>×</button>
            </div>`;
    }

    function addRichTableRow(block) {
        block?.querySelector('.student-rich-table')?.insertAdjacentHTML('beforeend', buildRichTableRow(['', '', '']));
    }

    async function saveDiaryEntry(event) {
        event.preventDefault();
        clearStudentFieldErrors();

        const figureValidation = validateFigureBlocks();
        if (figureValidation.length) {
            renderFieldErrors({ DetailedReport: figureValidation });
            showStatus('Проверьте рисунки в подробном отчёте.', true);
            return;
        }

        const payload = {
            workDate: $('#diaryWorkDate').value,
            shortDescription: $('#diaryShortDescription').value,
            detailedReport: JSON.stringify(serializeRichReport()),
            figures: await readFigurePayloads()
        };

        const result = await postJson(withAssignment(urls.saveDiary, state.currentDetails.assignmentId), payload);
        if (!result.ok) {
            renderFieldErrors(result.errors || {});
            showStatus(result.message || 'Не удалось сохранить запись дневника.', true);
            return;
        }

        applyUpdatedDetails(result.data);
        showStatus('Запись дневника сохранена.', false);
    }

    function serializeRichReport() {
        return $$('#studentRichReportBuilder [data-rich-block]').map(block => {
            const type = block.dataset.richBlock;
            if (type === 'table') {
                return {
                    type,
                    rows: Array.from(block.querySelectorAll('[data-rich-table-row]'))
                        .map(row => Array.from(row.querySelectorAll('input')).map(input => input.value.trim()))
                        .filter(row => row.some(Boolean))
                };
            }
            if (type === 'figure') {
                return {
                    type,
                    caption: block.querySelector('[data-figure-caption]')?.value.trim() || '',
                    fileName: block.querySelector('[data-figure-file]')?.files?.[0]?.name || block.querySelector('[data-figure-file-name]')?.textContent || ''
                };
            }
            return { type: 'text', text: block.querySelector('[data-rich-text]')?.value.trim() || '' };
        }).filter(block => block.type !== 'text' || block.text);
    }

    function validateFigureBlocks() {
        const errors = [];
        $$('#studentRichReportBuilder [data-rich-block="figure"]').forEach(block => {
            const file = block.querySelector('[data-figure-file]')?.files?.[0];
            const caption = block.querySelector('[data-figure-caption]')?.value.trim();
            if (file && !file.type.startsWith('image/')) {
                errors.push('К рисункам можно прикреплять только изображения.');
            }
            if (file && file.size > 8 * 1024 * 1024) {
                errors.push('Размер одного рисунка не должен превышать 8 МБ.');
            }
            if (file && !caption) {
                errors.push('Укажите название для каждого нового рисунка.');
            }
        });
        return errors;
    }

    async function readFigurePayloads() {
        const payloads = [];
        const blocks = $$('#studentRichReportBuilder [data-rich-block="figure"]');
        for (let i = 0; i < blocks.length; i += 1) {
            const block = blocks[i];
            const file = block.querySelector('[data-figure-file]')?.files?.[0];
            if (!file) {
                continue;
            }
            payloads.push({
                caption: block.querySelector('[data-figure-caption]')?.value.trim() || file.name,
                fileName: file.name,
                contentType: file.type || 'application/octet-stream',
                base64Content: await fileToBase64(file),
                sortOrder: i + 1
            });
        }
        return payloads;
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

    function closeModal() {
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

function parseReportBlocks(value) {
    if (!value) {
        return [];
    }
    try {
        const parsed = JSON.parse(value);
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        return [{ type: 'text', text: value }];
    }
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
