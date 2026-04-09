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

    function initCustomSelect(selectId) {
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
});Ц