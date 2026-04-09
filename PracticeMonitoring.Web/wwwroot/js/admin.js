document.addEventListener('DOMContentLoaded', function () {
    const buttons = document.querySelectorAll('.admin-tab-button');
    const consoles = document.querySelectorAll('.admin-console');
    const panels = document.querySelectorAll('.admin-panel');

    buttons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = button.dataset.tab;

            buttons.forEach(x => x.classList.remove('active'));
            consoles.forEach(x => x.classList.remove('active'));
            panels.forEach(x => x.classList.remove('active'));

            button.classList.add('active');

            const consoleTarget = document.getElementById(targetId);
            if (consoleTarget) consoleTarget.classList.add('active');
        });
    });

    const modalBackdrop = document.getElementById('accountModalBackdrop');
    const closeModalButton = document.getElementById('closeAccountModalButton');
    const accountModalTitle = document.getElementById('accountModalTitle');

    const adminSurname = document.getElementById('adminSurname');
    const adminFirstName = document.getElementById('adminFirstName');
    const adminPatronymic = document.getElementById('adminPatronymic');
    const adminEmail = document.getElementById('adminEmail');
    const adminRole = document.getElementById('adminRole');
    const adminSpecialtyView = document.getElementById('adminSpecialtyView');
    const adminGroupView = document.getElementById('adminGroupView');
    const adminCourseView = document.getElementById('adminCourseView');
    const adminAvatarUpload = document.getElementById('adminAvatarUpload');
    const adminAvatarPreview = document.getElementById('adminAvatarPreview');
    const toggleActiveButton = document.getElementById('toggleActiveButton');

    let currentMode = 'edit';
    let currentUserId = null;
    let currentActive = true;

    function openModal() {
        if (modalBackdrop) modalBackdrop.classList.add('open');
    }

    function closeModal() {
        if (modalBackdrop) modalBackdrop.classList.remove('open');
    }

    closeModalButton?.addEventListener('click', closeModal);
    modalBackdrop?.addEventListener('click', e => {
        if (e.target === modalBackdrop) closeModal();
    });

    function fillAvatar(url, fullName) {
        if (!adminAvatarPreview) return;

        if (url) {
            adminAvatarPreview.innerHTML = `<img src="${url}" alt="avatar">`;
        } else {
            const initials = (fullName || '??').substring(0, 2).toUpperCase();
            adminAvatarPreview.textContent = initials;
        }
    }

    document.querySelectorAll('.edit-account-button').forEach(button => {
        button.addEventListener('click', function () {
            const row = button.closest('.account-row');
            if (!row) return;

            currentMode = 'edit';
            currentUserId = row.dataset.userId;
            currentActive = row.dataset.active === 'true';

            accountModalTitle.textContent = 'Редактирование аккаунта';
            adminSurname.value = splitFullName(row.dataset.fullName).surname;
            adminFirstName.value = splitFullName(row.dataset.fullName).firstName;
            adminPatronymic.value = splitFullName(row.dataset.fullName).patronymic;
            adminEmail.value = row.dataset.email || '';
            adminRole.value = row.dataset.role || 'Student';
            adminSpecialtyView.value = [row.dataset.specialtyCode, row.dataset.specialtyName].filter(Boolean).join(' ');
            adminGroupView.value = row.dataset.groupName || '';
            adminCourseView.value = row.dataset.course || '';
            toggleActiveButton.textContent = currentActive ? 'Деактивировать' : 'Активировать';

            fillAvatar(row.dataset.avatarUrl || '', row.dataset.fullName || '');
            openModal();
        });
    });

    document.querySelectorAll('[data-create-role]').forEach(button => {
        button.addEventListener('click', function () {
            currentMode = 'create';
            currentUserId = null;
            currentActive = true;

            const role = button.dataset.createRole;
            accountModalTitle.textContent = `Создание пользователя (${role})`;

            adminSurname.value = '';
            adminFirstName.value = '';
            adminPatronymic.value = '';
            adminEmail.value = '';
            adminRole.value = role;
            adminSpecialtyView.value = '';
            adminGroupView.value = '';
            adminCourseView.value = '';
            toggleActiveButton.textContent = 'Деактивировать';

            fillAvatar('', '??');
            openModal();
        });
    });

    toggleActiveButton?.addEventListener('click', function () {
        currentActive = !currentActive;
        toggleActiveButton.textContent = currentActive ? 'Деактивировать' : 'Активировать';
    });

    function splitFullName(fullName) {
        const parts = (fullName || '').trim().split(/\s+/);
        return {
            surname: parts[0] || '',
            firstName: parts[1] || '',
            patronymic: parts.slice(2).join(' ')
        };
    }
});