document.addEventListener('DOMContentLoaded', function () {
    const modalBackdrop = document.getElementById('profileModalBackdrop');
    const confirmBackdrop = document.getElementById('confirmModalBackdrop');

    const openButton = document.getElementById('openProfileSettingsButton');
    const closeButton = document.getElementById('closeProfileSettingsButton');
    const cancelButton = document.getElementById('cancelProfileSettingsButton');

    const cancelConfirmButton = document.getElementById('cancelConfirmButton');
    const approveConfirmButton = document.getElementById('approveConfirmButton');

    const avatarUpload = document.getElementById('avatarUpload');
    const modalAvatarPreview = document.getElementById('modalAvatarPreview');
    const headerAvatarPreview = document.getElementById('studentAvatarPreview');
    const settingsForm = document.getElementById('profileSettingsForm');

    const themeSelect = document.getElementById('themeSelect');

    const editSurname = document.getElementById('editSurname');
    const editFirstName = document.getElementById('editFirstName');
    const editPatronymic = document.getElementById('editPatronymic');
    const editEmail = document.getElementById('editEmail');

    const initialSurname = document.getElementById('initialSurname')?.value || '';
    const initialFirstName = document.getElementById('initialFirstName')?.value || '';
    const initialPatronymic = document.getElementById('initialPatronymic')?.value || '';
    const initialEmail = document.getElementById('initialEmail')?.value || '';
    const initialTheme = document.getElementById('initialTheme')?.value || 'light';
    const initialAvatarUrl = document.getElementById('initialAvatarUrl')?.value || '';

    let hasNewAvatar = false;
    let pendingSubmit = false;

    function openModal() {
        if (modalBackdrop) {
            modalBackdrop.classList.add('open');
        }
    }

    function closeModal() {
        if (modalBackdrop) {
            modalBackdrop.classList.remove('open');
        }
    }

    function openConfirmModal() {
        if (confirmBackdrop) {
            confirmBackdrop.classList.add('open');
        }
    }

    function closeConfirmModal() {
        if (confirmBackdrop) {
            confirmBackdrop.classList.remove('open');
        }
    }

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

            if (selectId === 'themeSelect') {
                document.documentElement.setAttribute('data-theme', nativeSelect.value);
            }
        });

        buildCustomSelect(selectId);
    }

    function onlyThemeChanged() {
        const surnameChanged = (editSurname?.value || '') !== initialSurname;
        const firstNameChanged = (editFirstName?.value || '') !== initialFirstName;
        const patronymicChanged = (editPatronymic?.value || '') !== initialPatronymic;
        const emailChanged = (editEmail?.value || '') !== initialEmail;
        const themeChanged = (themeSelect?.value || 'light') !== initialTheme;

        return themeChanged && !surnameChanged && !firstNameChanged && !patronymicChanged && !emailChanged && !hasNewAvatar;
    }

    function hasProfileDataChanges() {
        return (
            (editSurname?.value || '') !== initialSurname ||
            (editFirstName?.value || '') !== initialFirstName ||
            (editPatronymic?.value || '') !== initialPatronymic ||
            (editEmail?.value || '') !== initialEmail ||
            hasNewAvatar
        );
    }

    if (openButton) {
        openButton.addEventListener('click', openModal);
    }

    if (closeButton) {
        closeButton.addEventListener('click', closeModal);
    }

    if (cancelButton) {
        cancelButton.addEventListener('click', closeModal);
    }

    if (modalBackdrop) {
        modalBackdrop.addEventListener('click', function (e) {
            if (e.target === modalBackdrop) {
                closeModal();
            }
        });
    }

    if (confirmBackdrop) {
        confirmBackdrop.addEventListener('click', function (e) {
            if (e.target === confirmBackdrop) {
                closeConfirmModal();
            }
        });
    }

    if (cancelConfirmButton) {
        cancelConfirmButton.addEventListener('click', closeConfirmModal);
    }

    if (approveConfirmButton) {
        approveConfirmButton.addEventListener('click', function () {
            pendingSubmit = true;
            closeConfirmModal();
            settingsForm.submit();
        });
    }

    if (avatarUpload) {
        avatarUpload.addEventListener('change', function () {
            const file = avatarUpload.files && avatarUpload.files[0];
            if (!file) {
                hasNewAvatar = false;
                return;
            }

            hasNewAvatar = true;

            const reader = new FileReader();
            reader.onload = function (event) {
                const imageHtml = `<img src="${event.target.result}" alt="Аватар">`;

                if (modalAvatarPreview) {
                    modalAvatarPreview.innerHTML = imageHtml;
                }

                if (headerAvatarPreview) {
                    headerAvatarPreview.innerHTML = imageHtml;
                }
            };

            reader.readAsDataURL(file);
        });
    }

    if (settingsForm) {
        settingsForm.addEventListener('submit', function (e) {
            if (pendingSubmit) {
                pendingSubmit = false;
                return;
            }

            if (onlyThemeChanged()) {
                return;
            }

            if (hasProfileDataChanges()) {
                e.preventDefault();
                openConfirmModal();
            }
        });
    }

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.custom-select')) {
            document.querySelectorAll('.custom-select.open').forEach(x => x.classList.remove('open'));
        }
    });

    initCustomSelect('themeSelect');
});