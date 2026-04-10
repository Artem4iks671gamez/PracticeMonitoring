document.addEventListener('DOMContentLoaded', function () {
    const navButtons = document.querySelectorAll('.admin-nav-button');
    const panels = document.querySelectorAll('.admin-panel');

    navButtons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = button.dataset.panel;

            navButtons.forEach(x => x.classList.remove('active'));
            panels.forEach(x => x.classList.remove('active'));

            button.classList.add('active');

            const target = document.getElementById(targetId);
            if (target) {
                target.classList.add('active');
            }
        });
    });

    const subtabButtons = document.querySelectorAll('.admin-subtab-button');
    const consoles = document.querySelectorAll('.admin-console');

    subtabButtons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = button.dataset.console;

            subtabButtons.forEach(x => x.classList.remove('active'));
            consoles.forEach(x => x.classList.remove('active'));

            button.classList.add('active');

            const target = document.getElementById(targetId);
            if (target) {
                target.classList.add('active');
            }
        });
    });

    function buildCustomSelect(selectId) {
        const nativeSelect = document.getElementById(selectId);
        const custom = document.querySelector(`.custom-select[data-target="${selectId}"]`);

        if (!nativeSelect || !custom) return;

        const trigger = custom.querySelector('.custom-select-trigger');
        const menu = custom.querySelector('.custom-select-menu');

        if (!trigger || !menu) return;

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

                menu.querySelectorAll('.custom-select-option').forEach(x => x.classList.remove('selected'));
                item.classList.add('selected');
                trigger.textContent = opt.textContent;

                custom.classList.remove('open');
            });

            menu.appendChild(item);
        });

        if (!nativeSelect.value && options.length > 0) {
            trigger.textContent = options[0].textContent;
        }
    }

    function initCustomSelect(selectId, onChange) {
        const nativeSelect = document.getElementById(selectId);
        const custom = document.querySelector(`.custom-select[data-target="${selectId}"]`);

        if (!nativeSelect || !custom) return;

        const trigger = custom.querySelector('.custom-select-trigger');
        if (!trigger) return;

        trigger.addEventListener('click', () => {
            document.querySelectorAll('.custom-select.open').forEach(x => {
                if (x !== custom) x.classList.remove('open');
            });

            custom.classList.toggle('open');
        });

        nativeSelect.addEventListener('change', () => {
            buildCustomSelect(selectId);
            if (onChange) onChange(nativeSelect.value);
            applyAccountsFilters();
        });

        buildCustomSelect(selectId);
    }

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.custom-select')) {
            document.querySelectorAll('.custom-select.open').forEach(x => x.classList.remove('open'));
        }
    });

    const searchInput = document.getElementById('accountsSearchInput');
    const roleFilterInputs = document.querySelectorAll('[data-role-filter]');
    const statusFilterInputs = document.querySelectorAll('[data-status-filter]');
    const sortSelect = document.getElementById('sortSelect');
    const accountsList = document.getElementById('accountsList');
    const accountsCountLabel = document.getElementById('accountsCountLabel');

    function getCards() {
        return Array.from(document.querySelectorAll('.account-card'));
    }

    function getActiveRoleFilters() {
        return Array.from(roleFilterInputs)
            .filter(input => input.checked)
            .map(input => input.dataset.roleFilter);
    }

    function getActiveStatusFilters() {
        return Array.from(statusFilterInputs)
            .filter(input => input.checked)
            .map(input => input.dataset.statusFilter);
    }

    function cardSearchText(card) {
        return [
            card.dataset.fullName,
            card.dataset.email,
            card.dataset.role,
            card.dataset.groupName,
            card.dataset.specialtyCode,
            card.dataset.specialtyName,
            card.dataset.course
        ]
            .filter(Boolean)
            .join(' ')
            .toLowerCase();
    }

    function applyAccountsFilters() {
        const searchValue = (searchInput?.value || '').trim().toLowerCase();
        const activeRoles = getActiveRoleFilters();
        const activeStatuses = getActiveStatusFilters();
        const sortValue = sortSelect?.value || 'name-asc';

        let cards = getCards();

        cards.forEach(card => {
            const role = card.dataset.role || '';
            const isActive = (card.dataset.active || 'false') === 'true';

            const passesRoleFilters = activeRoles.length > 0 && activeRoles.includes(role);
            const passesStatusFilters =
                activeStatuses.length > 0 &&
                (
                    (activeStatuses.includes('active') && isActive) ||
                    (activeStatuses.includes('inactive') && !isActive)
                );

            const passesBaseFilters = passesRoleFilters && passesStatusFilters;
            const passesSearch = !searchValue || cardSearchText(card).includes(searchValue);

            const visible = passesBaseFilters && passesSearch;
            card.style.display = visible ? '' : 'none';
        });

        cards = cards.filter(card => card.style.display !== 'none');

        cards.sort((a, b) => {
            const nameA = (a.dataset.fullName || '').toLowerCase();
            const nameB = (b.dataset.fullName || '').toLowerCase();
            const roleA = (a.dataset.role || '').toLowerCase();
            const roleB = (b.dataset.role || '').toLowerCase();
            const activeA = (a.dataset.active || 'false') === 'true';
            const activeB = (b.dataset.active || 'false') === 'true';

            switch (sortValue) {
                case 'name-desc':
                    return nameB.localeCompare(nameA, 'ru');
                case 'role-asc':
                    return roleA.localeCompare(roleB, 'ru') || nameA.localeCompare(nameB, 'ru');
                case 'status-desc':
                    if (activeA === activeB) return nameA.localeCompare(nameB, 'ru');
                    return activeA ? -1 : 1;
                case 'name-asc':
                default:
                    return nameA.localeCompare(nameB, 'ru');
            }
        });

        cards.forEach(card => accountsList.appendChild(card));

        if (accountsCountLabel) {
            accountsCountLabel.textContent = `Показано аккаунтов: ${cards.length}`;
        }
    }

    roleFilterInputs.forEach(input => {
        input.addEventListener('change', applyAccountsFilters);
    });

    statusFilterInputs.forEach(input => {
        input.addEventListener('change', applyAccountsFilters);
    });

    searchInput?.addEventListener('input', applyAccountsFilters);

    initCustomSelect('sortSelect');
    applyAccountsFilters();

    const adminThemeToggle = document.getElementById('adminThemeToggle');
    const storageKey = 'admin-theme-preference';

    const savedTheme = localStorage.getItem(storageKey);
    if (savedTheme === 'dark' || savedTheme === 'light') {
        document.documentElement.setAttribute('data-theme', savedTheme);
    }

    adminThemeToggle?.addEventListener('click', function () {
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';

        document.documentElement.setAttribute('data-theme', nextTheme);
        localStorage.setItem(storageKey, nextTheme);
    });

    const userModalBackdrop = document.getElementById('userModalBackdrop');
    const closeUserModalButton = document.getElementById('closeUserModalButton');
    const cancelUserModalButton = document.getElementById('cancelUserModalButton');

    const userModalTitle = document.getElementById('userModalTitle');
    const userModalSubtitle = document.getElementById('userModalSubtitle');

    const modalUserId = document.getElementById('modalUserId');
    const modalIsCreateMode = document.getElementById('modalIsCreateMode');
    const modalCurrentAvatarUrl = document.getElementById('modalCurrentAvatarUrl');
    const modalRemoveAvatar = document.getElementById('modalRemoveAvatar');
    const modalIsActive = document.getElementById('modalIsActive');

    const modalSurname = document.getElementById('modalSurname');
    const modalFirstName = document.getElementById('modalFirstName');
    const modalPatronymic = document.getElementById('modalPatronymic');
    const modalEmail = document.getElementById('modalEmail');
    const modalRoleSelect = document.getElementById('modalRoleSelect');
    const modalPassword = document.getElementById('modalPassword');
    const modalPasswordGroup = document.getElementById('modalPasswordGroup');

    const modalSpecialtySelect = document.getElementById('modalSpecialtySelect');
    const modalGroupSelect = document.getElementById('modalGroupSelect');
    const modalCourseDisplay = document.getElementById('modalCourseDisplay');

    const modalSpecialtyGroup = document.getElementById('modalSpecialtyGroup');
    const modalGroupWrapper = document.getElementById('modalGroupWrapper');
    const modalCourseGroup = document.getElementById('modalCourseGroup');

    const modalAvatarFile = document.getElementById('modalAvatarFile');
    const modalAvatarFileName = document.getElementById('modalAvatarFileName');
    const modalAvatarPreview = document.getElementById('modalAvatarPreview');
    const removeAvatarButton = document.getElementById('removeAvatarButton');
    const modalActiveSwitch = document.getElementById('modalActiveSwitch');

    let currentEditUser = null;

    function openUserModal() {
        userModalBackdrop?.classList.add('open');
    }

    function closeUserModal() {
        userModalBackdrop?.classList.remove('open');
    }

    closeUserModalButton?.addEventListener('click', closeUserModal);
    cancelUserModalButton?.addEventListener('click', closeUserModal);

    userModalBackdrop?.addEventListener('click', function (e) {
        if (e.target === userModalBackdrop) {
            closeUserModal();
        }
    });

    function getInitials(fullName) {
        return (fullName || 'AA')
            .split(' ')
            .filter(Boolean)
            .slice(0, 2)
            .map(x => x[0])
            .join('')
            .toUpperCase();
    }

    function setAvatarPreview(url, fullName) {
        if (!modalAvatarPreview) return;

        if (url) {
            modalAvatarPreview.innerHTML = `<img src="${url}" alt="avatar">`;
        } else {
            modalAvatarPreview.textContent = getInitials(fullName);
        }
    }

    function setSelectedFileName(file) {
        if (!modalAvatarFileName) return;
        modalAvatarFileName.textContent = file ? file.name : 'Файл не выбран';
    }

    function parseFullName(fullName) {
        const parts = (fullName || '').trim().split(/\s+/);
        return {
            surname: parts[0] || '',
            firstName: parts[1] || '',
            patronymic: parts.slice(2).join(' ')
        };
    }

    async function loadSpecialties(selectedSpecialtyId, selectedGroupId) {
        try {
            const response = await fetch(`${window.apiBaseUrl.replace(/\/$/, '')}/api/Helping/specialties`);
            if (!response.ok) return;

            const list = await response.json();
            modalSpecialtySelect.innerHTML = '<option value="">-- выберите специальность --</option>';

            list.forEach(item => {
                const opt = document.createElement('option');
                opt.value = item.id;
                opt.textContent = `${item.code} — ${item.name}`;
                if (selectedSpecialtyId && Number(selectedSpecialtyId) === item.id) {
                    opt.selected = true;
                }
                modalSpecialtySelect.appendChild(opt);
            });

            buildCustomSelect('modalSpecialtySelect');

            if (selectedSpecialtyId) {
                await loadGroups(selectedSpecialtyId, selectedGroupId);
            } else {
                modalGroupSelect.innerHTML = '<option value="">-- выберите группу --</option>';
                buildCustomSelect('modalGroupSelect');
                modalCourseDisplay.value = '';
            }
        } catch {
        }
    }

    async function loadGroups(specialtyId, selectedGroupId) {
        if (!specialtyId) {
            modalGroupSelect.innerHTML = '<option value="">-- выберите группу --</option>';
            buildCustomSelect('modalGroupSelect');
            modalCourseDisplay.value = '';
            return;
        }

        try {
            const response = await fetch(`${window.apiBaseUrl.replace(/\/$/, '')}/api/Helping/groups?specialtyId=${encodeURIComponent(specialtyId)}`);
            if (!response.ok) return;

            const list = await response.json();
            modalGroupSelect.innerHTML = '<option value="">-- выберите группу --</option>';

            list.forEach(item => {
                const opt = document.createElement('option');
                opt.value = item.id;
                opt.textContent = `${item.name} (курс ${item.course})`;
                opt.dataset.course = item.course;

                if (selectedGroupId && Number(selectedGroupId) === item.id) {
                    opt.selected = true;
                    modalCourseDisplay.value = item.course;
                }

                modalGroupSelect.appendChild(opt);
            });

            buildCustomSelect('modalGroupSelect');
        } catch {
        }
    }

    function syncRoleVisibility() {
        const role = modalRoleSelect.value;
        const isStudent = role === 'Student';

        modalSpecialtyGroup.style.display = isStudent ? '' : 'none';
        modalGroupWrapper.style.display = isStudent ? '' : 'none';
        modalCourseGroup.style.display = isStudent ? '' : 'none';
    }

    modalRoleSelect?.addEventListener('change', syncRoleVisibility);

    modalSpecialtySelect?.addEventListener('change', async function () {
        await loadGroups(modalSpecialtySelect.value, null);
    });

    modalGroupSelect?.addEventListener('change', function () {
        const selected = modalGroupSelect.options[modalGroupSelect.selectedIndex];
        modalCourseDisplay.value = selected?.dataset.course || '';
    });

    modalAvatarFile?.addEventListener('change', function () {
        const file = modalAvatarFile.files && modalAvatarFile.files[0];
        setSelectedFileName(file || null);

        if (!file) return;

        modalRemoveAvatar.value = 'false';

        const reader = new FileReader();
        reader.onload = function (e) {
            if (modalAvatarPreview) {
                modalAvatarPreview.innerHTML = `<img src="${e.target.result}" alt="avatar">`;
            }
        };
        reader.readAsDataURL(file);
    });

    removeAvatarButton?.addEventListener('click', function () {
        modalRemoveAvatar.value = 'true';
        modalCurrentAvatarUrl.value = '';
        modalAvatarFile.value = '';
        setSelectedFileName(null);
        setAvatarPreview('', `${modalSurname.value} ${modalFirstName.value}`);
    });

    modalActiveSwitch?.addEventListener('change', function () {
        modalIsActive.value = modalActiveSwitch.checked ? 'true' : 'false';
    });

    document.querySelectorAll('.edit-account-button').forEach(button => {
        button.addEventListener('click', async function () {
            const card = button.closest('.account-card');
            if (!card) return;

            currentEditUser = card;

            const full = parseFullName(card.dataset.fullName || '');

            userModalTitle.textContent = 'Редактирование пользователя';
            userModalSubtitle.textContent = 'Изменение данных выбранного аккаунта';

            modalUserId.value = card.dataset.userId || '';
            modalIsCreateMode.value = 'false';

            modalSurname.value = full.surname;
            modalFirstName.value = full.firstName;
            modalPatronymic.value = full.patronymic;
            modalEmail.value = card.dataset.email || '';
            modalRoleSelect.value = card.dataset.role || 'Student';
            modalPassword.value = '';
            modalPasswordGroup.style.display = 'none';

            modalCurrentAvatarUrl.value = card.dataset.avatarUrl || '';
            modalRemoveAvatar.value = 'false';
            modalAvatarFile.value = '';
            setSelectedFileName(null);

            modalActiveSwitch.checked = (card.dataset.active || 'false') === 'true';
            modalIsActive.value = modalActiveSwitch.checked ? 'true' : 'false';

            setAvatarPreview(card.dataset.avatarUrl || '', card.dataset.fullName || '');

            buildCustomSelect('modalRoleSelect');
            syncRoleVisibility();

            await loadSpecialties(card.dataset.specialtyId || '', card.dataset.groupId || '');

            openUserModal();
        });
    });

    document.querySelectorAll('.admin-action-button').forEach(button => {
        button.addEventListener('click', async function () {
            const role = button.dataset.createRole || 'Admin';

            currentEditUser = null;

            userModalTitle.textContent = 'Создание пользователя';
            userModalSubtitle.textContent = 'Создание нового аккаунта через админку';

            modalUserId.value = '';
            modalIsCreateMode.value = 'true';

            modalSurname.value = '';
            modalFirstName.value = '';
            modalPatronymic.value = '';
            modalEmail.value = '';
            modalRoleSelect.value = role;
            modalPassword.value = '';
            modalPasswordGroup.style.display = '';

            modalCurrentAvatarUrl.value = '';
            modalRemoveAvatar.value = 'false';
            modalAvatarFile.value = '';
            setSelectedFileName(null);

            modalActiveSwitch.checked = true;
            modalIsActive.value = 'true';

            setAvatarPreview('', 'НП');

            buildCustomSelect('modalRoleSelect');
            syncRoleVisibility();

            modalSpecialtySelect.innerHTML = '<option value="">-- выберите специальность --</option>';
            modalGroupSelect.innerHTML = '<option value="">-- выберите группу --</option>';
            modalCourseDisplay.value = '';

            buildCustomSelect('modalSpecialtySelect');
            buildCustomSelect('modalGroupSelect');

            if (role === 'Student') {
                await loadSpecialties('', '');
            }

            openUserModal();
        });
    });

    initCustomSelect('sortSelect');
    initCustomSelect('modalRoleSelect', syncRoleVisibility);
    initCustomSelect('modalSpecialtySelect');
    initCustomSelect('modalGroupSelect');

    applyAccountsFilters();
});