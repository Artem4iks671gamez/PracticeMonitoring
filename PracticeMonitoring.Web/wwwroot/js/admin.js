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

    const searchInput = document.getElementById('accountsSearchInput');
    const roleFilterSelect = document.getElementById('roleFilterSelect');
    const statusFilterSelect = document.getElementById('statusFilterSelect');
    const sortSelect = document.getElementById('sortSelect');
    const accountsList = document.getElementById('accountsList');
    const accountsCountLabel = document.getElementById('accountsCountLabel');

    function getCards() {
        return Array.from(document.querySelectorAll('.account-card'));
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
        const roleValue = roleFilterSelect?.value || 'all';
        const statusValue = statusFilterSelect?.value || 'all';
        const sortValue = sortSelect?.value || 'name-asc';

        let cards = getCards();

        cards.forEach(card => {
            const matchesSearch = !searchValue || cardSearchText(card).includes(searchValue);
            const matchesRole = roleValue === 'all' || (card.dataset.role || '') === roleValue;
            const isActive = (card.dataset.active || 'false') === 'true';
            const matchesStatus =
                statusValue === 'all' ||
                (statusValue === 'active' && isActive) ||
                (statusValue === 'inactive' && !isActive);

            const visible = matchesSearch && matchesRole && matchesStatus;
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

    searchInput?.addEventListener('input', applyAccountsFilters);
    roleFilterSelect?.addEventListener('change', applyAccountsFilters);
    statusFilterSelect?.addEventListener('change', applyAccountsFilters);
    sortSelect?.addEventListener('change', applyAccountsFilters);

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
});