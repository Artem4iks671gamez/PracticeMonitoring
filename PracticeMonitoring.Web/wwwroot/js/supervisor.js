document.addEventListener('DOMContentLoaded', () => {
    const workspace = document.querySelector('[data-supervisor-workspace]');
    if (!workspace) {
        return;
    }

    initSupervisorWorkspace(workspace);
});

function initSupervisorWorkspace(workspace) {
    const dashboard = window.initialSupervisorDashboard || {};
    const students = Array.isArray(dashboard.students) ? dashboard.students : [];
    const practices = Array.isArray(dashboard.practices) ? dashboard.practices : [];
    const risks = Array.isArray(dashboard.risks) ? dashboard.risks : [];
    let activeDetails = null;

    const $ = selector => workspace.querySelector(selector);
    const $$ = selector => Array.from(workspace.querySelectorAll(selector));

    const filters = {
        search: '',
        practiceId: 'all',
        group: 'all',
        status: 'all'
    };

    bindEvents();
    renderAll();
    openAssignmentFromQuery();

    function bindEvents() {
        $$('[data-supervisor-panel-target]').forEach(button => {
            button.addEventListener('click', () => activatePanel(button.dataset.supervisorPanelTarget));
        });

        $$('[data-risk-filter]').forEach(button => {
            button.addEventListener('click', () => {
                $$('[data-risk-filter]').forEach(item => item.classList.toggle('active', item === button));
                filters.status = button.dataset.riskFilter || 'all';
                const statusFilter = $('#supervisorStatusFilter');
                if (statusFilter) {
                    statusFilter.value = filters.status;
                }
                activatePanel('studentsPanel');
                renderStudents();
            });
        });

        $('#supervisorStudentSearch')?.addEventListener('input', event => {
            filters.search = event.target.value.trim().toLowerCase();
            renderStudents();
        });

        $('#supervisorPracticeFilter')?.addEventListener('change', event => {
            filters.practiceId = event.target.value;
            renderStudents();
        });

        $('#supervisorGroupFilter')?.addEventListener('change', event => {
            filters.group = event.target.value;
            renderStudents();
        });

        $('#supervisorStatusFilter')?.addEventListener('change', event => {
            filters.status = event.target.value;
            renderStudents();
        });

        workspace.addEventListener('click', event => {
            const target = event.target instanceof Element ? event.target : null;
            if (!target) {
                return;
            }

            const detailsButton = target.closest('[data-open-supervisor-assignment]');
            if (detailsButton) {
                openAssignmentDetails(Number(detailsButton.dataset.openSupervisorAssignment || '0'));
                return;
            }

            const dayReportButton = target.closest('[data-open-supervisor-day-report]');
            if (dayReportButton) {
                const entry = findActiveDiaryEntry(Number(dayReportButton.dataset.openSupervisorDayReport || '0'));
                renderDayReportViewer(entry);
                return;
            }

            const reviewButton = target.closest('[data-supervisor-review-save]');
            if (reviewButton) {
                saveDiaryReview(Number(reviewButton.dataset.supervisorReviewSave || '0'));
                return;
            }

            const sectionCommentButton = target.closest('[data-supervisor-section-comment-save]');
            if (sectionCommentButton) {
                saveSectionComment(sectionCommentButton.dataset.supervisorSectionCommentSave || '');
                return;
            }

            if (target.id === 'supervisorStudentDetailsModal') {
                closeDetailsModal();
            }
        });

        $('#closeSupervisorStudentDetailsButton')?.addEventListener('click', closeDetailsModal);
    }

    function renderAll() {
        renderKpis();
        renderCharts();
        renderFilters();
        renderStudents();
        renderPractices();
        renderRisks();
        renderDocuments();
    }

    function activatePanel(panelId) {
        $$('[data-supervisor-panel-target]').forEach(button => {
            button.classList.toggle('active', button.dataset.supervisorPanelTarget === panelId);
        });
        $$('[data-supervisor-panel]').forEach(panel => {
            panel.classList.toggle('active', panel.id === panelId);
        });
    }

    function renderKpis() {
        const summary = dashboard.summary || {};
        const items = [
            ['Студентов', summary.totalStudents || 0, 'Назначено на вас'],
            ['Активных', summary.activeStudents || 0, 'Практика ещё идёт'],
            ['Средний прогресс', `${summary.averageProgress || 0}%`, 'По всем студентам'],
            ['Критичных', summary.criticalCount || 0, 'Нужно вмешаться'],
            ['Требуют внимания', summary.attentionCount || 0, 'Есть отставание'],
            ['Отчёт готов', summary.reportReadyCount || 0, 'Можно принимать']
        ];

        const target = $('#supervisorKpiGrid');
        if (!target) {
            return;
        }

        target.innerHTML = items.map(([label, value, hint]) => `
            <article class="supervisor-kpi-card">
                <span>${escapeHtml(label)}</span>
                <strong>${escapeHtml(String(value))}</strong>
                <p>${escapeHtml(hint)}</p>
            </article>
        `).join('');
    }

    function renderCharts() {
        renderBarChart('#progressBucketsChart', dashboard.progressBuckets || [], false);
        renderBarChart('#groupProgressChart', dashboard.groupProgress || [], true);
        renderRiskDistribution();
    }

    function renderBarChart(selector, points, valueIsPercent) {
        const target = $(selector);
        if (!target) {
            return;
        }

        if (!Array.isArray(points) || points.length === 0) {
            target.innerHTML = '<div class="supervisor-empty-state">Нет данных для графика.</div>';
            return;
        }

        target.innerHTML = points.map(point => {
            const percent = valueIsPercent ? point.value : point.percent;
            const value = valueIsPercent ? `${point.value}%` : point.value;
            return `
                <div class="supervisor-chart-row">
                    <div class="supervisor-chart-label">${escapeHtml(point.label)}</div>
                    <div class="supervisor-chart-track">
                        <span style="width:${Math.max(2, Math.min(100, percent || 0))}%"></span>
                    </div>
                    <strong>${escapeHtml(String(value))}</strong>
                </div>`;
        }).join('');
    }

    function renderRiskDistribution() {
        const target = $('#riskDistributionChart');
        const points = Array.isArray(dashboard.riskDistribution) ? dashboard.riskDistribution : [];
        if (!target) {
            return;
        }

        if (points.length === 0) {
            target.innerHTML = '<div class="supervisor-empty-state">Нет данных о рисках.</div>';
            return;
        }

        target.innerHTML = points.map(point => `
            <div class="supervisor-risk-pill">
                <span>${escapeHtml(point.label)}</span>
                <strong>${point.value}</strong>
                <em>${point.percent}%</em>
            </div>
        `).join('');
    }

    function renderFilters() {
        const practiceFilter = $('#supervisorPracticeFilter');
        if (practiceFilter) {
            practiceFilter.innerHTML = '<option value="all">Все практики</option>' + practices.map(item => `
                <option value="${item.practiceId}">${escapeHtml(`${item.practiceIndex} ${item.practiceName}`)}</option>
            `).join('');
        }

        const groups = [...new Set(students.map(x => x.groupName || 'Без группы'))].sort();
        const groupFilter = $('#supervisorGroupFilter');
        if (groupFilter) {
            groupFilter.innerHTML = '<option value="all">Все группы</option>' + groups.map(group => `
                <option value="${escapeHtmlAttribute(group)}">${escapeHtml(group)}</option>
            `).join('');
        }
    }

    function getFilteredStudents() {
        return students.filter(student => {
            const haystack = [
                student.studentFullName,
                student.groupName,
                student.practiceIndex,
                student.practiceName,
                student.organizationName
            ].join(' ').toLowerCase();

            if (filters.search && !haystack.includes(filters.search)) {
                return false;
            }

            if (filters.practiceId !== 'all' && String(student.practiceId) !== String(filters.practiceId)) {
                return false;
            }

            if (filters.group !== 'all' && String(student.groupName || 'Без группы') !== filters.group) {
                return false;
            }

            if (filters.status === 'ready') {
                return student.isReportReady;
            }

            return filters.status === 'all' || student.riskLevel === filters.status;
        });
    }

    function renderStudents() {
        const target = $('#supervisorStudentsList');
        if (!target) {
            return;
        }

        const filtered = getFilteredStudents();
        if (filtered.length === 0) {
            target.innerHTML = '<div class="supervisor-empty-state">По выбранным фильтрам студентов нет.</div>';
            return;
        }

        target.innerHTML = filtered.map(student => `
            <article class="supervisor-student-card risk-${escapeHtmlAttribute(student.riskLevel)}">
                <div class="supervisor-student-main">
                    <div class="supervisor-student-avatar">${buildAvatar(student)}</div>
                    <div>
                        <h3>${escapeHtml(student.studentFullName)}</h3>
                        <p>${escapeHtml(student.groupName || 'Без группы')} · ${escapeHtml(student.practiceIndex)} ${escapeHtml(student.practiceName)}</p>
                        <span>${escapeHtml(student.organizationName || 'Организация не указана')}</span>
                    </div>
                </div>
                <div class="supervisor-progress-cell">
                    <div class="supervisor-progress-head">
                        <strong>${student.progressPercent}%</strong>
                        <span>${escapeHtml(student.riskLabel)}</span>
                    </div>
                    <div class="supervisor-progress-bar"><span style="width:${student.progressPercent}%"></span></div>
                    <small>${escapeHtml(student.mainIssue || '')}</small>
                </div>
                <div class="supervisor-student-metrics">
                    <span>Дневник: ${student.diaryEntriesCount}/${student.expectedDiaryEntriesCount || student.workDaysCount}</span>
                    <span>Подробно: ${student.detailedReportsCount}</span>
                    <span>Проверено: ${student.gradedDiaryEntriesCount || 0}/${student.workDaysCount || 0}</span>
                    <span>${student.isReportReady ? 'Отчёт готов' : 'Отчёт не готов'}</span>
                </div>
                <button type="button" class="supervisor-primary-button" data-open-supervisor-assignment="${student.assignmentId}">Открыть</button>
            </article>
        `).join('');
    }

    function renderPractices() {
        const target = $('#supervisorPracticesGrid');
        if (!target) {
            return;
        }

        if (practices.length === 0) {
            target.innerHTML = '<div class="supervisor-empty-state">В нагрузке пока нет практик.</div>';
            return;
        }

        target.innerHTML = practices.map(practice => `
            <article class="supervisor-practice-card">
                <div class="supervisor-card-header">
                    <div>
                        <h3>${escapeHtml(practice.practiceIndex)} ${escapeHtml(practice.practiceName)}</h3>
                        <p>${escapeHtml(practice.specialtyCode)} ${escapeHtml(practice.specialtyName)}</p>
                    </div>
                    <strong>${practice.averageProgress}%</strong>
                </div>
                <div class="supervisor-progress-bar"><span style="width:${practice.averageProgress}%"></span></div>
                <div class="supervisor-practice-stats">
                    <span>${practice.studentsCount} студ.</span>
                    <span>${practice.criticalCount} крит.</span>
                    <span>${practice.attentionCount} вним.</span>
                    <span>${practice.reportReadyCount} готово</span>
                </div>
                <small>${formatDate(practice.startDate)} - ${formatDate(practice.endDate)}</small>
            </article>
        `).join('');
    }

    function renderRisks() {
        const target = $('#supervisorRiskList');
        if (!target) {
            return;
        }

        if (risks.length === 0) {
            target.innerHTML = '<div class="supervisor-empty-state">Критичных рисков нет.</div>';
            return;
        }

        target.innerHTML = risks.map(risk => `
            <article class="supervisor-risk-card risk-${escapeHtmlAttribute(risk.riskLevel)}">
                <div>
                    <strong>${escapeHtml(risk.studentFullName)}</strong>
                    <p>${escapeHtml(risk.groupName || 'Без группы')} · ${escapeHtml(risk.practiceIndex)} ${escapeHtml(risk.practiceName)}</p>
                    <span>${escapeHtml(risk.message)}</span>
                </div>
                <div>
                    <em>${escapeHtml(risk.riskLabel)}</em>
                    <button type="button" class="supervisor-secondary-button" data-open-supervisor-assignment="${risk.assignmentId}">Карточка</button>
                </div>
            </article>
        `).join('');
    }

    function renderDocuments() {
        const target = $('#supervisorDocumentsGrid');
        if (!target) {
            return;
        }

        if (students.length === 0) {
            target.innerHTML = '<div class="supervisor-empty-state">Нет назначенных студентов.</div>';
            return;
        }

        target.innerHTML = students.map(student => `
            <article class="supervisor-document-card">
                <div>
                    <strong>${escapeHtml(student.studentFullName)}</strong>
                    <span>${escapeHtml(student.practiceIndex)} · ${escapeHtml(student.groupName || 'Без группы')}</span>
                </div>
                <div class="supervisor-document-checks">
                    ${buildCheck('Организация', student.hasOrganization)}
                    ${buildCheck('Введение', student.hasIntroduction)}
                    ${buildCheck('Техника', student.hasTechnicalTools)}
                    ${buildCheck('Дневник', student.diaryEntriesCount >= student.workDaysCount && student.detailedReportsCount >= student.workDaysCount)}
                    ${buildCheck('Проверка', (student.gradedDiaryEntriesCount || 0) >= student.workDaysCount)}
                    ${buildCheck('Источники', student.hasSources)}
                    ${buildCheck('Файлы', student.hasAppendices)}
                </div>
                <button type="button" class="supervisor-secondary-button" data-open-supervisor-assignment="${student.assignmentId}">Подробнее</button>
            </article>
        `).join('');
    }

    async function openAssignmentDetails(assignmentId) {
        if (!assignmentId) {
            return;
        }

        const modal = $('#supervisorStudentDetailsModal');
        const body = $('#supervisorStudentDetailsBody');
        if (!modal || !body) {
            return;
        }

        modal.hidden = false;
        body.innerHTML = '<div class="supervisor-empty-state">Загружается карточка студента...</div>';

        const url = `${workspace.dataset.assignmentDetailsUrl}?assignmentId=${encodeURIComponent(assignmentId)}`;
        const response = await fetch(url, { cache: 'no-store' });
        if (!response.ok) {
            body.innerHTML = '<div class="supervisor-empty-state">Не удалось загрузить карточку студента.</div>';
            return;
        }

        const details = await response.json();
        activeDetails = details;
        $('#supervisorStudentDetailsTitle').textContent = details.studentFullName || 'Студент';
        $('#supervisorStudentDetailsSubtitle').textContent = `${details.groupName || 'Без группы'} · ${details.practiceIndex} ${details.practiceName}`;
        body.innerHTML = buildDetailsBody(details);
        renderDayReportViewer((details.diaryEntries || []).find(entry => entry.hasDetailedReport) || null);
    }

    function closeDetailsModal() {
        const modal = $('#supervisorStudentDetailsModal');
        if (modal) {
            modal.hidden = true;
        }
        activeDetails = null;
    }

    function buildDetailsBody(details) {
        return `
            <div class="supervisor-details-summary">
                <div class="supervisor-details-progress">
                    <strong>${details.progressPercent}%</strong>
                    <span>${escapeHtml(details.riskLabel)}</span>
                    <div class="supervisor-progress-bar"><span style="width:${details.progressPercent}%"></span></div>
                    <p>${escapeHtml(details.mainIssue || '')}</p>
                </div>
                <div class="supervisor-details-metrics">
                    <span>Дневник: ${details.diaryEntriesCount}/${details.expectedDiaryEntriesCount || details.workDaysCount}</span>
                    <span>Подробных отчётов: ${details.detailedReportsCount}</span>
                    <span>Проверено руководителем: ${details.gradedDiaryEntriesCount || 0}/${details.workDaysCount || 0}</span>
                    <span>Последняя активность: ${formatDateTime(details.lastActivityAtUtc)}</span>
                </div>
            </div>

            <section class="supervisor-details-section">
                <h3>Готовность разделов</h3>
                <div class="supervisor-section-check-grid">
                    ${(details.reportSections || []).map(section => `
                        <div class="supervisor-section-check ${section.isReady ? 'ready' : 'missing'}">
                            <strong>${escapeHtml(section.name)}</strong>
                            <span>${escapeHtml(section.description)}</span>
                        </div>
                    `).join('')}
                </div>
            </section>

            <section class="supervisor-details-section">
                <h3>Комментарии к разделам</h3>
                <div class="supervisor-section-comment-grid">
                    ${buildSectionCommentEditor(details, 'organization', 'Данные об организации')}
                    ${buildSectionCommentEditor(details, 'introduction', 'Введение')}
                    ${buildSectionCommentEditor(details, 'sources', 'Источники')}
                </div>
            </section>

            <section class="supervisor-details-section">
                <h3>Организация</h3>
                <div class="supervisor-details-grid">
                    ${buildInfo('Полное название', details.organizationFullName || details.organizationName)}
                    ${buildInfo('Сокращённое название', details.organizationShortName)}
                    ${buildInfo('Адрес', details.organizationAddress)}
                    ${buildInfo('Руководитель от организации', details.organizationSupervisorFullName)}
                    ${buildInfo('Должность', details.organizationSupervisorPosition)}
                    ${buildInfo('Контакты', [details.organizationSupervisorPhone, details.organizationSupervisorEmail].filter(Boolean).join(' · '))}
                </div>
            </section>

            <section class="supervisor-details-section">
                <h3>Дневник</h3>
                <div class="supervisor-review-layout">
                    <div class="supervisor-diary-list">
                        ${(details.diaryEntries || []).length ? details.diaryEntries.map(entry => buildDiaryReviewCard(entry)).join('') : '<div class="supervisor-empty-state">Дневник пока не заполнен.</div>'}
                    </div>
                    <div class="supervisor-day-report-viewer" id="supervisorDayReportViewer">
                        <div class="supervisor-empty-state">Выберите рабочий день, чтобы открыть подробный отчёт.</div>
                    </div>
                </div>
            </section>

            <section class="supervisor-details-section">
                <h3>Источники и приложения</h3>
                <div class="supervisor-details-grid">
                    <div class="supervisor-details-list-block">
                        <strong>Источники</strong>
                        ${(details.sources || []).length ? details.sources.map(item => `<span>${escapeHtml(item.title)}</span>`).join('') : '<span>Не указаны</span>'}
                    </div>
                    <div class="supervisor-details-list-block">
                        <strong>Приложения</strong>
                        ${(details.appendices || []).length ? details.appendices.map(item => buildAppendixRow(item)).join('') : '<span>Не загружены</span>'}
                    </div>
                </div>
            </section>`;
    }

    function buildSectionCommentEditor(details, sectionKey, sectionTitle) {
        const comment = findSectionComment(details, sectionKey);
        return `
            <article class="supervisor-section-comment-card">
                <div>
                    <strong>${escapeHtml(sectionTitle)}</strong>
                    ${comment?.updatedAtUtc ? `<span>Последний комментарий: ${formatDateTime(comment.updatedAtUtc)}</span>` : '<span>Комментарий ещё не оставлен</span>'}
                </div>
                <textarea class="form-input"
                          rows="4"
                          maxlength="2000"
                          data-section-comment="${escapeHtmlAttribute(sectionKey)}"
                          placeholder="Комментарий будет виден студенту">${escapeHtml(comment?.comment || '')}</textarea>
                <button type="button" class="supervisor-primary-button" data-supervisor-section-comment-save="${escapeHtmlAttribute(sectionKey)}">Сохранить комментарий</button>
                <div class="supervisor-review-status" data-section-comment-status="${escapeHtmlAttribute(sectionKey)}"></div>
            </article>`;
    }

    function buildDiaryReviewCard(entry) {
        const reviewText = entry.isReviewed
            ? `Проверено${entry.supervisorGrade ? ` · оценка ${entry.supervisorGrade}` : ''}`
            : 'Ожидает проверки';
        const openButton = entry.hasDetailedReport
            ? `<button type="button" class="supervisor-secondary-button" data-open-supervisor-day-report="${entry.id}">Открыть отчёт</button>`
            : '';

        return `
            <article class="supervisor-diary-review-card ${entry.isReviewed ? 'reviewed' : 'pending'}">
                <div>
                    <strong>${formatDate(entry.workDate)}</strong>
                    <p>${escapeHtml(entry.shortDescription || 'Краткая запись не заполнена')}</p>
                    <span>${entry.hasDetailedReport ? 'Подробный отчёт есть' : 'Подробного отчёта нет'} · вложений: ${entry.attachmentsCount || 0}</span>
                    <em>${escapeHtml(reviewText)}</em>
                    ${entry.supervisorComment ? `<small>${escapeHtml(entry.supervisorComment)}</small>` : ''}
                </div>
                ${openButton}
            </article>`;
    }

    function buildAppendixRow(item) {
        const openUrl = withQuery(workspace.dataset.openAppendixUrl, 'appendixId', item.id);
        const downloadUrl = withQuery(workspace.dataset.downloadAppendixUrl, 'appendixId', item.id);
        return `
            <div class="supervisor-file-row">
                <span>${escapeHtml(item.title || item.fileName)} · ${escapeHtml(item.fileName)} · ${formatBytes(item.sizeBytes)}</span>
                ${item.description ? `<small>${escapeHtml(item.description)}</small>` : ''}
                <div class="supervisor-file-actions">
                    <a class="supervisor-secondary-button" href="${escapeHtmlAttribute(openUrl)}" target="_blank" rel="noopener noreferrer">Открыть</a>
                    <a class="supervisor-primary-button" href="${escapeHtmlAttribute(downloadUrl)}">Скачать</a>
                </div>
            </div>`;
    }

    function renderDayReportViewer(entry) {
        const target = $('#supervisorDayReportViewer');
        if (!target) {
            return;
        }

        if (!entry) {
            target.innerHTML = '<div class="supervisor-empty-state">Выберите рабочий день, чтобы открыть подробный отчёт.</div>';
            return;
        }

        const documentModel = parseReportDocument(entry.detailedReport);
        target.innerHTML = `
            <div class="supervisor-day-report-header">
                <div>
                    <span>Подробный отчёт за день</span>
                    <h4>${formatDate(entry.workDate)}</h4>
                    <p>${entry.isReviewed ? `Проверено · оценка ${entry.supervisorGrade || '-'}` : 'Ожидает проверки руководителем'}</p>
                </div>
                <div class="supervisor-review-form" data-review-form="${entry.id}">
                    <select class="form-input" data-review-grade="${entry.id}" aria-label="Оценка">
                        <option value="">Оценка</option>
                        ${[5, 4, 3, 2].map(value => `<option value="${value}" ${Number(entry.supervisorGrade) === value ? 'selected' : ''}>${value}</option>`).join('')}
                    </select>
                    <textarea class="form-input" rows="3" data-review-comment="${entry.id}" placeholder="Комментарий для студента">${escapeHtml(entry.supervisorComment || '')}</textarea>
                    <button type="button" class="supervisor-primary-button" data-supervisor-review-save="${entry.id}">Сохранить проверку</button>
                    <div class="supervisor-review-status" data-review-status="${entry.id}"></div>
                </div>
            </div>
            <div class="supervisor-report-document">
                ${documentModel.blocks.length ? documentModel.blocks.map(block => renderReportBlock(block)).join('') : '<div class="supervisor-empty-state">Подробный отчёт пустой.</div>'}
            </div>
            ${renderAttachmentList(entry.attachments || [])}`;
    }

    function renderReportBlock(block) {
        if (block.type === 'table') {
            const rows = Array.isArray(block.rows) ? block.rows : [];
            return `
                <section class="supervisor-report-block">
                    <strong>${escapeHtml(block.title || block.caption || 'Таблица без подписи')}</strong>
                    <div class="supervisor-report-table-wrap">
                        <table>
                            <tbody>
                                ${rows.map(row => `<tr>${(row.cells || []).filter(cell => !cell.hidden).map(cell => `<td colspan="${Number(cell.colspan || 1)}" rowspan="${Number(cell.rowspan || 1)}">${escapeHtml(cell.text || '')}</td>`).join('')}</tr>`).join('')}
                            </tbody>
                        </table>
                    </div>
                </section>`;
        }

        if (block.type === 'image' || block.type === 'figure') {
            const attachmentId = Number(block.attachmentId || 0);
            const openUrl = attachmentId ? withQuery(workspace.dataset.openDiaryAttachmentUrl, 'attachmentId', attachmentId) : '';
            const downloadUrl = attachmentId ? withQuery(workspace.dataset.downloadDiaryAttachmentUrl, 'attachmentId', attachmentId) : '';
            return `
                <section class="supervisor-report-block">
                    <strong>${escapeHtml(block.title || block.caption || 'Рисунок без подписи')}</strong>
                    ${openUrl ? `<img class="supervisor-report-image" src="${escapeHtmlAttribute(openUrl)}" alt="${escapeHtmlAttribute(block.alt || block.title || '')}" />` : '<div class="supervisor-empty-state">Файл рисунка не найден.</div>'}
                    ${downloadUrl ? `<a class="supervisor-secondary-button" href="${escapeHtmlAttribute(downloadUrl)}">Скачать рисунок</a>` : ''}
                </section>`;
        }

        const content = String(block.content || block.text || '');
        if (block.mode === 'bullet_list' || block.mode === 'numbered_list') {
            const items = content.split(/\r?\n/).map(item => item.trim()).filter(Boolean);
            const tag = block.mode === 'numbered_list' ? 'ol' : 'ul';
            return `<section class="supervisor-report-block"><${tag}>${items.map(item => `<li>${escapeHtml(item)}</li>`).join('')}</${tag}></section>`;
        }

        return `<section class="supervisor-report-block"><p>${escapeHtml(content || 'Пустой текстовый блок')}</p></section>`;
    }

    function renderAttachmentList(attachments) {
        if (!attachments.length) {
            return '';
        }

        return `
            <div class="supervisor-report-attachments">
                <strong>Вложения дня</strong>
                ${attachments.map(item => {
                    const openUrl = withQuery(workspace.dataset.openDiaryAttachmentUrl, 'attachmentId', item.id);
                    const downloadUrl = withQuery(workspace.dataset.downloadDiaryAttachmentUrl, 'attachmentId', item.id);
                    return `
                        <div class="supervisor-file-row">
                            <span>${escapeHtml(item.caption || item.fileName)} · ${escapeHtml(item.fileName)} · ${formatBytes(item.sizeBytes)}</span>
                            <div class="supervisor-file-actions">
                                <a class="supervisor-secondary-button" href="${escapeHtmlAttribute(openUrl)}" target="_blank" rel="noopener noreferrer">Открыть</a>
                                <a class="supervisor-primary-button" href="${escapeHtmlAttribute(downloadUrl)}">Скачать</a>
                            </div>
                        </div>`;
                }).join('')}
            </div>`;
    }

    async function saveDiaryReview(entryId) {
        const grade = Number(workspace.querySelector(`[data-review-grade="${entryId}"]`)?.value || '0');
        const comment = workspace.querySelector(`[data-review-comment="${entryId}"]`)?.value || '';
        const status = workspace.querySelector(`[data-review-status="${entryId}"]`);

        if (!grade) {
            if (status) {
                status.textContent = 'Выберите оценку.';
                status.classList.add('error');
            }
            return;
        }

        if (status) {
            status.textContent = 'Сохраняется...';
            status.classList.remove('error');
        }

        const response = await fetch(withQuery(workspace.dataset.reviewDiaryEntryUrl, 'entryId', entryId), {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ grade, comment })
        });

        if (!response.ok) {
            const error = await safeReadJson(response);
            if (status) {
                status.textContent = error?.message || 'Не удалось сохранить проверку.';
                status.classList.add('error');
            }
            return;
        }

        activeDetails = await response.json();
        const body = $('#supervisorStudentDetailsBody');
        if (body) {
            body.innerHTML = buildDetailsBody(activeDetails);
        }
        renderDayReportViewer(findActiveDiaryEntry(entryId));
    }

    async function saveSectionComment(sectionKey) {
        if (!activeDetails || !sectionKey) {
            return;
        }

        const textarea = workspace.querySelector(`[data-section-comment="${sectionKey}"]`);
        const status = workspace.querySelector(`[data-section-comment-status="${sectionKey}"]`);
        const comment = textarea?.value || '';

        if (!comment.trim()) {
            if (status) {
                status.textContent = 'Введите комментарий.';
                status.classList.add('error');
            }
            return;
        }

        if (status) {
            status.textContent = 'Сохраняется...';
            status.classList.remove('error');
        }

        let url = withQuery(workspace.dataset.saveSectionCommentUrl, 'assignmentId', activeDetails.assignmentId);
        url = withQuery(url, 'sectionKey', sectionKey);
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ comment })
        });

        if (!response.ok) {
            const error = await safeReadJson(response);
            if (status) {
                status.textContent = error?.message || 'Не удалось сохранить комментарий.';
                status.classList.add('error');
            }
            return;
        }

        activeDetails = await response.json();
        const body = $('#supervisorStudentDetailsBody');
        if (body) {
            body.innerHTML = buildDetailsBody(activeDetails);
        }
        renderDayReportViewer((activeDetails.diaryEntries || []).find(entry => entry.hasDetailedReport) || null);
    }

    function parseReportDocument(value) {
        if (!value) {
            return { blocks: [] };
        }

        try {
            const parsed = JSON.parse(value);
            return {
                blocks: Array.isArray(parsed.blocks) ? parsed.blocks : Array.isArray(parsed.content) ? parsed.content : []
            };
        } catch {
            return { blocks: [{ type: 'text', content: String(value || '') }] };
        }
    }

    function findActiveDiaryEntry(entryId) {
        return (activeDetails?.diaryEntries || []).find(entry => Number(entry.id) === Number(entryId)) || null;
    }

    function findSectionComment(details, sectionKey) {
        return (details?.sectionComments || []).find(item => item.sectionKey === sectionKey) || null;
    }

    function openAssignmentFromQuery() {
        const assignmentId = Number(new URLSearchParams(window.location.search).get('assignmentId') || '0');
        if (assignmentId > 0) {
            activatePanel('studentsPanel');
            openAssignmentDetails(assignmentId);
        }
    }

    function withQuery(baseUrl, key, value) {
        if (!baseUrl) {
            return '';
        }
        const separator = baseUrl.includes('?') ? '&' : '?';
        return `${baseUrl}${separator}${encodeURIComponent(key)}=${encodeURIComponent(value)}`;
    }

    async function safeReadJson(response) {
        try {
            return await response.json();
        } catch {
            return null;
        }
    }

    function buildAvatar(student) {
        if (student.studentAvatarUrl) {
            return `<img src="${escapeHtmlAttribute(student.studentAvatarUrl)}" alt="Аватар студента" />`;
        }

        return `<span>${escapeHtml((student.studentFullName || '?').trim()[0] || '?')}</span>`;
    }

    function buildCheck(label, isReady) {
        return `<span class="${isReady ? 'ready' : 'missing'}">${escapeHtml(label)}</span>`;
    }

    function buildInfo(label, value) {
        return `
            <div class="supervisor-info-item">
                <span>${escapeHtml(label)}</span>
                <strong>${escapeHtml(value || 'Не указано')}</strong>
            </div>`;
    }

    function formatDate(value) {
        if (!value) {
            return '-';
        }

        return new Date(value).toLocaleDateString('ru-RU');
    }

    function formatDateTime(value) {
        if (!value) {
            return 'нет данных';
        }

        return new Date(value).toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'short' });
    }

    function formatBytes(value) {
        const size = Number(value || 0);
        if (size < 1024) {
            return `${size} Р‘`;
        }
        if (size < 1024 * 1024) {
            return `${(size / 1024).toFixed(1)} КБ`;
        }
        return `${(size / 1024 / 1024).toFixed(1)} МБ`;
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function escapeHtmlAttribute(value) {
        return escapeHtml(value);
    }
}
