document.addEventListener('DOMContentLoaded', () => {
    initRolePanels();
    document.querySelectorAll('.messaging-module').forEach(initMessagingModule);
});

function initRolePanels() {
    const buttons = Array.from(document.querySelectorAll('[data-page-panel-target]'));
    const panels = Array.from(document.querySelectorAll('[data-page-panel]'));

    if (!buttons.length || !panels.length) {
        return;
    }

    buttons.forEach(button => {
        button.addEventListener('click', () => {
            const targetId = button.dataset.pagePanelTarget;
            buttons.forEach(item => item.classList.remove('active'));
            panels.forEach(item => item.classList.remove('active'));
            button.classList.add('active');
            document.getElementById(targetId)?.classList.add('active');
        });
    });
}

function initMessagingModule(moduleElement) {
    const currentUserId = Number(moduleElement.dataset.currentUserId || '0');
    const sendUrl = moduleElement.dataset.sendUrl || '';
    const startUrl = moduleElement.dataset.startUrl || '';
    const threadUrlBase = moduleElement.dataset.threadUrlBase || '';
    const searchUrl = moduleElement.dataset.searchUrl || '';
    const threadsUrl = moduleElement.dataset.threadsUrl || '';
    const downloadUrlBase = moduleElement.dataset.downloadUrlBase || '';

    const searchInput = moduleElement.querySelector('[data-chat-search-input]');
    const searchResults = moduleElement.querySelector('[data-chat-search-results]');
    const threadList = moduleElement.querySelector('[data-chat-thread-list]');
    const statusBanner = moduleElement.querySelector('[data-chat-status-banner]');
    const welcomeBlock = moduleElement.querySelector('[data-chat-welcome]');
    const activeShell = moduleElement.querySelector('[data-chat-active-shell]');
    const headerAvatar = moduleElement.querySelector('[data-chat-header-avatar]');
    const headerName = moduleElement.querySelector('[data-chat-header-name]');
    const headerSubtitle = moduleElement.querySelector('[data-chat-header-subtitle]');
    const messageList = moduleElement.querySelector('[data-chat-message-list]');
    const messageForm = moduleElement.querySelector('[data-chat-message-form]');
    const threadIdInput = moduleElement.querySelector('[data-chat-thread-id]');
    const targetUserIdInput = moduleElement.querySelector('[data-chat-target-user-id]');
    const fileInput = moduleElement.querySelector('[data-chat-file-input]');
    const attachmentList = moduleElement.querySelector('[data-chat-attachment-list]');
    const sendButton = messageForm?.querySelector('.messaging-send-button');
    const antiForgeryToken = messageForm?.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    const state = {
        activeThreadId: 0,
        draftUser: null,
        threads: readThreadsFromDom(threadList),
        searchTimer: null,
        searchVersion: 0
    };

    renderThreadList();
    showIdleState();

    searchInput?.addEventListener('input', () => {
        window.clearTimeout(state.searchTimer);
        state.searchTimer = window.setTimeout(() => {
            searchContacts(searchInput.value || '');
        }, 220);
    });

    searchInput?.addEventListener('focus', () => {
        searchContacts(searchInput.value || '');
    });

    document.addEventListener('click', event => {
        if (!event.target.closest('.messaging-search-block')) {
            hideSearchResults();
        }
    });

    threadList?.addEventListener('click', event => {
        const button = event.target.closest('.messaging-thread-card');
        if (!button) {
            return;
        }

        const threadId = Number(button.dataset.threadId || '0');
        if (threadId > 0) {
            loadThread(threadId, true);
        }
    });

    searchResults?.addEventListener('click', async event => {
        const button = event.target.closest('.messaging-search-result');
        if (!button) {
            return;
        }

        const targetUserId = Number(button.dataset.userId || '0');
        if (targetUserId <= 0) {
            return;
        }

        const response = await fetch(startUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(targetUserId)
        });

        if (!response.ok) {
            showStatus('Не удалось открыть диалог.', 'error');
            return;
        }

        const payload = await response.json();
        hideSearchResults();
        searchInput.value = '';

        if (payload.id > 0) {
            await refreshThreads(payload.id);
            await loadThread(payload.id, true);
            return;
        }

        openDraft(payload.otherUser || {
            id: targetUserId,
            fullName: button.dataset.userName || 'Новый диалог',
            email: button.dataset.userEmail || '',
            role: button.dataset.userRole || '',
            avatarUrl: button.dataset.userAvatarUrl || '',
            subtitle: button.dataset.userSubtitle || ''
        });
    });

    messageForm?.addEventListener('submit', async event => {
        event.preventDefault();

        const formData = new FormData(messageForm);
        setSendingState(true);

        try {
            const response = await fetch(sendUrl, {
                method: 'POST',
                headers: antiForgeryToken
                    ? { 'RequestVerificationToken': antiForgeryToken }
                    : {},
                body: formData
            });

            if (!response.ok) {
                const error = await safeReadJson(response);
                showStatus(error?.message || 'Не удалось отправить сообщение.', 'error');
                return;
            }

            const message = await response.json();
            clearComposer();
            state.activeThreadId = Number(message.threadId || 0);
            state.draftUser = null;
            await refreshThreads(state.activeThreadId);
            await loadThread(state.activeThreadId, true);
        } finally {
            setSendingState(false);
        }
    });

    fileInput?.addEventListener('change', renderSelectedFiles);

    async function refreshThreads(preferredThreadId) {
        const response = await fetch(threadsUrl, { cache: 'no-store' });
        if (!response.ok) {
            return;
        }

        state.threads = await response.json();
        renderThreadList();

        if (preferredThreadId) {
            state.activeThreadId = preferredThreadId;
            markActiveThread();
        }
    }

    async function loadThread(threadId, markRead) {
        if (threadId <= 0) {
            return;
        }

        const response = await fetch(`${threadUrlBase}?id=${encodeURIComponent(threadId)}`, { cache: 'no-store' });
        if (!response.ok) {
            showStatus('Не удалось загрузить сообщения.', 'error');
            return;
        }

        const thread = await response.json();
        state.activeThreadId = thread.id;
        state.draftUser = null;
        showConversation(thread.otherUser, thread.messages || []);

        if (threadIdInput) {
            threadIdInput.value = String(thread.id);
        }

        if (targetUserIdInput) {
            targetUserIdInput.value = '';
        }

        markActiveThread();

        if (markRead) {
            await refreshThreads(thread.id);
        }
    }

    function showIdleState() {
        state.activeThreadId = 0;
        state.draftUser = null;

        if (welcomeBlock) {
            welcomeBlock.hidden = false;
        }

        if (activeShell) {
            activeShell.hidden = true;
        }

        if (threadIdInput) {
            threadIdInput.value = '0';
        }

        if (targetUserIdInput) {
            targetUserIdInput.value = '';
        }

        markActiveThread();
    }

    function openDraft(user) {
        state.activeThreadId = 0;
        state.draftUser = user;
        showConversation(user, [], 'Отправьте первое сообщение, чтобы создать диалог и закрепить его в списке чатов.');
        if (threadIdInput) {
            threadIdInput.value = '0';
        }
        if (targetUserIdInput) {
            targetUserIdInput.value = String(user.id || '');
        }
        markActiveThread();
    }

    function showConversation(user, messages, draftHint) {
        if (!welcomeBlock || !activeShell || !headerName || !headerSubtitle || !headerAvatar) {
            return;
        }

        welcomeBlock.hidden = true;
        activeShell.hidden = false;
        headerName.textContent = user.fullName || 'Собеседник';
        headerSubtitle.textContent = draftHint || user.subtitle || user.email || '';
        headerAvatar.innerHTML = buildAvatarMarkup(user, 'messaging-conversation-avatar-image');
        renderMessages(messages, draftHint);
    }

    function renderMessages(messages, draftHint) {
        if (!messageList) {
            return;
        }

        if (!messages.length) {
            messageList.innerHTML = `
                <div class="messaging-empty-card messaging-empty-card-center">
                    <div class="messaging-empty-title">Сообщений пока нет</div>
                    <div class="messaging-empty-text">${escapeHtml(draftHint || 'Начните переписку: отправьте первое сообщение или прикрепите файл.')}</div>
                </div>`;
            return;
        }

        messageList.innerHTML = messages.map(message => {
            const own = Number(message.senderUserId) === currentUserId;
            const attachments = (message.attachments || []).map(attachment => `
                <a class="messaging-attachment-chip" href="${downloadUrlBase}?id=${attachment.id}">
                    <span>${escapeHtml(attachment.fileName)}</span>
                    <span>${formatBytes(attachment.sizeBytes)}</span>
                </a>`).join('');

            return `
                <div class="messaging-message ${own ? 'own' : ''}">
                    <div class="messaging-message-meta">
                        <span class="messaging-message-author">${escapeHtml(own ? 'Вы' : message.senderFullName || 'Собеседник')}</span>
                        <span class="messaging-message-time">${formatMessageTime(message.createdAtUtc)}</span>
                    </div>
                    ${message.text ? `<div class="messaging-message-bubble">${escapeHtml(message.text).replace(/\n/g, '<br />')}</div>` : ''}
                    ${attachments ? `<div class="messaging-message-attachments">${attachments}</div>` : ''}
                </div>`;
        }).join('');

        messageList.scrollTop = messageList.scrollHeight;
    }

    function renderThreadList() {
        if (!threadList) {
            return;
        }

        if (!state.threads.length) {
            threadList.innerHTML = `
                <div class="messaging-empty-card">
                    <div class="messaging-empty-title">Пока нет ни одного диалога</div>
                    <div class="messaging-empty-text">Найдите доступного собеседника через поиск выше и отправьте первое сообщение. После этого диалог появится в списке чатов.</div>
                </div>`;
            return;
        }

        threadList.innerHTML = state.threads.map(thread => `
            <button type="button"
                    class="messaging-thread-card ${thread.id === state.activeThreadId ? 'active' : ''}"
                    data-thread-id="${thread.id}"
                    data-user-id="${thread.otherUser.id}">
                <div class="messaging-thread-avatar">
                    ${buildAvatarMarkup(thread.otherUser, 'messaging-thread-avatar-image')}
                </div>
                <div class="messaging-thread-main">
                    <div class="messaging-thread-topline">
                        <span class="messaging-thread-name">${escapeHtml(thread.otherUser.fullName)}</span>
                        <span class="messaging-thread-date">${thread.lastMessageAtUtc ? formatListTime(thread.lastMessageAtUtc) : ''}</span>
                    </div>
                    <div class="messaging-thread-subtitle">${escapeHtml(thread.otherUser.subtitle || '')}</div>
                    <div class="messaging-thread-preview-row">
                        <span class="messaging-thread-preview">${escapeHtml(thread.lastMessagePreview || '')}</span>
                        ${thread.unreadCount > 0 ? `<span class="messaging-thread-badge">${thread.unreadCount}</span>` : ''}
                    </div>
                </div>
            </button>`).join('');
    }

    function renderSelectedFiles() {
        if (!attachmentList || !fileInput) {
            return;
        }

        const files = Array.from(fileInput.files || []);
        attachmentList.innerHTML = files.map(file => `
            <div class="messaging-selected-file">
                <span>${escapeHtml(file.name)}</span>
                <span>${formatBytes(file.size)}</span>
            </div>`).join('');
    }

    async function searchContacts(query) {
        if (!searchResults) {
            return;
        }

        const version = ++state.searchVersion;
        const response = await fetch(`${searchUrl}?query=${encodeURIComponent(query || '')}`, { cache: 'no-store' });
        if (!response.ok || version !== state.searchVersion) {
            return;
        }

        const users = await response.json();
        if (version !== state.searchVersion) {
            return;
        }

        if (!users.length) {
            searchResults.innerHTML = '<div class="messaging-search-empty">Ничего не найдено.</div>';
            searchResults.classList.add('open');
            return;
        }

        searchResults.innerHTML = users.map(user => `
            <button type="button"
                    class="messaging-search-result"
                    data-user-id="${user.id}"
                    data-user-name="${escapeHtmlAttribute(user.fullName)}"
                    data-user-email="${escapeHtmlAttribute(user.email || '')}"
                    data-user-role="${escapeHtmlAttribute(user.role || '')}"
                    data-user-avatar-url="${escapeHtmlAttribute(user.avatarUrl || '')}"
                    data-user-subtitle="${escapeHtmlAttribute(user.subtitle || '')}">
                <div class="messaging-search-avatar">${buildAvatarMarkup(user, 'messaging-search-avatar-image')}</div>
                <div class="messaging-search-main">
                    <div class="messaging-search-name">${escapeHtml(user.fullName)}</div>
                    <div class="messaging-search-subtitle">${escapeHtml(user.subtitle || user.email || '')}</div>
                </div>
            </button>`).join('');
        searchResults.classList.add('open');
    }

    function hideSearchResults() {
        if (!searchResults) {
            return;
        }
        searchResults.classList.remove('open');
    }

    function markActiveThread() {
        threadList?.querySelectorAll('.messaging-thread-card').forEach(card => {
            const isActive = Number(card.dataset.threadId || '0') === state.activeThreadId;
            card.classList.toggle('active', isActive);
        });
    }

    function clearComposer() {
        if (messageForm) {
            messageForm.reset();
        }
        if (attachmentList) {
            attachmentList.innerHTML = '';
        }
    }

    function showStatus(message, kind) {
        if (!statusBanner) {
            return;
        }

        statusBanner.textContent = message;
        statusBanner.classList.toggle('visible', Boolean(message));
        statusBanner.classList.toggle('error', kind === 'error');
        statusBanner.classList.toggle('success', kind === 'success');

        if (message) {
            window.setTimeout(() => {
                if (statusBanner.textContent === message) {
                    statusBanner.textContent = '';
                    statusBanner.classList.remove('visible', 'error', 'success');
                }
            }, 4000);
        }
    }

    function setSendingState(isSending) {
        if (!sendButton) {
            return;
        }

        sendButton.disabled = isSending;
        sendButton.textContent = isSending ? 'Отправка...' : 'Отправить';
    }
}

function readThreadsFromDom(threadList) {
    if (!threadList) {
        return [];
    }

    return Array.from(threadList.querySelectorAll('.messaging-thread-card')).map(card => ({
        id: Number(card.dataset.threadId || '0'),
        otherUser: {
            id: Number(card.dataset.userId || '0'),
            fullName: card.dataset.userName || '',
            email: card.dataset.userEmail || '',
            role: card.dataset.userRole || '',
            avatarUrl: card.dataset.userAvatarUrl || '',
            subtitle: card.dataset.userSubtitle || ''
        },
        lastMessagePreview: card.querySelector('.messaging-thread-preview')?.textContent || '',
        lastMessageAtUtc: null,
        unreadCount: Number(card.querySelector('.messaging-thread-badge')?.textContent || '0')
    }));
}

function buildAvatarMarkup(user, imageClass) {
    if (user.avatarUrl) {
        return `<img class="${imageClass}" src="${escapeHtmlAttribute(user.avatarUrl)}" alt="Аватар" />`;
    }

    const initials = (user.fullName || '?')
        .split(' ')
        .filter(Boolean)
        .slice(0, 2)
        .map(part => part[0])
        .join('')
        .toUpperCase();

    return `<span>${escapeHtml(initials || '?')}</span>`;
}

function formatListTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    return new Intl.DateTimeFormat('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).format(date);
}

function formatMessageTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    return new Intl.DateTimeFormat('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).format(date);
}

function formatBytes(bytes) {
    const value = Number(bytes || 0);
    if (value < 1024) {
        return `${value} Б`;
    }

    if (value < 1024 * 1024) {
        return `${(value / 1024).toFixed(1)} КБ`;
    }

    return `${(value / (1024 * 1024)).toFixed(1)} МБ`;
}

function escapeHtml(value) {
    return String(value || '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
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
